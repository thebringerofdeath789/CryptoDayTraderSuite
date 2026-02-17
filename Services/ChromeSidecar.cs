using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
    public enum SidecarStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error
    }

    public class ChromeSidecar : IDisposable
    {
        public sealed class SidecarAiResponse
        {
            public string Raw { get; set; }
            public string Service { get; set; }
            public string Model { get; set; }
            public string SourceLabel { get; set; }
        }

        private enum AiProvider
        {
            Unknown,
            ChatGpt,
            Gemini,
            Claude
        }

        private const string DebugBase = "http://localhost:9222";
        private const string DebugUrl = DebugBase + "/json";
        private const string ChatGptStartUrl = "https://chatgpt.com/";
        private const string GeminiStartUrl = "https://gemini.google.com/";
        private const string ClaudeStartUrl = "https://claude.ai/new";
        private static readonly AiProvider[] ProviderRoundRobin = new[] { AiProvider.ChatGpt, AiProvider.Gemini, AiProvider.Claude };
        private const int ConnectTimeoutMs = 12000;
        private const int HostProbeTimeoutMs = 5000;
        private const int HostLaunchWaitMs = 9000;
        private const int SwHide = 0;
        private const int SwShow = 5;
        private const int SwRestore = 9;

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private ClientWebSocket _ws;
        private readonly HttpClient _http;
        private readonly JavaScriptSerializer _json;
        private readonly SemaphoreSlim _queryLock;
        private readonly SemaphoreSlim _sendLock;
        private readonly object _pendingLock = new object();
        private readonly Dictionary<int, TaskCompletionSource<string>> _pendingResponses;
        private int _msgId;
        private CancellationTokenSource _receivePumpCts;
        private Task _receivePumpTask;

        private AiProvider _provider = AiProvider.Unknown;
        private string _targetTitle;
        private string _targetUrl;
        private string _lastAiResponse = string.Empty;
        private bool _chromeLaunchAttempted;
        private int _roundRobinCursor;
        private int _managedChromeProcessId;
        private bool _launchChromeHidden;

        public event Action<string> OnLog;
        public event Action<SidecarStatus> StatusChanged;

        public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;
        public SidecarStatus Status { get; private set; } = SidecarStatus.Disconnected;

        public string CurrentServiceName => ProviderToServiceName(_provider);
        public string CurrentModelName => DetectModelName(_targetTitle, _provider);
        public string CurrentSourceLabel => BuildSourceLabel(CurrentServiceName, CurrentModelName);
        public bool LaunchChromeHidden => _launchChromeHidden;

        public void SetLaunchChromeHidden(bool hidden)
        {
            _launchChromeHidden = hidden;
            Log(hidden ? "Chrome launch mode set to hidden." : "Chrome launch mode set to minimized.");
        }

        public bool SetManagedChromeVisible(bool visible)
        {
            var handle = TryGetManagedChromeWindowHandle();
            if (handle == IntPtr.Zero)
            {
                Log("No managed Chrome window found to toggle visibility.");
                return false;
            }

            if (visible)
            {
                ShowWindow(handle, SwRestore);
                ShowWindow(handle, SwShow);
                return true;
            }

            ShowWindow(handle, SwHide);
            return true;
        }

        public bool IsManagedChromeVisible()
        {
            var handle = TryGetManagedChromeWindowHandle();
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            return IsWindowVisible(handle);
        }

        public ChromeSidecar()
        {
            _http = new HttpClient();
            _http.Timeout = TimeSpan.FromMilliseconds(ConnectTimeoutMs);
            _json = new JavaScriptSerializer();
            _queryLock = new SemaphoreSlim(1, 1);
            _sendLock = new SemaphoreSlim(1, 1);
            _pendingResponses = new Dictionary<int, TaskCompletionSource<string>>();
        }

        private void SetStatus(SidecarStatus status)
        {
            if (Status != status)
            {
                Status = status;
                StatusChanged?.Invoke(Status);
            }
        }

        private void MarkDisconnected(string reason)
        {
            if (Status != SidecarStatus.Disconnected)
            {
                if (string.IsNullOrWhiteSpace(reason))
                {
                    Log("Disconnected");
                }
                else
                {
                    Log("Disconnected: " + reason);
                }
            }

            SetStatus(SidecarStatus.Disconnected);
        }

        public async Task<bool> ConnectAsync()
        {
            return await ConnectInternalAsync(AiProvider.Unknown);
        }

        public async Task<bool> ConnectAsync(string preferredService)
        {
            return await ConnectInternalAsync(ParsePreferredProvider(preferredService));
        }

        private async Task<bool> ConnectInternalAsync(AiProvider preferredProvider)
        {
            SetStatus(SidecarStatus.Connecting);

            try
            {
                StopReceivePump();
                try { _ws?.Dispose(); } catch { }
                _ws = null;

                await EnsureDebugHostReadyAsync(preferredProvider);

                var tabs = await GetTabsAsync();

                if (preferredProvider != AiProvider.Unknown)
                {
                    await EnsureProviderTabAsync(preferredProvider);
                    tabs = await GetTabsAsync();
                }

                var aiTabs = tabs.Where(IsAiTab).ToList();
                if (aiTabs.Count == 0)
                {
                    aiTabs = await EnsureAnyAiTabAsync(preferredProvider);
                    if (aiTabs.Count == 0)
                    {
                        Log("No AI tab found and automatic tab creation failed.");
                        MarkDisconnected("No AI tab found after automatic creation attempt.");
                        return false;
                    }
                }

                dynamic target = null;

                if (preferredProvider != AiProvider.Unknown)
                {
                    var preferredTabs = aiTabs.Where(t => DetectProvider(GetTabTitle(t), GetTabUrl(t)) == preferredProvider).ToList();
                    target = preferredTabs.FirstOrDefault(IsActiveTab) ?? preferredTabs.FirstOrDefault();
                }

                if (target == null)
                {
                    target = aiTabs.FirstOrDefault(IsActiveTab) ?? aiTabs.FirstOrDefault();
                }

                if (target == null)
                {
                    MarkDisconnected("No target AI tab could be selected.");
                    return false;
                }

                var wsUrl = GetWebSocketDebuggerUrl(target);
                if (string.IsNullOrEmpty(wsUrl))
                {
                    Log("Target tab does not have a WebSocket URL.");
                    MarkDisconnected("Target AI tab missing WebSocket debugger URL.");
                    return false;
                }

                _targetTitle = GetTabTitle(target);
                _targetUrl = GetTabUrl(target);
                _provider = DetectProvider(_targetTitle, _targetUrl);

                await ActivateTabAsync(target);

                Log("Connecting to " + _targetTitle + " [" + _provider + "]...");

                _ws = new ClientWebSocket();
                using (var connectCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(ConnectTimeoutMs)))
                {
                    await _ws.ConnectAsync(new Uri(wsUrl), connectCts.Token);
                }
                StartReceivePump();

                Log("Connected via CDP.");
                SetStatus(SidecarStatus.Connected);
                return true;
            }
            catch (Exception ex)
            {
                Log("Connection failed: " + ex.Message);
                SetStatus(SidecarStatus.Error);
                return false;
            }
        }

        public async Task<List<string>> GetAvailableServicesAsync()
        {
            var services = new List<string>();
            try
            {
                await EnsureProviderTabAsync(AiProvider.ChatGpt);
                await EnsureProviderTabAsync(AiProvider.Gemini);
                await EnsureProviderTabAsync(AiProvider.Claude);

                var tabs = await GetTabsAsync();
                foreach (var tab in tabs)
                {
                    if (!IsAiTab(tab)) continue;

                    var provider = DetectProvider(GetTabTitle(tab), GetTabUrl(tab));
                    var service = ProviderToServiceName(provider);
                    if (!string.IsNullOrWhiteSpace(service) && !services.Contains(service))
                    {
                        services.Add(service);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("GetAvailableServices failed: " + ex.Message);
            }

            return services;
        }

        public async Task<List<SidecarAiResponse>> QueryAcrossAvailableServicesAsync(string prompt)
        {
            var results = new List<SidecarAiResponse>();
            var startProvider = GetNextRoundRobinPrimary();
            var providers = BuildProviderFallbackOrder(startProvider, false);

            foreach (var provider in providers)
            {
                var service = ProviderToServiceName(provider);
                try
                {
                    var connected = await ConnectAsync(service);
                    if (!connected)
                    {
                        Log("Skipping " + service + ": unable to connect.");
                        continue;
                    }

                    var raw = await QueryAIAsync(prompt, provider);
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        Log("No response captured from " + service + ".");
                        continue;
                    }

                    results.Add(new SidecarAiResponse
                    {
                        Raw = raw,
                        Service = CurrentServiceName,
                        Model = CurrentModelName,
                        SourceLabel = CurrentSourceLabel
                    });
                }
                catch (Exception ex)
                {
                    Log("Query failed for " + service + ": " + ex.Message);
                }
            }

            return results;
        }

        public async Task<string> EvaluateJsAsync(string script, int responseTimeoutMs = 12000)
        {
            if (!IsConnected) return string.Empty;

            var id = Interlocked.Increment(ref _msgId);
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_pendingLock)
            {
                _pendingResponses[id] = tcs;
            }

            var command = new
            {
                id = id,
                method = "Runtime.evaluate",
                @params = new
                {
                    expression = script,
                    returnByValue = true,
                    awaitPromise = true
                }
            };

            var request = _json.Serialize(command);
            var sendOk = await SendAsync(request, 3000);
            if (!sendOk)
            {
                lock (_pendingLock)
                {
                    _pendingResponses.Remove(id);
                }
                Log("EvaluateJsAsync send failed/timed out.");
                return "error: send-timeout";
            }

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Math.Max(1000, responseTimeoutMs)));
            if (completed != tcs.Task)
            {
                lock (_pendingLock)
                {
                    _pendingResponses.Remove(id);
                }

                Log("EvaluateJsAsync timeout waiting for matching CDP response.");
                return string.Empty;
            }

            var responseJson = await tcs.Task;
            if (string.IsNullOrWhiteSpace(responseJson))
            {
                return string.Empty;
            }

            try
            {
                var dict = _json.Deserialize<Dictionary<string, object>>(responseJson);
                if (dict != null && dict.ContainsKey("result"))
                {
                    var result = dict["result"] as Dictionary<string, object>;
                    if (result != null && result.ContainsKey("result"))
                    {
                        var inner = result["result"] as Dictionary<string, object>;
                        if (inner != null && inner.ContainsKey("value"))
                        {
                            return inner["value"] == null ? string.Empty : inner["value"].ToString();
                        }
                    }
                }

                if (dict != null && dict.ContainsKey("error"))
                {
                    return "error: " + dict["error"];
                }
            }
            catch { }

            return responseJson;
        }

        public async Task<string> QueryAIAsync(string prompt)
        {
            return await QueryAIAsync(prompt, _provider, false);
        }

        private async Task<string> QueryAIAsync(string prompt, AiProvider preferredProvider)
        {
            return await QueryAIAsync(prompt, preferredProvider, preferredProvider != AiProvider.Unknown);
        }

        private async Task<string> QueryAIAsync(string prompt, AiProvider preferredProvider, bool strictProvider)
        {
            if (!IsConnected) return string.Empty;

            await _queryLock.WaitAsync();
            try
            {
                var primaryProvider = preferredProvider == AiProvider.Unknown ? GetNextRoundRobinPrimary() : preferredProvider;
                if (primaryProvider == AiProvider.Unknown)
                {
                    primaryProvider = AiProvider.ChatGpt;
                }

                if (_provider != primaryProvider)
                {
                    var preferredService = ProviderToServiceName(primaryProvider);
                    var reconnected = await ConnectAsync(preferredService);
                    if (!reconnected || !IsConnected)
                    {
                        Log("Failed to switch provider before query: " + preferredService);
                        return string.Empty;
                    }

                    await Task.Delay(primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude ? 700 : 350);
                }

                var escapedPrompt = EscapeJsString(prompt ?? string.Empty);
                var baselineResponse = (await EvaluateJsAsync(BuildReadScript(primaryProvider), 3500) ?? string.Empty).Trim();

                if (primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude)
                {
                    await Task.Delay(700);
                }

                var injectResult = string.Empty;
                for (var pass = 0; pass < 2; pass++)
                {
                    injectResult = string.Empty;
                    foreach (var provider in BuildProviderFallbackOrder(primaryProvider, strictProvider))
                    {
                        var timeoutMs = provider == AiProvider.Gemini || provider == AiProvider.Claude ? 12000 : 8000;
                        injectResult = await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), timeoutMs, 2, 400);
                        injectResult = await ResolvePendingSendStatusAsync(provider, injectResult);
                        if (!IsError(injectResult) && !IsNoSendStatus(injectResult) && !string.IsNullOrWhiteSpace(injectResult))
                        {
                            _provider = provider;
                            AdvanceRoundRobinCursor(provider);
                            primaryProvider = provider;
                            break;
                        }
                    }

                    if (!IsError(injectResult) && !string.IsNullOrWhiteSpace(injectResult))
                    {
                        break;
                    }

                    if (pass == 0)
                    {
                        Log("Prompt injection first pass failed; reconnecting and retrying...");
                        var reconnectService = ProviderToServiceName(primaryProvider);
                        await ConnectAsync(reconnectService);
                        await Task.Delay(primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude ? 900 : 400);
                    }
                }

                if (IsNoSendStatus(injectResult))
                {
                    Log("Prompt injection did not submit (no-send) for provider " + ProviderToServiceName(primaryProvider) + ".");
                    return string.Empty;
                }

                if (IsError(injectResult) || string.IsNullOrWhiteSpace(injectResult))
                {
                    Log("Prompt injection failed: " + injectResult);
                    return string.Empty;
                }

                Log("Prompt injection result: " + (string.IsNullOrWhiteSpace(injectResult) ? "(empty)" : injectResult));
                Log("Prompt injected. Waiting for model response...");

                var direct = await PollForModelResponseAsync(primaryProvider, baselineResponse, strictProvider);
                if (!string.IsNullOrWhiteSpace(direct))
                {
                    _lastAiResponse = direct;
                    Log("Model response captured.");
                    return direct;
                }

                if (primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude)
                {
                    var stuck = await IsProviderLikelyStuckAsync(primaryProvider);
                    if (stuck)
                    {
                        Log("Detected stuck provider state; attempting refresh-and-resend recovery...");
                        var refreshed = await AttemptRefreshAndResendRecoveryAsync(primaryProvider, escapedPrompt, strictProvider);
                        if (!string.IsNullOrWhiteSpace(refreshed))
                        {
                            _lastAiResponse = refreshed;
                            Log("Model response captured after refresh-and-resend recovery.");
                            return refreshed;
                        }
                    }

                    Log("Provider response stalled; attempting recovery (new chat / refresh / fresh tab)...");
                    var recovered = await AttemptProviderRecoveryAsync(primaryProvider, escapedPrompt, strictProvider);
                    if (!string.IsNullOrWhiteSpace(recovered))
                    {
                        _lastAiResponse = recovered;
                        Log("Model response captured after provider recovery.");
                        return recovered;
                    }
                }

                Log("Response polling timed out.");
                return string.Empty;
            }
            finally
            {
                _queryLock.Release();
            }
        }

        private List<AiProvider> BuildProviderFallbackOrder(AiProvider primaryProvider, bool strictProvider)
        {
            if (strictProvider)
            {
                return new List<AiProvider> { primaryProvider == AiProvider.Unknown ? AiProvider.ChatGpt : primaryProvider };
            }

            var start = primaryProvider == AiProvider.Unknown ? AiProvider.ChatGpt : primaryProvider;
            var startIndex = Array.IndexOf(ProviderRoundRobin, start);
            if (startIndex < 0) startIndex = 0;

            var ordered = new List<AiProvider>(ProviderRoundRobin.Length);
            for (var i = 0; i < ProviderRoundRobin.Length; i++)
            {
                ordered.Add(ProviderRoundRobin[(startIndex + i) % ProviderRoundRobin.Length]);
            }

            return ordered;
        }

        private AiProvider GetNextRoundRobinPrimary()
        {
            if (_roundRobinCursor < 0 || _roundRobinCursor >= ProviderRoundRobin.Length)
            {
                _roundRobinCursor = 0;
            }

            var provider = ProviderRoundRobin[_roundRobinCursor];
            _roundRobinCursor = (_roundRobinCursor + 1) % ProviderRoundRobin.Length;
            return provider;
        }

        private void AdvanceRoundRobinCursor(AiProvider provider)
        {
            var index = Array.IndexOf(ProviderRoundRobin, provider);
            if (index < 0)
            {
                return;
            }

            _roundRobinCursor = (index + 1) % ProviderRoundRobin.Length;
        }

        private async Task<string> ReadCandidateResponseAsync(AiProvider primaryProvider, string baselineResponse, bool allowBaselineMatch, bool strictProvider)
        {
            foreach (var provider in BuildProviderFallbackOrder(primaryProvider, strictProvider))
            {
                var candidate = (await EvaluateJsAsync(BuildReadScript(provider), 2500) ?? string.Empty).Trim();
                if (!IsAcceptableResponseCandidate(candidate, baselineResponse, allowBaselineMatch))
                {
                    continue;
                }

                if (provider != _provider)
                {
                    _provider = provider;
                }

                return candidate;
            }

            if (primaryProvider == AiProvider.Claude || primaryProvider == AiProvider.Gemini)
            {
                return string.Empty;
            }

            var generic = (await EvaluateJsAsync(BuildGenericReadScript(), 2500) ?? string.Empty).Trim();
            if (IsAcceptableResponseCandidate(generic, baselineResponse, allowBaselineMatch))
            {
                return generic;
            }

            return string.Empty;
        }

        private async Task<bool> IsProviderLikelyStuckAsync(AiProvider provider)
        {
            if (provider != AiProvider.Claude && provider != AiProvider.Gemini)
            {
                return false;
            }

            var script = BuildStuckDetectionScript(provider);
            var result = (await EvaluateJsAsync(script, 3500) ?? string.Empty).Trim().ToLowerInvariant();
            return string.Equals(result, "stuck", StringComparison.OrdinalIgnoreCase);
        }

        private string BuildStuckDetectionScript(AiProvider provider)
        {
            if (provider == AiProvider.Claude)
            {
                return @"(function(){
                    try{
                        var getComposer=function(){
                            return document.querySelector('div[contenteditable=""true""][data-testid*=""chat-input""]')
                                || document.querySelector('div[contenteditable=""true""][aria-label*=""Talk to Claude""]')
                                || document.querySelector('div[contenteditable=""true""][aria-label*=""Message Claude""]')
                                || document.querySelector('div[contenteditable=""true""][role=""textbox""]')
                                || document.querySelector('textarea');
                        };
                        var composer=getComposer();
                        var composerText='';
                        if(composer){composerText=((composer.value||composer.innerText||composer.textContent||'')+'').trim();}

                        var spinner=document.querySelector('[class*=""spin""]')||document.querySelector('[class*=""loading""]')||document.querySelector('svg[class*=""animate-spin""]');
                        var stopBtn=(function(){
                            var nodes=document.querySelectorAll('button,[role=""button""]');
                            for(var i=0;i<nodes.length;i++){
                                var t=((nodes[i].innerText||nodes[i].textContent||'')+'').toLowerCase();
                                var a=((nodes[i].getAttribute('aria-label')||'')+'').toLowerCase();
                                if(t.indexOf('stop')>=0||t.indexOf('cancel')>=0||a.indexOf('stop')>=0||a.indexOf('cancel')>=0) return true;
                            }
                            return false;
                        })();

                        var assistant=document.querySelector('[data-testid*=""assistant""],[data-message-author-role=""assistant""]');
                        var busy=!!spinner || !!stopBtn;
                        var hasDraft=composerText.length>0;
                        var hasAssistant=!!assistant;
                        if(busy && hasDraft && !hasAssistant){ return 'stuck'; }
                        if(busy && hasDraft){ return 'stuck'; }
                        return 'ok';
                    }catch(e){return 'ok';}
                })();";
            }

            return @"(function(){
                try{
                    var composer=document.querySelector('div[contenteditable=""true""][aria-label*=""prompt""]')||document.querySelector('div[contenteditable=""true""]')||document.querySelector('textarea');
                    var composerText='';
                    if(composer){composerText=((composer.value||composer.innerText||composer.textContent||'')+'').trim();}
                    var spinner=document.querySelector('[class*=""spin""]')||document.querySelector('[class*=""loading""]')||document.querySelector('mat-progress-spinner,md-progress-circular');
                    var busy=!!spinner;
                    if(busy && composerText.length>0){ return 'stuck'; }
                    return 'ok';
                }catch(e){return 'ok';}
            })();";
        }

        private async Task<string> AttemptRefreshAndResendRecoveryAsync(AiProvider provider, string escapedPrompt, bool strictProvider)
        {
            var service = ProviderToServiceName(provider);
            try
            {
                await EvaluateJsAsync("(function(){try{location.reload();return 'ok';}catch(e){return 'error:'+e.message;}})();", 3000);
            }
            catch
            {
            }

            await Task.Delay(provider == AiProvider.Gemini ? 2600 : 2200);
            await ConnectAsync(service);
            await Task.Delay(provider == AiProvider.Gemini ? 1200 : 1000);

            var baseline = (await EvaluateJsAsync(BuildReadScript(provider), 3500) ?? string.Empty).Trim();
            var inject = await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), 12000, 2, 400);
            inject = await ResolvePendingSendStatusAsync(provider, inject);

            if (IsNoSendStatus(inject) || IsError(inject) || string.IsNullOrWhiteSpace(inject))
            {
                return string.Empty;
            }

            return await PollForModelResponseAsync(provider, baseline, strictProvider);
        }

        private bool IsAcceptableResponseCandidate(string response, string baselineResponse, bool allowBaselineMatch)
        {
            if (string.IsNullOrWhiteSpace(response)) return false;
            if (IsError(response))
            {
                Log("Read script returned error: " + response);
                return false;
            }
            if (LooksLikeThinkingState(response)) return false;
            if (!IsLikelyAssistantContent(response)) return false;
            if (!allowBaselineMatch
                && !string.IsNullOrWhiteSpace(baselineResponse)
                && string.Equals(response, baselineResponse, StringComparison.Ordinal)) return false;

            return true;
        }

        private async Task<string> EvaluateJsWithRetryAsync(string script, int timeoutMs, int attempts, int retryDelayMs)
        {
            var tries = Math.Max(1, attempts);
            for (var attempt = 0; attempt < tries; attempt++)
            {
                var result = await EvaluateJsAsync(script, timeoutMs);
                if (!string.IsNullOrWhiteSpace(result) && !IsError(result))
                {
                    return result;
                }

                if (attempt + 1 < tries)
                {
                    await Task.Delay(Math.Max(100, retryDelayMs));
                }
            }

            return string.Empty;
        }

        private async Task<string> PollForModelResponseAsync(AiProvider provider, string baselineResponse, bool strictProvider)
        {
            string bestCandidate = string.Empty;
            int stableCount = 0;

            var maxAttempts = provider == AiProvider.Gemini || provider == AiProvider.Claude ? 32 : 20;
            var pollDelayMs = provider == AiProvider.Gemini || provider == AiProvider.Claude ? 2000 : 1500;

            for (var i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(pollDelayMs);
                Log("Polling model response attempt " + (i + 1));

                var response = await ReadCandidateResponseAsync(provider, baselineResponse, false, strictProvider);
                if (string.IsNullOrWhiteSpace(response)) continue;

                if (string.Equals(response, bestCandidate, StringComparison.Ordinal))
                {
                    stableCount++;
                }
                else
                {
                    bestCandidate = response;
                    stableCount = 1;
                }

                if (stableCount >= 2 || response.Length > 12)
                {
                    await Task.Delay(1000);
                    var confirm = await ReadCandidateResponseAsync(provider, baselineResponse, false, strictProvider);
                    if (!string.IsNullOrWhiteSpace(confirm))
                    {
                        return confirm;
                    }

                    if (response.Length > 12 && response.Length <= 32)
                    {
                        return response;
                    }
                }
            }

            if (provider != AiProvider.Claude && provider != AiProvider.Gemini)
            {
                var generic = (await EvaluateJsAsync(BuildGenericReadScript(), 3000) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(generic)
                    && IsLikelyAssistantContent(generic)
                    && !LooksLikeThinkingState(generic))
                {
                    Log("Model response captured via generic fallback selector.");
                    return generic;
                }
            }

            return string.Empty;
        }

        private async Task<string> AttemptProviderRecoveryAsync(AiProvider provider, string escapedPrompt, bool strictProvider)
        {
            if (provider != AiProvider.Gemini && provider != AiProvider.Claude)
            {
                return string.Empty;
            }

            var service = ProviderToServiceName(provider);

            try
            {
                await EvaluateJsAsync(BuildStartFreshChatScript(provider), 5000);
            }
            catch
            {
            }

            await Task.Delay(provider == AiProvider.Gemini ? 1200 : 900);

            var baseline = (await EvaluateJsAsync(BuildReadScript(provider), 3500) ?? string.Empty).Trim();
            var inject = await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), 12000, 2, 400);
            inject = await ResolvePendingSendStatusAsync(provider, inject);
            if (!IsError(inject) && !IsNoSendStatus(inject) && !string.IsNullOrWhiteSpace(inject))
            {
                var recovered = await PollForModelResponseAsync(provider, baseline, strictProvider);
                if (!string.IsNullOrWhiteSpace(recovered))
                {
                    return recovered;
                }
            }

            Log("Soft recovery did not produce a response; opening fresh provider tab...");

            try
            {
                var startUrl = GetProviderStartUrl(provider);
                if (!string.IsNullOrWhiteSpace(startUrl))
                {
                    await CreateTabAsync(startUrl);
                }
            }
            catch
            {
            }

            await Task.Delay(provider == AiProvider.Gemini ? 1800 : 1300);
            await ConnectAsync(service);
            await Task.Delay(provider == AiProvider.Gemini ? 1200 : 900);

            baseline = (await EvaluateJsAsync(BuildReadScript(provider), 3500) ?? string.Empty).Trim();
            inject = await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), 12000, 2, 400);
            inject = await ResolvePendingSendStatusAsync(provider, inject);
            if (IsNoSendStatus(inject))
            {
                Log("Fresh-tab recovery prompt was injected but not submitted (no-send).");
                return string.Empty;
            }

            if (IsError(inject) || string.IsNullOrWhiteSpace(inject))
            {
                Log("Fresh-tab recovery prompt injection failed: " + inject);
                return string.Empty;
            }

            return await PollForModelResponseAsync(provider, baseline, strictProvider);
        }

        private async Task<bool> SendAsync(string msg, int timeoutMs)
        {
            await _sendLock.WaitAsync();
            var bytes = Encoding.UTF8.GetBytes(msg);
            try
            {
                var sendTask = _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                var completed = await Task.WhenAny(sendTask, Task.Delay(Math.Max(500, timeoutMs)));
                if (completed != sendTask)
                {
                    if (!IsConnected)
                    {
                        MarkDisconnected("CDP send timed out after socket close.");
                    }

                    return false;
                }

                await sendTask;
                return true;
            }
            catch
            {
                if (!IsConnected)
                {
                    MarkDisconnected("CDP send failed due to closed socket.");
                }

                return false;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        private void StartReceivePump()
        {
            StopReceivePump();
            _receivePumpCts = new CancellationTokenSource();
            _receivePumpTask = Task.Run(() => ReceivePumpAsync(_receivePumpCts.Token));
        }

        private void StopReceivePump()
        {
            try
            {
                if (_receivePumpCts != null)
                {
                    _receivePumpCts.Cancel();
                }
            }
            catch
            {
            }

            try
            {
                if (_receivePumpTask != null)
                {
                    _receivePumpTask.Wait(300);
                }
            }
            catch
            {
            }

            try { _receivePumpCts?.Dispose(); } catch { }
            _receivePumpCts = null;
            _receivePumpTask = null;

            CancelPendingResponses();
        }

        private void CancelPendingResponses()
        {
            List<TaskCompletionSource<string>> pending;
            lock (_pendingLock)
            {
                pending = _pendingResponses.Values.ToList();
                _pendingResponses.Clear();
            }

            foreach (var p in pending)
            {
                try { p.TrySetResult(string.Empty); } catch { }
            }
        }

        private async Task ReceivePumpAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (!IsConnected)
                {
                    if (Status == SidecarStatus.Connected)
                    {
                        MarkDisconnected("CDP websocket is not open.");
                    }

                    await Task.Delay(100, token).ConfigureAwait(false);
                    continue;
                }

                string responseJson;
                try
                {
                    responseJson = await ReceiveAsync(30000);
                }
                catch
                {
                    responseJson = string.Empty;
                }

                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    if (!IsConnected && Status == SidecarStatus.Connected)
                    {
                        MarkDisconnected("CDP receive returned empty after socket close.");
                    }

                    continue;
                }

                int responseId;
                if (!TryGetMessageId(responseJson, out responseId))
                {
                    continue;
                }

                TaskCompletionSource<string> pending = null;
                lock (_pendingLock)
                {
                    if (_pendingResponses.TryGetValue(responseId, out pending))
                    {
                        _pendingResponses.Remove(responseId);
                    }
                }

                if (pending != null)
                {
                    pending.TrySetResult(responseJson);
                }
            }
        }

        private async Task<string> ReceiveAsync(int timeoutMs)
        {
            if (!IsConnected) return string.Empty;

            var buffer = new byte[8192];
            var sb = new StringBuilder();

            try
            {
                do
                {
                    var receiveTask = _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    var completed = await Task.WhenAny(receiveTask, Task.Delay(Math.Max(500, timeoutMs)));
                    if (completed != receiveTask)
                    {
                        if (!IsConnected && Status == SidecarStatus.Connected)
                        {
                            MarkDisconnected("CDP receive timed out after socket close.");
                        }

                        return string.Empty;
                    }

                    var result = await receiveTask;
                    sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        break;
                    }
                }
                while (true);
            }
            catch
            {
                if (!IsConnected && Status == SidecarStatus.Connected)
                {
                    MarkDisconnected("CDP receive failed due to closed socket.");
                }

                return string.Empty;
            }

            return sb.ToString();
        }

        private bool TryGetMessageId(string json, out int id)
        {
            id = 0;
            try
            {
                var dict = _json.Deserialize<Dictionary<string, object>>(json);
                if (dict == null || !dict.ContainsKey("id") || dict["id"] == null) return false;

                var val = dict["id"];
                if (val is int) { id = (int)val; return true; }
                if (val is long) { id = (int)(long)val; return true; }
                if (val is double) { id = (int)(double)val; return true; }

                int parsed;
                if (int.TryParse(val.ToString(), out parsed))
                {
                    id = parsed;
                    return true;
                }
            }
            catch { }

            return false;
        }

        private bool IsAiTab(dynamic tab)
        {
            var type = GetTabType(tab);
            if (!string.Equals(type, "page", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var title = GetTabTitle(tab);
            var url = GetTabUrl(tab);
            return ContainsIgnoreCase(title, "ChatGPT")
                || ContainsIgnoreCase(title, "Gemini")
                || ContainsIgnoreCase(title, "Claude")
                || ContainsIgnoreCase(url, "chatgpt.com")
                || ContainsIgnoreCase(url, "gemini.google.com")
                || ContainsIgnoreCase(url, "claude.ai");
        }

        private async Task<List<dynamic>> GetTabsAsync()
        {
            try
            {
                var json = await _http.GetStringAsync(DebugUrl);
                return _json.Deserialize<List<dynamic>>(json) ?? new List<dynamic>();
            }
            catch
            {
                return new List<dynamic>();
            }
        }

        private async Task EnsureDebugHostReadyAsync(AiProvider preferredProvider)
        {
            if (await IsDebugHostAvailableAsync())
            {
                return;
            }

            if (_chromeLaunchAttempted)
            {
                return;
            }

            _chromeLaunchAttempted = true;
            var startUrl = GetProviderStartUrl(preferredProvider == AiProvider.Unknown ? AiProvider.ChatGpt : preferredProvider);
            var launched = TryStartChromeDebugHost(startUrl);
            if (!launched)
            {
                Log("Chrome debug host unavailable and automatic Chrome launch failed.");
                return;
            }

            Log("Started Chrome in remote-debug mode; waiting for CDP host...");
            var begin = DateTime.UtcNow;
            while ((DateTime.UtcNow - begin).TotalMilliseconds < HostLaunchWaitMs)
            {
                await Task.Delay(500);
                if (await IsDebugHostAvailableAsync())
                {
                    Log("CDP host is now available.");
                    return;
                }
            }

            Log("CDP host did not become available after automatic Chrome launch.");
        }

        private async Task<bool> IsDebugHostAvailableAsync()
        {
            try
            {
                using (var cts = new CancellationTokenSource(HostProbeTimeoutMs))
                using (var request = new HttpRequestMessage(HttpMethod.Get, DebugBase + "/json/version"))
                using (var response = await _http.SendAsync(request, cts.Token))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool TryStartChromeDebugHost(string startUrl)
        {
            try
            {
                var chromePath = ResolveChromePath();
                if (string.IsNullOrWhiteSpace(chromePath))
                {
                    return false;
                }

                var userDataDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CryptoSidecar");

                var args = new StringBuilder();
                args.Append("--remote-debugging-port=9222 ");
                args.Append("--user-data-dir=\"").Append(userDataDir).Append("\"");
                args.Append(" --disable-session-crashed-bubble");
                args.Append(" --no-first-run");
                args.Append(" --no-default-browser-check");
                args.Append(" --start-minimized");
                if (!string.IsNullOrWhiteSpace(startUrl))
                {
                    args.Append(" \"").Append(startUrl).Append("\"");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = chromePath,
                    Arguments = args.ToString(),
                    UseShellExecute = true,
                    WindowStyle = _launchChromeHidden ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Minimized
                };

                var process = Process.Start(psi);
                _managedChromeProcessId = process != null ? process.Id : 0;
                if (_launchChromeHidden)
                {
                    Thread.Sleep(450);
                    SetManagedChromeVisible(false);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private IntPtr TryGetManagedChromeWindowHandle()
        {
            try
            {
                if (_managedChromeProcessId <= 0)
                {
                    return IntPtr.Zero;
                }

                var process = Process.GetProcessById(_managedChromeProcessId);
                if (process == null || process.HasExited)
                {
                    return IntPtr.Zero;
                }

                for (var i = 0; i < 12; i++)
                {
                    process.Refresh();
                    var handle = process.MainWindowHandle;
                    if (handle != IntPtr.Zero)
                    {
                        return handle;
                    }

                    Thread.Sleep(150);
                }
            }
            catch
            {
            }

            return IntPtr.Zero;
        }

        private string ResolveChromePath()
        {
            var candidates = new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\Application\chrome.exe")
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var path = candidates[i];
                if (!string.IsNullOrWhiteSpace(path) && System.IO.File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        private async Task<bool> EnsureProviderTabAsync(AiProvider provider)
        {
            if (provider == AiProvider.Unknown) return false;

            var tabs = await GetTabsAsync();
            if (tabs.Any(t => DetectProvider(GetTabTitle(t), GetTabUrl(t)) == provider)) return true;

            var url = GetProviderStartUrl(provider);
            var created = await CreateTabAsync(url);
            if (created)
            {
                Log("Opened background tab for " + ProviderToServiceName(provider) + ".");
            }
            return created;
        }

        private async Task<List<dynamic>> EnsureAnyAiTabAsync(AiProvider preferredProvider)
        {
            var providers = new List<AiProvider>();
            if (preferredProvider != AiProvider.Unknown)
            {
                providers.Add(preferredProvider);
            }

            if (!providers.Contains(AiProvider.ChatGpt)) providers.Add(AiProvider.ChatGpt);
            if (!providers.Contains(AiProvider.Gemini)) providers.Add(AiProvider.Gemini);
            if (!providers.Contains(AiProvider.Claude)) providers.Add(AiProvider.Claude);

            for (var attempt = 0; attempt < 3; attempt++)
            {
                foreach (var provider in providers)
                {
                    await EnsureProviderTabAsync(provider);
                }

                await Task.Delay(300 + (attempt * 250));

                var tabs = await GetTabsAsync();
                var aiTabs = tabs.Where(IsAiTab).ToList();
                if (aiTabs.Count > 0)
                {
                    return aiTabs;
                }

                if (attempt >= 1)
                {
                    foreach (var provider in providers)
                    {
                        await TryCreateProviderFallbackTabAsync(provider);
                    }
                }
            }

            return new List<dynamic>();
        }

        private async Task TryCreateProviderFallbackTabAsync(AiProvider provider)
        {
            foreach (var url in GetProviderFallbackUrls(provider))
            {
                var created = await CreateTabAsync(url);
                if (created)
                {
                    Log("Opened fallback tab for " + ProviderToServiceName(provider) + " at " + url + ".");
                    return;
                }
            }
        }

        private List<string> GetProviderFallbackUrls(AiProvider provider)
        {
            if (provider == AiProvider.ChatGpt)
            {
                return new List<string>
                {
                    ChatGptStartUrl,
                    "https://chat.openai.com/"
                };
            }

            if (provider == AiProvider.Gemini)
            {
                return new List<string>
                {
                    GeminiStartUrl,
                    "https://gemini.google.com/app"
                };
            }

            if (provider == AiProvider.Claude)
            {
                return new List<string>
                {
                    ClaudeStartUrl,
                    "https://claude.ai/"
                };
            }

            return new List<string>
            {
                ChatGptStartUrl,
                GeminiStartUrl,
                ClaudeStartUrl
            };
        }

        private string GetProviderStartUrl(AiProvider provider)
        {
            switch (provider)
            {
                case AiProvider.ChatGpt:
                    return ChatGptStartUrl;
                case AiProvider.Gemini:
                    return GeminiStartUrl;
                case AiProvider.Claude:
                    return ClaudeStartUrl;
                default:
                    return ChatGptStartUrl;
            }
        }

        private async Task<bool> CreateTabAsync(string url)
        {
            try
            {
                var encoded = Uri.EscapeDataString(url ?? string.Empty);
                var endpoint = DebugBase + "/json/new?" + encoded;

                try
                {
                    using (var put = new HttpRequestMessage(HttpMethod.Put, endpoint))
                    using (var putResp = await _http.SendAsync(put))
                    {
                        if (putResp.IsSuccessStatusCode) return true;
                    }
                }
                catch
                {
                }

                using (var getResp = await _http.GetAsync(endpoint))
                {
                    return getResp.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private string GetTabTitle(dynamic tab)
        {
            try { return tab["title"] as string ?? string.Empty; } catch { return string.Empty; }
        }

        private string GetTabId(dynamic tab)
        {
            try { return tab["id"] as string ?? string.Empty; } catch { return string.Empty; }
        }

        private string GetTabType(dynamic tab)
        {
            try { return tab["type"] as string ?? string.Empty; } catch { return string.Empty; }
        }

        private string GetTabUrl(dynamic tab)
        {
            try { return tab["url"] as string ?? string.Empty; } catch { return string.Empty; }
        }

        private string GetWebSocketDebuggerUrl(dynamic tab)
        {
            try { return tab["webSocketDebuggerUrl"] as string ?? string.Empty; } catch { return string.Empty; }
        }

        private async Task<bool> ActivateTabAsync(dynamic tab)
        {
            var id = GetTabId(tab);
            if (string.IsNullOrWhiteSpace(id)) return false;

            try
            {
                using (var resp = await _http.GetAsync(DebugBase + "/json/activate/" + Uri.EscapeDataString(id)))
                {
                    return resp.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private AiProvider DetectProvider(string title, string url)
        {
            var t = (title ?? string.Empty).ToLowerInvariant();
            var u = (url ?? string.Empty).ToLowerInvariant();

            if (u.Contains("chatgpt.com") || t.Contains("chatgpt")) return AiProvider.ChatGpt;
            if (u.Contains("gemini.google.com") || t.Contains("gemini")) return AiProvider.Gemini;
            if (u.Contains("claude.ai") || t.Contains("claude")) return AiProvider.Claude;
            return AiProvider.Unknown;
        }

        private AiProvider ParsePreferredProvider(string preferredService)
        {
            var s = (preferredService ?? string.Empty).Trim().ToLowerInvariant();
            if (s.Contains("chatgpt") || s.Contains("openai")) return AiProvider.ChatGpt;
            if (s.Contains("gemini") || s.Contains("google")) return AiProvider.Gemini;
            if (s.Contains("claude") || s.Contains("anthropic")) return AiProvider.Claude;
            return AiProvider.Unknown;
        }

        private string ProviderToServiceName(AiProvider provider)
        {
            switch (provider)
            {
                case AiProvider.ChatGpt: return "ChatGPT";
                case AiProvider.Gemini: return "Gemini";
                case AiProvider.Claude: return "Claude";
                default: return "Unknown";
            }
        }

        private string DetectModelName(string title, AiProvider provider)
        {
            var t = (title ?? string.Empty).Trim();
            var lower = t.ToLowerInvariant();

            if (lower.Contains("gpt-5")) return "GPT-5";
            if (lower.Contains("gpt-4.1")) return "GPT-4.1";
            if (lower.Contains("gpt-4")) return "GPT-4";
            if (lower.Contains("o3")) return "o3";
            if (lower.Contains("o1")) return "o1";
            if (lower.Contains("gemini 2.5 pro")) return "Gemini 2.5 Pro";
            if (lower.Contains("gemini 2.5 flash")) return "Gemini 2.5 Flash";
            if (lower.Contains("gemini 2.0")) return "Gemini 2.0";
            if (lower.Contains("claude 3.5 sonnet")) return "Claude 3.5 Sonnet";
            if (lower.Contains("claude 3.7 sonnet")) return "Claude 3.7 Sonnet";
            if (lower.Contains("claude sonnet 4")) return "Claude Sonnet 4";
            if (lower.Contains("claude opus")) return "Claude Opus";

            switch (provider)
            {
                case AiProvider.ChatGpt: return "Auto";
                case AiProvider.Gemini: return "Auto";
                case AiProvider.Claude: return "Auto";
                default: return "Unknown";
            }
        }

        private string BuildSourceLabel(string service, string model)
        {
            var s = string.IsNullOrWhiteSpace(service) ? "Unknown" : service.Trim();
            var m = string.IsNullOrWhiteSpace(model) ? "Auto" : model.Trim();
            return s + " (" + m + ")";
        }

        private string EscapeJsString(string input)
        {
            return (input ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", string.Empty)
                .Replace("\n", "\\n");
        }

        private bool IsError(string value)
        {
            return !string.IsNullOrEmpty(value) && value.StartsWith("error:", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsNoSendStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            return v.IndexOf("no-send", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsPendingSendStatus(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            var v = value.Trim();
            return v.IndexOf("pending", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private async Task<string> ResolvePendingSendStatusAsync(AiProvider provider, string injectResult)
        {
            if (provider != AiProvider.Gemini && provider != AiProvider.Claude && provider != AiProvider.ChatGpt)
            {
                return injectResult;
            }

            var current = injectResult ?? string.Empty;
            var shouldPoll = IsPendingSendStatus(current) || string.Equals(current.Trim(), "ok", StringComparison.OrdinalIgnoreCase);
            if (!shouldPoll)
            {
                return current;
            }

            for (var i = 0; i < 12; i++)
            {
                await Task.Delay(150);
                var status = (await EvaluateJsAsync("(function(){return window.__cdts_last_send_status||'';})();", 2000) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(status))
                {
                    continue;
                }

                if (IsNoSendStatus(status))
                {
                    return "ok:no-send";
                }

                if (status.IndexOf("sent", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "ok:" + status;
                }
            }

            return current;
        }

        private bool LooksLikeThinkingState(string value)
        {
            var v = (value ?? string.Empty).ToLowerInvariant();
            return v.Contains("thinking") || v.Contains("generating") || v.Contains("drafting") || v.Contains("analyzing") || v.Contains("continue generating");
        }

        private bool IsLikelyAssistantContent(string value)
        {
            var v = (value ?? string.Empty).Trim();
            if (v.Length < 16) return false;

            var lower = v.ToLowerInvariant();
            if (lower == "you" || lower == "chatgpt" || lower == "gemini" || lower == "claude") return false;
            if (lower.StartsWith("you\n") || lower.StartsWith("chatgpt\n") || lower.StartsWith("gemini\n") || lower.StartsWith("claude\n")) return false;
            if (lower.StartsWith("return only ")) return false;
            if (lower.StartsWith("you are a ")) return false;
            if (lower.Contains("json data:")) return false;
            if (lower.Contains("no wrapper keys")) return false;
            if (lower.Contains("return only this exact token:")) return false;
            if (lower.Contains("cdts_rt_") && lower.Contains("return only")) return false;
            if (lower.Contains("cdts_json_start") && lower.Contains("return only")) return false;
            if (lower.Contains("please double-check responses") && lower.Contains("return only")) return false;

            return true;
        }

        private string BuildInjectScript(AiProvider provider, string escapedPrompt)
        {
            if (provider == AiProvider.Claude)
            {
                return "(function(){"
                     + "var p=\"" + escapedPrompt + "\";"
                     + "window.__cdts_last_send_status='pending';"
                     + "var box=document.querySelector('div[contenteditable=\"true\"][data-testid*=\"chat-input\"]')||document.querySelector('div[contenteditable=\"true\"][aria-label*=\"Talk to Claude\"]')||document.querySelector('div[contenteditable=\"true\"][aria-label*=\"Message Claude\"]')||document.querySelector('div[contenteditable=\"true\"][data-slate-editor=\"true\"]')||document.querySelector('div[contenteditable=\"true\"][role=\"textbox\"]')||document.querySelector('div.ProseMirror[contenteditable=\"true\"]')||document.querySelector('textarea');"
                     + "if(!box)return 'error: claude input not found';"
                     + "var getBoxText=function(){try{if((box.tagName||'').toLowerCase()==='textarea'){return (box.value||'').trim();}return (box.innerText||box.textContent||'').trim();}catch(e){return '';}};"
                     + "var beforeText=getBoxText();"
                     + "try{box.focus();}catch(e){}"
                     + "if((box.tagName||'').toLowerCase()==='textarea'){"
                     + "box.value='';box.value=p;box.dispatchEvent(new Event('input',{bubbles:true}));box.dispatchEvent(new Event('change',{bubbles:true}));"
                     + "}else{"
                     + "try{document.execCommand('selectAll',false,null);}catch(e){}"
                     + "var inserted=false;"
                     + "try{inserted=document.execCommand('insertText',false,p)===true;}catch(e){inserted=false;}"
                     + "if(!inserted){box.innerText=p;box.textContent=p;}"
                     + "try{box.dispatchEvent(new InputEvent('beforeinput',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){}"
                     + "try{box.dispatchEvent(new InputEvent('input',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){box.dispatchEvent(new Event('input',{bubbles:true}));}"
                     + "box.dispatchEvent(new Event('change',{bubbles:true}));"
                     + "}"
                     + "var afterInsertText=getBoxText();"
                     + "var inserted=afterInsertText.length>0 && afterInsertText!==beforeText;"
                     + "if(!inserted){window.__cdts_last_send_status='no-send';return 'ok:no-send';}"
                     + "var isStopLike=function(btn){"
                     + "if(!btn)return false;"
                     + "var al=((btn.getAttribute('aria-label')||'')+'').toLowerCase();"
                     + "var tt=((btn.getAttribute('title')||'')+'').toLowerCase();"
                     + "var dt=((btn.getAttribute('data-tooltip')||'')+'').toLowerCase();"
                     + "var tx=((btn.innerText||btn.textContent||'')+'').toLowerCase();"
                     + "return al.indexOf('stop')>=0||al.indexOf('cancel')>=0||tt.indexOf('stop')>=0||tt.indexOf('cancel')>=0||dt.indexOf('stop')>=0||dt.indexOf('cancel')>=0||tx.indexOf('stop')>=0||tx.indexOf('cancel')>=0;"
                     + "};"
                     + "var hasStopVisible=function(){"
                     + "var nodes=document.querySelectorAll('button,[role=\"button\"]');"
                     + "for(var i=0;i<nodes.length;i++){if(isStopLike(nodes[i])){return true;}}"
                     + "return false;"
                     + "};"
                     + "var findSend=function(){"
                     + "var sels=['button[aria-label*=\"Send Message\"]','button[aria-label*=\"Send message\"]','button[data-testid=\"send-button\"]','button[data-testid*=\"send\"]','button[data-testid*=\"submit\"]','button[title*=\"Send\"]','button[aria-label*=\"Send\"]','button[type=\"submit\"]'];"
                     + "for(var si=0;si<sels.length;si++){var nodes=document.querySelectorAll(sels[si]);for(var ni=0;ni<nodes.length;ni++){var n=nodes[ni];if(n && !n.disabled && n.getAttribute('aria-disabled')!=='true' && !isStopLike(n)){return n;}}}"
                     + "return null;"
                     + "};"
                     + "var trySend=function(attempt){"
                     + "var method='none';"
                     + "if(hasStopVisible()){window.__cdts_last_send_status='sent:stop-visible';return;}"
                     + "var send=findSend();"
                     + "if(send){"
                     + "try{send.click();method='button';}catch(e){}"
                     + "if(method==='none'){try{send.dispatchEvent(new MouseEvent('click',{view:window,bubbles:true,cancelable:true}));method='button';}catch(e){}}"
                     + "}"
                     + "if(method==='none'){"
                     + "try{var form=box.closest('form');if(form){if(form.requestSubmit){form.requestSubmit();method='form';}else if(form.submit){form.submit();method='form';}}}catch(e){}"
                     + "}"
                     + "if(method==='none'){"
                     + "var target=document.activeElement||box;"
                     + "try{target.focus();}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keypress',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                     + "if(method==='none'){"
                     + "try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,ctrlKey:true,bubbles:true}));}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,ctrlKey:true,bubbles:true}));}catch(e){}"
                     + "}"
                     + "method='keyboard';"
                     + "}"
                     + "setTimeout(function(){"
                     + "var afterSendText=getBoxText();"
                     + "var send2=findSend();"
                     + "var sendStillEnabled=!!send2;"
                     + "var probablySent=(afterSendText.length===0 || (afterSendText!==afterInsertText) || !sendStillEnabled || hasStopVisible());"
                     + "if(probablySent){window.__cdts_last_send_status='sent:'+method;return;}"
                     + "if(attempt>=10){window.__cdts_last_send_status='no-send';return;}"
                     + "setTimeout(function(){trySend(attempt+1);},120);"
                     + "},140);"
                     + "};"
                     + "setTimeout(function(){trySend(0);},80);"
                     + "return 'ok:pending';"
                     + "})();";
            }

            if (provider == AiProvider.Gemini)
            {
                return "(function(){"
                     + "var p=\"" + escapedPrompt + "\";"
                     + "window.__cdts_last_send_status='pending';"
                     + "var box=document.querySelector('div[contenteditable=\"true\"][aria-label*=\"Enter a prompt\"]')||document.querySelector('div[contenteditable=\"true\"][aria-label*=\"prompt\"]')||document.querySelector('div.ql-editor[contenteditable=\"true\"]')||document.querySelector('rich-textarea div[contenteditable=\"true\"]')||document.querySelector('div[contenteditable=\"true\"]')||document.querySelector('textarea');"
                     + "if(!box)return 'error: gemini input not found';"
                     + "var getBoxText=function(){try{if((box.tagName||'').toLowerCase()==='textarea'){return (box.value||'').trim();}return (box.innerText||box.textContent||'').trim();}catch(e){return '';}};"
                     + "var beforeText=getBoxText();"
                     + "try{box.focus();}catch(e){}"
                     + "if((box.tagName||'').toLowerCase()==='textarea'){"
                     + "box.value='';box.value=p;box.dispatchEvent(new Event('input',{bubbles:true}));box.dispatchEvent(new Event('change',{bubbles:true}));"
                     + "}else{"
                     + "try{document.execCommand('selectAll',false,null);}catch(e){}"
                     + "var inserted=false;"
                     + "try{inserted=document.execCommand('insertText',false,p)===true;}catch(e){inserted=false;}"
                     + "if(!inserted){box.innerText=p;box.textContent=p;}"
                     + "try{box.dispatchEvent(new InputEvent('beforeinput',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){}"
                     + "try{box.dispatchEvent(new InputEvent('input',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){box.dispatchEvent(new Event('input',{bubbles:true}));}"
                     + "box.dispatchEvent(new Event('change',{bubbles:true}));"
                     + "}"
                     + "var afterInsertText=getBoxText();"
                     + "var inserted=afterInsertText.length>0 && afterInsertText!==beforeText;"
                     + "if(!inserted){window.__cdts_last_send_status='no-send';return 'ok:no-send';}"
                     + "var isStopLike=function(btn){"
                     + "if(!btn)return false;"
                     + "var al=((btn.getAttribute('aria-label')||'')+'').toLowerCase();"
                     + "var tt=((btn.getAttribute('title')||'')+'').toLowerCase();"
                     + "var dt=((btn.getAttribute('data-tooltip')||'')+'').toLowerCase();"
                     + "var tx=((btn.innerText||btn.textContent||'')+'').toLowerCase();"
                     + "return al.indexOf('stop')>=0||al.indexOf('cancel')>=0||tt.indexOf('stop')>=0||tt.indexOf('cancel')>=0||dt.indexOf('stop')>=0||dt.indexOf('cancel')>=0||tx.indexOf('stop')>=0||tx.indexOf('cancel')>=0;"
                     + "};"
                     + "var hasStopVisible=function(){"
                     + "var nodes=document.querySelectorAll('button,[role=\"button\"]');"
                     + "for(var i=0;i<nodes.length;i++){if(isStopLike(nodes[i])){return true;}}"
                     + "return false;"
                     + "};"
                     + "var findSend=function(){"
                     + "var sels=['button[aria-label*=\"Send\"]','button[aria-label*=\"send\"]','button[data-test-id*=\"send\"]','button[data-testid*=\"send\"]','button[mattooltip*=\"Send\"]','button[mattooltip*=\"send\"]','button[aria-label*=\"Submit\"]','button[type=\"submit\"]'];"
                     + "for(var si=0;si<sels.length;si++){var nodes=document.querySelectorAll(sels[si]);for(var ni=0;ni<nodes.length;ni++){var n=nodes[ni];if(n && !n.disabled && n.getAttribute('aria-disabled')!=='true' && !isStopLike(n)){return n;}}}"
                     + "return null;"
                     + "};"
                     + "var trySend=function(attempt){"
                     + "var method='none';"
                     + "if(hasStopVisible()){window.__cdts_last_send_status='sent:stop-visible';return;}"
                     + "var send=findSend();"
                     + "if(send){"
                     + "try{send.click();method='button';}catch(e){}"
                     + "if(method==='none'){try{send.dispatchEvent(new MouseEvent('click',{view:window,bubbles:true,cancelable:true}));method='button';}catch(e){}}"
                     + "}"
                     + "if(method==='none'){"
                     + "try{var form=box.closest('form');if(form){if(form.requestSubmit){form.requestSubmit();method='form';}else if(form.submit){form.submit();method='form';}}}catch(e){}"
                     + "}"
                     + "if(method==='none'){"
                     + "var target=document.activeElement||box;"
                     + "try{target.focus();}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keypress',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                     + "if(method==='none'){"
                     + "try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,ctrlKey:true,bubbles:true}));}catch(e){}"
                     + "try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,ctrlKey:true,bubbles:true}));}catch(e){}"
                     + "}"
                     + "method='keyboard';"
                     + "}"
                     + "setTimeout(function(){"
                     + "var afterSendText=getBoxText();"
                     + "var send2=findSend();"
                     + "var sendStillEnabled=!!send2;"
                     + "var probablySent=(afterSendText.length===0 || (afterSendText!==afterInsertText) || !sendStillEnabled || hasStopVisible());"
                     + "if(probablySent){window.__cdts_last_send_status='sent:'+method;return;}"
                     + "if(attempt>=10){window.__cdts_last_send_status='no-send';return;}"
                     + "setTimeout(function(){trySend(attempt+1);},130);"
                     + "},160);"
                     + "};"
                     + "setTimeout(function(){trySend(0);},80);"
                     + "return 'ok:pending';"
                     + "})();";
            }

            return "(function(){"
                 + "var p=\"" + escapedPrompt + "\";"
                  + "window.__cdts_last_send_status='pending';"
                 + "var ta=document.querySelector('#prompt-textarea')||document.querySelector('textarea')||document.querySelector('div[contenteditable=\"true\"][id*=\"prompt\"]')||document.querySelector('div[contenteditable=\"true\"][data-testid*=\"composer\"]')||document.querySelector('div[contenteditable=\"true\"]');"
                 + "if(!ta)return 'error: chatgpt input not found';"
                  + "var getText=function(){try{if((ta.tagName||'').toLowerCase()==='textarea'){return (ta.value||'').trim();}return (ta.innerText||ta.textContent||'').trim();}catch(e){return '';}};"
                  + "var beforeText=getText();"
                 + "try{ta.focus();}catch(e){}"
                 + "var isTextArea=((ta.tagName||'').toLowerCase()==='textarea');"
                 + "if(isTextArea){"
                  + "ta.value='';ta.value=p;ta.dispatchEvent(new Event('input',{bubbles:true}));ta.dispatchEvent(new Event('change',{bubbles:true}));"
                 + "}else{"
                  + "try{document.execCommand('selectAll',false,null);}catch(e){}"
                  + "var inserted=false;"
                  + "try{inserted=document.execCommand('insertText',false,p)===true;}catch(e){inserted=false;}"
                  + "if(!inserted){ta.innerText=p;ta.textContent=p;}"
                  + "try{ta.dispatchEvent(new InputEvent('beforeinput',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){}"
                  + "try{ta.dispatchEvent(new InputEvent('input',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){ta.dispatchEvent(new Event('input',{bubbles:true}));}"
                  + "ta.dispatchEvent(new Event('change',{bubbles:true}));"
                 + "}"
                  + "var afterInsertText=getText();"
                  + "var inserted=afterInsertText.length>0 && afterInsertText!==beforeText;"
                  + "if(!inserted){window.__cdts_last_send_status='no-send';return 'ok:no-send';}"
                  + "var findSend=function(){"
                  + "var sels=['[data-testid=\"send-button\"]','button[data-testid*=\"send\"]','button[data-testid*=\"composer-send\"]','button[aria-label*=\"Send\"]','button[aria-label*=\"send\"]','button[type=\"submit\"]'];"
                  + "for(var si=0;si<sels.length;si++){var nodes=document.querySelectorAll(sels[si]);for(var ni=0;ni<nodes.length;ni++){var n=nodes[ni];if(n && !n.disabled && n.getAttribute('aria-disabled')!=='true'){return n;}}}"
                  + "return null;"
                  + "};"
                  + "var trySend=function(attempt){"
                  + "var method='none';"
                  + "var btn=findSend();"
                  + "if(btn){"
                  + "try{btn.click();method='button';}catch(e){}"
                  + "if(method==='none'){try{btn.dispatchEvent(new MouseEvent('click',{view:window,bubbles:true,cancelable:true}));method='button';}catch(e){}}"
                  + "}"
                  + "if(method==='none'){"
                  + "try{var form=ta.closest('form');if(form){if(form.requestSubmit){form.requestSubmit();method='form';}else if(form.submit){form.submit();method='form';}}}catch(e){}"
                  + "}"
                  + "if(method==='none'){"
                  + "var target=document.activeElement||ta;"
                  + "try{target.focus();}catch(e){}"
                  + "try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                  + "try{target.dispatchEvent(new KeyboardEvent('keypress',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                  + "try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}"
                  + "method='keyboard';"
                  + "}"
                  + "setTimeout(function(){"
                  + "var afterSendText=getText();"
                  + "var send2=findSend();"
                  + "var sendStillEnabled=!!send2;"
                  + "var probablySent=(afterSendText.length===0 || (afterSendText!==afterInsertText) || !sendStillEnabled);"
                  + "if(probablySent){window.__cdts_last_send_status='sent:'+method;return;}"
                  + "if(attempt>=10){window.__cdts_last_send_status='no-send';return;}"
                  + "setTimeout(function(){trySend(attempt+1);},120);"
                  + "},150);"
                  + "};"
                  + "setTimeout(function(){trySend(0);},80);"
                  + "return 'ok:pending';"
                 + "})();";
        }

        private string BuildReadScript(AiProvider provider)
        {
            if (provider == AiProvider.Claude)
            {
                return @"(function(){
                    var sels=[
                        '[data-testid=""assistant-turn""]',
                        '[data-message-author-role=""assistant""]',
                        'article[data-author*=""assistant""]',
                        '[data-is-assistant=""true""]'
                    ];
                    var textSels=['.prose','[data-testid*=""message-content""]','[data-testid*=""markdown""]','p','li','pre','code'];
                    var isPromptLike=function(t){
                        var x=(t||'').trim().toLowerCase();
                        if(!x)return true;
                        if(x.indexOf('return only ')===0)return true;
                        if(x.indexOf('return only this exact token:')>=0)return true;
                        if(x.indexOf('you are a ')===0)return true;
                        if(x.indexOf('json data:')>=0)return true;
                        if(x.indexOf('no wrapper keys')>=0)return true;
                        if(x.indexOf('cdts_rt_')>=0 && x.indexOf('return only')>=0)return true;
                        if(x.indexOf('cdts_json_start')>=0 && x.indexOf('return only')>=0)return true;
                        if(x.indexOf('please double-check responses')>=0 && x.indexOf('return only')>=0)return true;
                        return false;
                    };
                    var hasBlockedAncestor=function(node){
                        if(!node || !node.closest)return false;
                        var blocked=node.closest('form,textarea,[role=""textbox""],[contenteditable=""true""],[data-testid*=""chat-input""],[data-testid*=""composer""],[class*=""composer""],[class*=""input""]');
                        return !!blocked;
                    };
                    var readNodeText=function(node){
                        if(!node)return '';
                        var best='';
                        for(var ts=0;ts<textSels.length;ts++){
                            var parts=node.querySelectorAll(textSels[ts]);
                            for(var pi=0;pi<parts.length;pi++){
                                if(hasBlockedAncestor(parts[pi]))continue;
                                var part=(parts[pi].innerText||parts[pi].textContent||'').trim();
                                if(part.length>best.length)best=part;
                            }
                        }
                        if(best.length<16){
                            best=(node.innerText||node.textContent||'').trim();
                        }
                        return best;
                    };
                    var newest='';
                    for(var s=0;s<sels.length;s++){
                        var nodes=document.querySelectorAll(sels[s]);
                        for(var i=0;i<nodes.length;i++){
                            if(hasBlockedAncestor(nodes[i]))continue;
                            var txt=readNodeText(nodes[i]);
                            if(txt.length>15 && !isPromptLike(txt))newest=txt;
                        }
                    }
                    if(!newest){
                        var all=document.querySelectorAll('[data-testid*=""assistant-turn""],[data-message-author-role=""assistant""],article[data-author*=""assistant""],[data-is-assistant=""true""]');
                        for(var j=0;j<all.length;j++){
                            if(hasBlockedAncestor(all[j]))continue;
                            var t=readNodeText(all[j]);
                            if(t.length>15 && t.toLowerCase().indexOf('you said')<0 && !isPromptLike(t))newest=t;
                        }
                    }
                    if(!newest)return '';
                    return newest;
                })();";
            }

            if (provider == AiProvider.Gemini)
            {
                return @"(function(){
                    var candidates=[];
                    var sels=['message-content .markdown','message-content','.model-response-text','div.response-content','.response-content','.markdown','model-response message-content','div[data-message-author-role=""model""]'];
                    for(var s=0;s<sels.length;s++){
                        var nodes=document.querySelectorAll(sels[s]);
                        for(var i=0;i<nodes.length;i++){
                            var txt=(nodes[i].innerText||'').trim();
                            if(txt.length>8)candidates.push(txt);
                        }
                    }
                    if(candidates.length===0){
                        var all=document.querySelectorAll('article,[role=""article""],div');
                        for(var j=0;j<all.length;j++){
                            var t=(all[j].innerText||'').trim();
                            if(t.length>120)candidates.push(t);
                        }
                    }
                    if(candidates.length===0)return '';
                    return candidates[candidates.length-1];
                })();";
            }

            return @"(function(){
                var sels=[
                    '[data-testid=""conversation-turn-assistant""]',
                    '[data-message-author-role=""assistant""]',
                    'article[data-testid*=""conversation-turn""] [data-message-author-role=""assistant""]',
                    'article[data-testid*=""conversation-turn-assistant""]'
                ];
                var textSels=['.markdown','.prose','p','li','pre','code'];
                var isPromptLike=function(t){
                    var x=(t||'').trim().toLowerCase();
                    if(!x)return true;
                    if(x.indexOf('return only ')===0)return true;
                    if(x.indexOf('return only this exact token:')>=0)return true;
                    if(x.indexOf('you are a ')===0)return true;
                    if(x.indexOf('json data:')>=0)return true;
                    if(x.indexOf('no wrapper keys')>=0)return true;
                    if(x.indexOf('cdts_rt_')>=0 && x.indexOf('return only')>=0)return true;
                    if(x.indexOf('cdts_json_start')>=0 && x.indexOf('return only')>=0)return true;
                    if(x.indexOf('please double-check responses')>=0 && x.indexOf('return only')>=0)return true;
                    return false;
                };
                var hasBlockedAncestor=function(node){
                    if(!node || !node.closest)return false;
                    var blocked=node.closest('form,textarea,[role=""textbox""],[contenteditable=""true""],[data-testid*=""composer""],[data-testid*=""prompt""],[class*=""composer""],[class*=""input""]');
                    return !!blocked;
                };
                var readNodeText=function(node){
                    if(!node)return '';
                    var best='';
                    for(var ts=0;ts<textSels.length;ts++){
                        var parts=node.querySelectorAll(textSels[ts]);
                        for(var pi=0;pi<parts.length;pi++){
                            if(hasBlockedAncestor(parts[pi]))continue;
                            var part=(parts[pi].innerText||parts[pi].textContent||'').trim();
                            if(part.length>best.length)best=part;
                        }
                    }
                    if(best.length<16){
                        best=(node.innerText||node.textContent||'').trim();
                    }
                    return best;
                };
                var newest='';
                for(var s=0;s<sels.length;s++){
                    var nodes=document.querySelectorAll(sels[s]);
                    for(var i=0;i<nodes.length;i++){
                        if(hasBlockedAncestor(nodes[i]))continue;
                        var txt=readNodeText(nodes[i]);
                        if(txt.length>8 && !isPromptLike(txt))newest=txt;
                    }
                }
                if(!newest){
                    var all=document.querySelectorAll('main article,[data-testid*=""conversation-turn""],[data-message-author-role=""assistant""]');
                    for(var j=0;j<all.length;j++){
                        if(hasBlockedAncestor(all[j]))continue;
                        var t=readNodeText(all[j]);
                        if(t.length>8 && !isPromptLike(t))newest=t;
                    }
                }
                if(!newest)return '';
                return newest;
            })();";
        }

        private string BuildGenericReadScript()
        {
            return @"(function(){
                var candidates=[];
                var nodes=document.querySelectorAll('.markdown,[role=""article""],article,div');
                for(var i=0;i<nodes.length;i++){
                    var t=(nodes[i].innerText||'').trim();
                    if(t.length>80)candidates.push(t);
                }
                if(candidates.length===0)return '';
                return candidates[candidates.length-1];
            })();";
        }

        private string BuildStartFreshChatScript(AiProvider provider)
        {
            if (provider == AiProvider.Gemini)
            {
                return @"(function(){
                    try{
                        var clicked=false;
                        var sels=[
                            'a[href*=""/app""]',
                            'button[aria-label*=""New chat""]',
                            'button[aria-label*=""New conversation""]',
                            'button[data-test-id*=""new-chat""]',
                            '[data-test-id*=""new-chat""] button'
                        ];
                        for(var s=0;s<sels.length;s++){
                            var nodes=document.querySelectorAll(sels[s]);
                            for(var i=0;i<nodes.length;i++){
                                var n=nodes[i];
                                if(n && !n.disabled){ n.click(); clicked=true; }
                            }
                        }

                        var btns=document.querySelectorAll('button');
                        for(var b=0;b<btns.length;b++){
                            var txt=(btns[b].innerText||'').trim().toLowerCase();
                            if(txt==='ok' || txt==='got it' || txt==='accept' || txt==='continue'){
                                btns[b].click();
                            }
                        }
                        return clicked ? 'ok:new-chat' : 'ok:no-new-chat';
                    }catch(e){
                        return 'error: '+(e && e.message ? e.message : 'unknown');
                    }
                })();";
            }

            if (provider == AiProvider.Claude)
            {
                return @"(function(){
                    try{
                        var clicked=false;
                        var sels=[
                            'a[href*=""/new""]',
                            'button[aria-label*=""New chat""]',
                            'button[data-testid*=""new-chat""]'
                        ];
                        for(var s=0;s<sels.length;s++){
                            var nodes=document.querySelectorAll(sels[s]);
                            for(var i=0;i<nodes.length;i++){
                                var n=nodes[i];
                                if(n && !n.disabled){ n.click(); clicked=true; }
                            }
                        }
                        return clicked ? 'ok:new-chat' : 'ok:no-new-chat';
                    }catch(e){
                        return 'error: '+(e && e.message ? e.message : 'unknown');
                    }
                })();";
            }

            return @"(function(){return 'ok';})();";
        }

        private bool ContainsIgnoreCase(string value, string needle)
        {
            return (value ?? string.Empty).IndexOf(needle ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsActiveTab(dynamic tab)
        {
            try
            {
                if (tab == null) return false;
                var dict = tab as IDictionary<string, object>;
                if (dict == null || !dict.ContainsKey("active") || dict["active"] == null) return false;

                var activeObj = dict["active"];
                if (activeObj is bool) return (bool)activeObj;

                bool active;
                return bool.TryParse(activeObj.ToString(), out active) && active;
            }
            catch
            {
                return false;
            }
        }

        private void Log(string msg)
        {
            var fullMsg = "[ChromeSidecar] " + msg;
            Util.Log.Debug(fullMsg);
            OnLog?.Invoke(fullMsg);
        }

        public void Dispose()
        {
            MarkDisconnected("Chrome sidecar disposed.");
            StopReceivePump();
            _managedChromeProcessId = 0;
            try { _ws?.Dispose(); } catch { }
            try { _http?.Dispose(); } catch { }
            try { _sendLock?.Dispose(); } catch { }
            try { _queryLock?.Dispose(); } catch { }
        }
    }
}
