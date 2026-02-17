using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using CryptoDayTraderSuite.Util;

namespace CryptoDayTraderSuite.Services
{
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

		[CompilerGenerated]
		private sealed class _003C_003Ec__DisplayClass45_0
		{
			public ChromeSidecar _003C_003E4__this;

			public AiProvider preferredProvider;

			internal bool _003CConnectInternalAsync_003Eb__0(dynamic t)
			{
				return _003C_003E4__this.DetectProvider(_003C_003E4__this.GetTabTitle(t), _003C_003E4__this.GetTabUrl(t)) == preferredProvider;
			}
		}

		[CompilerGenerated]
		private sealed class _003CActivateTabAsync_003Ed__78 : IAsyncStateMachine
		{
			private static class _003C_003Eo__78
			{
				public static CallSite<Func<CallSite, object, object>> _003C_003Ep__0;

				public static CallSite<Func<CallSite, object, object>> _003C_003Ep__1;

				public static CallSite<Func<CallSite, object, bool>> _003C_003Ep__2;

				public static CallSite<Func<CallSite, object, object>> _003C_003Ep__3;
			}

			public int _003C_003E1__state;

			public AsyncTaskMethodBuilder<bool> _003C_003Et__builder;

			public object tab;

			public ChromeSidecar _003C_003E4__this;

			private object _003Cid_003E5__1;

			private object _003Cresp_003E5__2;

			private IDisposable _003C_003Es__3;

			private object _003C_003Es__4;

			private object _003C_003Eu__1;

			private void MoveNext()
			{
				int num = _003C_003E1__state;
				bool result;
				try
				{
					if (num == 0)
					{
						goto IL_0138;
					}
					_003Cid_003E5__1 = _003C_003E4__this.GetTabId((dynamic)tab);
					if (!(string.IsNullOrWhiteSpace((dynamic)_003Cid_003E5__1) ? true : false))
					{
						goto IL_0138;
					}
					result = false;
					goto end_IL_0007;
					IL_0138:
					try
					{
						dynamic val;
						if (num != 0)
						{
							val = _003C_003E4__this._http.GetAsync("http://localhost:9222/json/activate/" + Uri.EscapeDataString((dynamic)_003Cid_003E5__1)).GetAwaiter();
							if (!(bool)val.IsCompleted)
							{
								num = (_003C_003E1__state = 0);
								_003C_003Eu__1 = val;
								ICriticalNotifyCompletion awaiter = val as ICriticalNotifyCompletion;
								_003CActivateTabAsync_003Ed__78 stateMachine = this;
								if (awaiter == null)
								{
									INotifyCompletion awaiter2 = (INotifyCompletion)(object)val;
									_003C_003Et__builder.AwaitOnCompleted(ref awaiter2, ref stateMachine);
									awaiter2 = null;
								}
								else
								{
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
								}
								awaiter = null;
								return;
							}
						}
						else
						{
							val = _003C_003Eu__1;
							_003C_003Eu__1 = null;
							num = (_003C_003E1__state = -1);
						}
						_003C_003Es__4 = val.GetResult();
						_003Cresp_003E5__2 = _003C_003Es__4;
						_003C_003Es__4 = null;
						_003C_003Es__3 = (dynamic)_003Cresp_003E5__2;
						try
						{
							result = ((dynamic)_003Cresp_003E5__2).IsSuccessStatusCode;
						}
						finally
						{
							if (num < 0 && _003C_003Es__3 != null)
							{
								_003C_003Es__3.Dispose();
							}
						}
					}
					catch
					{
						result = false;
					}
					end_IL_0007:;
				}
				catch (Exception exception)
				{
					_003C_003E1__state = -2;
					_003Cid_003E5__1 = null;
					_003C_003Et__builder.SetException(exception);
					return;
				}
				_003C_003E1__state = -2;
				_003Cid_003E5__1 = null;
				_003C_003Et__builder.SetResult(result);
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[CompilerGenerated]
		private sealed class _003CConnectInternalAsync_003Ed__45 : IAsyncStateMachine
		{
			private static class _003C_003Eo__45
			{
				public static CallSite<Func<CallSite, object, object>> _003C_003Ep__0;

				public static CallSite<Func<CallSite, object, object>> _003C_003Ep__1;

				public static CallSite<Func<CallSite, object, bool>> _003C_003Ep__2;

				public static CallSite<Action<CallSite, object>> _003C_003Ep__3;
			}

			public int _003C_003E1__state;

			public AsyncTaskMethodBuilder<bool> _003C_003Et__builder;

			public AiProvider preferredProvider;

			public ChromeSidecar _003C_003E4__this;

			private _003C_003Ec__DisplayClass45_0 _003C_003E8__1;

			private List<object> _003Ctabs_003E5__2;

			private List<object> _003CaiTabs_003E5__3;

			private object _003Ctarget_003E5__4;

			private object _003CwsUrl_003E5__5;

			private List<object> _003C_003Es__6;

			private List<object> _003C_003Es__7;

			private List<object> _003C_003Es__8;

			private List<object> _003CpreferredTabs_003E5__9;

			private CancellationTokenSource _003CconnectCts_003E5__10;

			private Exception _003Cex_003E5__11;

			private TaskAwaiter<List<object>> _003C_003Eu__1;

			private TaskAwaiter<bool> _003C_003Eu__2;

			private object _003C_003Eu__3;

			private TaskAwaiter _003C_003Eu__4;

			private void MoveNext()
			{
				int num = _003C_003E1__state;
				bool result;
				try
				{
					if ((uint)num > 5u)
					{
						_003C_003E8__1 = new _003C_003Ec__DisplayClass45_0();
						_003C_003E8__1._003C_003E4__this = _003C_003E4__this;
						_003C_003E8__1.preferredProvider = preferredProvider;
						_003C_003E4__this.SetStatus(SidecarStatus.Connecting);
					}
					try
					{
						TaskAwaiter<List<object>> awaiter4;
						TaskAwaiter<bool> awaiter3;
						TaskAwaiter<List<object>> awaiter2;
						TaskAwaiter<List<object>> awaiter;
						dynamic val;
						switch (num)
						{
						default:
							_003C_003E4__this.StopReceivePump();
							try
							{
								_003C_003E4__this._ws?.Dispose();
							}
							catch
							{
							}
							_003C_003E4__this._ws = null;
							awaiter4 = _003C_003E4__this.GetTabsAsync().GetAwaiter();
							if (!awaiter4.IsCompleted)
							{
								num = (_003C_003E1__state = 0);
								_003C_003Eu__1 = awaiter4;
								_003CConnectInternalAsync_003Ed__45 stateMachine = this;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter4, ref stateMachine);
								return;
							}
							goto IL_011f;
						case 0:
							awaiter4 = _003C_003Eu__1;
							_003C_003Eu__1 = default(TaskAwaiter<List<object>>);
							num = (_003C_003E1__state = -1);
							goto IL_011f;
						case 1:
							awaiter3 = _003C_003Eu__2;
							_003C_003Eu__2 = default(TaskAwaiter<bool>);
							num = (_003C_003E1__state = -1);
							goto IL_01c2;
						case 2:
							awaiter2 = _003C_003Eu__1;
							_003C_003Eu__1 = default(TaskAwaiter<List<object>>);
							num = (_003C_003E1__state = -1);
							goto IL_022a;
						case 3:
							awaiter = _003C_003Eu__1;
							_003C_003Eu__1 = default(TaskAwaiter<List<object>>);
							num = (_003C_003E1__state = -1);
							goto IL_02f5;
						case 4:
							val = _003C_003Eu__3;
							_003C_003Eu__3 = null;
							num = (_003C_003E1__state = -1);
							goto IL_09f4;
						case 5:
							break;
							IL_022a:
							_003C_003Es__7 = awaiter2.GetResult();
							_003Ctabs_003E5__2 = _003C_003Es__7;
							_003C_003Es__7 = null;
							goto IL_024b;
							IL_024b:
							_003CaiTabs_003E5__3 = _003Ctabs_003E5__2.Where(_003C_003E4__this.IsAiTab).ToList();
							if (_003CaiTabs_003E5__3.Count == 0)
							{
								awaiter = _003C_003E4__this.EnsureAnyAiTabAsync(_003C_003E8__1.preferredProvider).GetAwaiter();
								if (!awaiter.IsCompleted)
								{
									num = (_003C_003E1__state = 3);
									_003C_003Eu__1 = awaiter;
									_003CConnectInternalAsync_003Ed__45 stateMachine = this;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);
									return;
								}
								goto IL_02f5;
							}
							goto IL_0354;
							IL_011f:
							_003C_003Es__6 = awaiter4.GetResult();
							_003Ctabs_003E5__2 = _003C_003Es__6;
							_003C_003Es__6 = null;
							if (_003C_003E8__1.preferredProvider != AiProvider.Unknown)
							{
								awaiter3 = _003C_003E4__this.EnsureProviderTabAsync(_003C_003E8__1.preferredProvider).GetAwaiter();
								if (!awaiter3.IsCompleted)
								{
									num = (_003C_003E1__state = 1);
									_003C_003Eu__2 = awaiter3;
									_003CConnectInternalAsync_003Ed__45 stateMachine = this;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter3, ref stateMachine);
									return;
								}
								goto IL_01c2;
							}
							goto IL_024b;
							IL_09f4:
							val.GetResult();
							_003C_003E4__this.Log("Connecting to " + _003C_003E4__this._targetTitle + " [" + _003C_003E4__this._provider.ToString() + "]...");
							_003C_003E4__this._ws = new ClientWebSocket();
							_003CconnectCts_003E5__10 = new CancellationTokenSource(TimeSpan.FromMilliseconds(12000.0));
							break;
							IL_0354:
							_003Ctarget_003E5__4 = null;
							if (_003C_003E8__1.preferredProvider != AiProvider.Unknown)
							{
								_003CpreferredTabs_003E5__9 = _003CaiTabs_003E5__3.Where((dynamic t) => _003C_003E8__1._003C_003E4__this.DetectProvider(_003C_003E8__1._003C_003E4__this.GetTabTitle(t), _003C_003E8__1._003C_003E4__this.GetTabUrl(t)) == _003C_003E8__1.preferredProvider).ToList();
								_003Ctarget_003E5__4 = _003CpreferredTabs_003E5__9.FirstOrDefault(_003C_003E4__this.IsActiveTab) ?? _003CpreferredTabs_003E5__9.FirstOrDefault();
								_003CpreferredTabs_003E5__9 = null;
							}
							if ((dynamic)_003Ctarget_003E5__4 == null)
							{
								_003Ctarget_003E5__4 = _003CaiTabs_003E5__3.FirstOrDefault(_003C_003E4__this.IsActiveTab) ?? _003CaiTabs_003E5__3.FirstOrDefault();
							}
							if ((dynamic)_003Ctarget_003E5__4 == null)
							{
								_003C_003E4__this.MarkDisconnected("No target AI tab could be selected.");
								result = false;
							}
							else
							{
								_003CwsUrl_003E5__5 = _003C_003E4__this.GetWebSocketDebuggerUrl((dynamic)_003Ctarget_003E5__4);
								if (!(string.IsNullOrEmpty((dynamic)_003CwsUrl_003E5__5) ? true : false))
								{
									_003C_003E4__this._targetTitle = _003C_003E4__this.GetTabTitle((dynamic)_003Ctarget_003E5__4);
									_003C_003E4__this._targetUrl = _003C_003E4__this.GetTabUrl((dynamic)_003Ctarget_003E5__4);
									_003C_003E4__this._provider = _003C_003E4__this.DetectProvider(_003C_003E4__this._targetTitle, _003C_003E4__this._targetUrl);
									val = _003C_003E4__this.ActivateTabAsync((dynamic)_003Ctarget_003E5__4).GetAwaiter();
									if (!(bool)val.IsCompleted)
									{
										num = (_003C_003E1__state = 4);
										_003C_003Eu__3 = val;
										ICriticalNotifyCompletion awaiter5 = val as ICriticalNotifyCompletion;
										_003CConnectInternalAsync_003Ed__45 stateMachine = this;
										if (awaiter5 == null)
										{
											INotifyCompletion awaiter6 = (INotifyCompletion)(object)val;
											_003C_003Et__builder.AwaitOnCompleted(ref awaiter6, ref stateMachine);
											awaiter6 = null;
										}
										else
										{
											_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter5, ref stateMachine);
										}
										awaiter5 = null;
										return;
									}
									goto IL_09f4;
								}
								_003C_003E4__this.Log("Target tab does not have a WebSocket URL.");
								_003C_003E4__this.MarkDisconnected("Target AI tab missing WebSocket debugger URL.");
								result = false;
							}
							goto end_IL_004b;
							IL_01c2:
							awaiter3.GetResult();
							awaiter2 = _003C_003E4__this.GetTabsAsync().GetAwaiter();
							if (!awaiter2.IsCompleted)
							{
								num = (_003C_003E1__state = 2);
								_003C_003Eu__1 = awaiter2;
								_003CConnectInternalAsync_003Ed__45 stateMachine = this;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref stateMachine);
								return;
							}
							goto IL_022a;
							IL_02f5:
							_003C_003Es__8 = awaiter.GetResult();
							_003CaiTabs_003E5__3 = _003C_003Es__8;
							_003C_003Es__8 = null;
							if (_003CaiTabs_003E5__3.Count != 0)
							{
								goto IL_0354;
							}
							_003C_003E4__this.Log("No AI tab found and automatic tab creation failed.");
							_003C_003E4__this.MarkDisconnected("No AI tab found after automatic creation attempt.");
							result = false;
							goto end_IL_004b;
						}
						try
						{
							TaskAwaiter awaiter7;
							if (num != 5)
							{
								awaiter7 = _003C_003E4__this._ws.ConnectAsync(new Uri((dynamic)_003CwsUrl_003E5__5), _003CconnectCts_003E5__10.Token).GetAwaiter();
								if (!awaiter7.IsCompleted)
								{
									num = (_003C_003E1__state = 5);
									_003C_003Eu__4 = awaiter7;
									_003CConnectInternalAsync_003Ed__45 stateMachine = this;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter7, ref stateMachine);
									return;
								}
							}
							else
							{
								awaiter7 = _003C_003Eu__4;
								_003C_003Eu__4 = default(TaskAwaiter);
								num = (_003C_003E1__state = -1);
							}
							awaiter7.GetResult();
						}
						finally
						{
							if (num < 0 && _003CconnectCts_003E5__10 != null)
							{
								((IDisposable)_003CconnectCts_003E5__10).Dispose();
							}
						}
						_003CconnectCts_003E5__10 = null;
						_003C_003E4__this.StartReceivePump();
						_003C_003E4__this.Log("Connected via CDP.");
						_003C_003E4__this.SetStatus(SidecarStatus.Connected);
						result = true;
						end_IL_004b:;
					}
					catch (Exception ex)
					{
						_003Cex_003E5__11 = ex;
						_003C_003E4__this.Log("Connection failed: " + _003Cex_003E5__11.Message);
						_003C_003E4__this.SetStatus(SidecarStatus.Error);
						result = false;
					}
				}
				catch (Exception ex)
				{
					_003C_003E1__state = -2;
					_003C_003E8__1 = null;
					_003C_003Et__builder.SetException(ex);
					return;
				}
				_003C_003E1__state = -2;
				_003C_003E8__1 = null;
				_003C_003Et__builder.SetResult(result);
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		private const string DebugBase = "http://localhost:9222";

		private const string DebugUrl = "http://localhost:9222/json";

		private const string ChatGptStartUrl = "https://chatgpt.com/";

		private const string GeminiStartUrl = "https://gemini.google.com/";

		private const string ClaudeStartUrl = "https://claude.ai/new";

		private const int ConnectTimeoutMs = 12000;

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

		public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;

		public SidecarStatus Status { get; private set; } = SidecarStatus.Disconnected;

		public string CurrentServiceName => ProviderToServiceName(_provider);

		public string CurrentModelName => DetectModelName(_targetTitle, _provider);

		public string CurrentSourceLabel => BuildSourceLabel(CurrentServiceName, CurrentModelName);

		public event Action<string> OnLog;

		public event Action<SidecarStatus> StatusChanged;

		public ChromeSidecar()
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Expected O, but got Unknown
			_http = new HttpClient();
			_http.Timeout = TimeSpan.FromMilliseconds(12000.0);
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
				this.StatusChanged?.Invoke(Status);
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
				try
				{
					_ws?.Dispose();
				}
				catch
				{
				}
				_ws = null;
				List<object> tabs = await GetTabsAsync();
				if (preferredProvider != AiProvider.Unknown)
				{
					await EnsureProviderTabAsync(preferredProvider);
					tabs = await GetTabsAsync();
				}
				List<object> aiTabs = tabs.Where(IsAiTab).ToList();
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
					List<object> preferredTabs = aiTabs.Where((dynamic t) => DetectProvider(GetTabTitle(t), GetTabUrl(t)) == preferredProvider).ToList();
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
				dynamic wsUrl = GetWebSocketDebuggerUrl(target);
				if (string.IsNullOrEmpty(wsUrl))
				{
					Log("Target tab does not have a WebSocket URL.");
					MarkDisconnected("Target AI tab missing WebSocket debugger URL.");
					return false;
				}
				_targetTitle = GetTabTitle(target);
				_targetUrl = GetTabUrl(target);
				_provider = DetectProvider(_targetTitle, _targetUrl);
				dynamic awaiter = ActivateTabAsync(target).GetAwaiter();
				if (!(bool)awaiter.IsCompleted)
				{
					ICriticalNotifyCompletion awaiter2 = awaiter as ICriticalNotifyCompletion;
					_003CConnectInternalAsync_003Ed__45 stateMachine = (_003CConnectInternalAsync_003Ed__45)/*Error near IL_09a0: stateMachine*/;
					AsyncTaskMethodBuilder<bool> asyncTaskMethodBuilder = default(AsyncTaskMethodBuilder<bool>);
					if (awaiter2 == null)
					{
						INotifyCompletion awaiter3 = (INotifyCompletion)(object)awaiter;
						asyncTaskMethodBuilder.AwaitOnCompleted(ref awaiter3, ref stateMachine);
					}
					else
					{
						asyncTaskMethodBuilder.AwaitUnsafeOnCompleted(ref awaiter2, ref stateMachine);
					}
					/*Error near IL_09d7: leave MoveNext - await not detected correctly*/;
				}
				awaiter.GetResult();
				Log("Connecting to " + _targetTitle + " [" + _provider.ToString() + "]...");
				_ws = new ClientWebSocket();
				using (CancellationTokenSource connectCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(12000.0)))
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
				Exception ex2 = ex;
				Log("Connection failed: " + ex2.Message);
				SetStatus(SidecarStatus.Error);
				return false;
			}
		}

		public async Task<List<string>> GetAvailableServicesAsync()
		{
			List<string> services = new List<string>();
			try
			{
				await EnsureProviderTabAsync(AiProvider.ChatGpt);
				await EnsureProviderTabAsync(AiProvider.Gemini);
				await EnsureProviderTabAsync(AiProvider.Claude);
				foreach (dynamic tab in await GetTabsAsync())
				{
					if (!((!IsAiTab(tab)) ? true : false))
					{
						dynamic provider = DetectProvider(GetTabTitle(tab), GetTabUrl(tab));
						dynamic service = ProviderToServiceName(provider);
						if (!string.IsNullOrWhiteSpace(service) && !services.Contains(service))
						{
							services.Add(service);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;
				Log("GetAvailableServices failed: " + ex2.Message);
			}
			return services;
		}

		public async Task<List<SidecarAiResponse>> QueryAcrossAvailableServicesAsync(string prompt)
		{
			List<SidecarAiResponse> results = new List<SidecarAiResponse>();
			List<string> services = new List<string> { "ChatGPT", "Gemini", "Claude" };
			foreach (string service in services)
			{
				try
				{
					if (!(await ConnectAsync(service)))
					{
						Log("Skipping " + service + ": unable to connect.");
						continue;
					}
					AiProvider preferredProvider = ParsePreferredProvider(service);
					string raw = await QueryAIAsync(prompt, preferredProvider);
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
			if (!IsConnected)
			{
				return string.Empty;
			}
			int id = Interlocked.Increment(ref _msgId);
			TaskCompletionSource<string> tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
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
			string request = _json.Serialize(command);
			if (!(await SendAsync(request, 3000)))
			{
				lock (_pendingLock)
				{
					_pendingResponses.Remove(id);
				}
				Log("EvaluateJsAsync send failed/timed out.");
				return "error: send-timeout";
			}
			if (await Task.WhenAny(tcs.Task, Task.Delay(Math.Max(1000, responseTimeoutMs))) != tcs.Task)
			{
				lock (_pendingLock)
				{
					_pendingResponses.Remove(id);
				}
				Log("EvaluateJsAsync timeout waiting for matching CDP response.");
				return string.Empty;
			}
			string responseJson = await tcs.Task;
			if (string.IsNullOrWhiteSpace(responseJson))
			{
				return string.Empty;
			}
			try
			{
				Dictionary<string, object> dict = _json.Deserialize<Dictionary<string, object>>(responseJson);
				if (dict?.ContainsKey("result") ?? false)
				{
					Dictionary<string, object> result = dict["result"] as Dictionary<string, object>;
					if (result?.ContainsKey("result") ?? false)
					{
						Dictionary<string, object> inner = result["result"] as Dictionary<string, object>;
						if (inner?.ContainsKey("value") ?? false)
						{
							return (inner["value"] == null) ? string.Empty : inner["value"].ToString();
						}
					}
				}
				if (dict?.ContainsKey("error") ?? false)
				{
					return "error: " + dict["error"];
				}
			}
			catch
			{
			}
			return responseJson;
		}

		public async Task<string> QueryAIAsync(string prompt)
		{
			return await QueryAIAsync(prompt, _provider, strictProvider: false);
		}

		private async Task<string> QueryAIAsync(string prompt, AiProvider preferredProvider)
		{
			return await QueryAIAsync(prompt, preferredProvider, preferredProvider != AiProvider.Unknown);
		}

		private async Task<string> QueryAIAsync(string prompt, AiProvider preferredProvider, bool strictProvider)
		{
			if (!IsConnected)
			{
				return string.Empty;
			}
			await _queryLock.WaitAsync();
			try
			{
				AiProvider primaryProvider = ((preferredProvider == AiProvider.Unknown) ? _provider : preferredProvider);
				if (primaryProvider == AiProvider.Unknown)
				{
					primaryProvider = AiProvider.ChatGpt;
				}
				string escapedPrompt = EscapeJsString(prompt ?? string.Empty);
				string baselineResponse = ((await EvaluateJsAsync(BuildReadScript(primaryProvider), 3500)) ?? string.Empty).Trim();
				if (primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude)
				{
					await Task.Delay(700);
				}
				string injectResult = string.Empty;
				for (int pass = 0; pass < 2; pass++)
				{
					injectResult = string.Empty;
					foreach (AiProvider provider in BuildProviderFallbackOrder(primaryProvider, strictProvider))
					{
						int timeoutMs = ((provider == AiProvider.Gemini || provider == AiProvider.Claude) ? 12000 : 8000);
						injectResult = await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), timeoutMs, 2, 400);
						injectResult = await ResolvePendingSendStatusAsync(provider, injectResult);
						if (!IsError(injectResult) && !IsNoSendStatus(injectResult) && !string.IsNullOrWhiteSpace(injectResult))
						{
							_provider = provider;
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
						string reconnectService = ProviderToServiceName(primaryProvider);
						await ConnectAsync(reconnectService);
						await Task.Delay((primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude) ? 900 : 400);
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
				string direct = await PollForModelResponseAsync(primaryProvider, baselineResponse, strictProvider);
				if (!string.IsNullOrWhiteSpace(direct))
				{
					_lastAiResponse = direct;
					Log("Model response captured.");
					return direct;
				}
				if (primaryProvider == AiProvider.Gemini || primaryProvider == AiProvider.Claude)
				{
					Log("Provider response stalled; attempting recovery (new chat / refresh / fresh tab)...");
					string recovered = await AttemptProviderRecoveryAsync(primaryProvider, escapedPrompt, strictProvider);
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
				return new List<AiProvider> { (primaryProvider == AiProvider.Unknown) ? AiProvider.ChatGpt : primaryProvider };
			}
			switch (primaryProvider)
			{
			case AiProvider.Claude:
				return new List<AiProvider>
				{
					AiProvider.Claude,
					AiProvider.ChatGpt,
					AiProvider.Gemini
				};
			case AiProvider.Gemini:
				return new List<AiProvider>
				{
					AiProvider.Gemini,
					AiProvider.ChatGpt,
					AiProvider.Claude
				};
			case AiProvider.ChatGpt:
				return new List<AiProvider>
				{
					AiProvider.ChatGpt,
					AiProvider.Gemini,
					AiProvider.Claude
				};
			default:
				return new List<AiProvider>
				{
					AiProvider.ChatGpt,
					AiProvider.Gemini,
					AiProvider.Claude
				};
			}
		}

		private async Task<string> ReadCandidateResponseAsync(AiProvider primaryProvider, string baselineResponse, bool allowBaselineMatch, bool strictProvider)
		{
			foreach (AiProvider provider in BuildProviderFallbackOrder(primaryProvider, strictProvider))
			{
				string candidate = ((await EvaluateJsAsync(BuildReadScript(provider), 2500)) ?? string.Empty).Trim();
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
			string generic = ((await EvaluateJsAsync(BuildGenericReadScript(), 2500)) ?? string.Empty).Trim();
			if (IsAcceptableResponseCandidate(generic, baselineResponse, allowBaselineMatch))
			{
				return generic;
			}
			return string.Empty;
		}

		private bool IsAcceptableResponseCandidate(string response, string baselineResponse, bool allowBaselineMatch)
		{
			if (string.IsNullOrWhiteSpace(response))
			{
				return false;
			}
			if (IsError(response))
			{
				Log("Read script returned error: " + response);
				return false;
			}
			if (LooksLikeThinkingState(response))
			{
				return false;
			}
			if (!IsLikelyAssistantContent(response))
			{
				return false;
			}
			if (!allowBaselineMatch && !string.IsNullOrWhiteSpace(baselineResponse) && string.Equals(response, baselineResponse, StringComparison.Ordinal))
			{
				return false;
			}
			return true;
		}

		private async Task<string> EvaluateJsWithRetryAsync(string script, int timeoutMs, int attempts, int retryDelayMs)
		{
			int tries = Math.Max(1, attempts);
			for (int attempt = 0; attempt < tries; attempt++)
			{
				string result = await EvaluateJsAsync(script, timeoutMs);
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
			int maxAttempts = ((provider == AiProvider.Gemini || provider == AiProvider.Claude) ? 32 : 20);
			int pollDelayMs = ((provider == AiProvider.Gemini || provider == AiProvider.Claude) ? 2000 : 1500);
			for (int i = 0; i < maxAttempts; i++)
			{
				await Task.Delay(pollDelayMs);
				Log("Polling model response attempt " + (i + 1));
				string response = await ReadCandidateResponseAsync(provider, baselineResponse, i >= 6, strictProvider);
				if (string.IsNullOrWhiteSpace(response))
				{
					continue;
				}
				if (string.Equals(response, bestCandidate, StringComparison.Ordinal))
				{
					stableCount++;
				}
				else
				{
					bestCandidate = response;
					stableCount = 1;
				}
				if (stableCount >= 2 || response.Length > 40)
				{
					await Task.Delay(1000);
					string confirm = await ReadCandidateResponseAsync(provider, baselineResponse, allowBaselineMatch: true, strictProvider);
					if (!string.IsNullOrWhiteSpace(confirm))
					{
						return confirm;
					}
				}
			}
			string generic = ((await EvaluateJsAsync(BuildGenericReadScript(), 3000)) ?? string.Empty).Trim();
			if (!string.IsNullOrWhiteSpace(generic) && IsLikelyAssistantContent(generic) && !LooksLikeThinkingState(generic))
			{
				Log("Model response captured via generic fallback selector.");
				return generic;
			}
			return string.Empty;
		}

		private async Task<string> AttemptProviderRecoveryAsync(AiProvider provider, string escapedPrompt, bool strictProvider)
		{
			if (provider != AiProvider.Gemini && provider != AiProvider.Claude)
			{
				return string.Empty;
			}
			string service = ProviderToServiceName(provider);
			try
			{
				await EvaluateJsAsync(BuildStartFreshChatScript(provider), 5000);
			}
			catch
			{
			}
			await Task.Delay((provider == AiProvider.Gemini) ? 1200 : 900);
			string baseline = ((await EvaluateJsAsync(BuildReadScript(provider), 3500)) ?? string.Empty).Trim();
			string inject = await ResolvePendingSendStatusAsync(provider, await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), 12000, 2, 400));
			if (!IsError(inject) && !IsNoSendStatus(inject) && !string.IsNullOrWhiteSpace(inject))
			{
				string recovered = await PollForModelResponseAsync(provider, baseline, strictProvider);
				if (!string.IsNullOrWhiteSpace(recovered))
				{
					return recovered;
				}
			}
			Log("Soft recovery did not produce a response; opening fresh provider tab...");
			try
			{
				string startUrl = GetProviderStartUrl(provider);
				if (!string.IsNullOrWhiteSpace(startUrl))
				{
					await CreateTabAsync(startUrl);
				}
			}
			catch
			{
			}
			await Task.Delay((provider == AiProvider.Gemini) ? 1800 : 1300);
			await ConnectAsync(service);
			await Task.Delay((provider == AiProvider.Gemini) ? 1200 : 900);
			baseline = ((await EvaluateJsAsync(BuildReadScript(provider), 3500)) ?? string.Empty).Trim();
			inject = await ResolvePendingSendStatusAsync(provider, await EvaluateJsWithRetryAsync(BuildInjectScript(provider, escapedPrompt), 12000, 2, 400));
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
			byte[] bytes = Encoding.UTF8.GetBytes(msg);
			try
			{
				Task sendTask = _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
				if (await Task.WhenAny(sendTask, Task.Delay(Math.Max(500, timeoutMs))) != sendTask)
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
			try
			{
				_receivePumpCts?.Dispose();
			}
			catch
			{
			}
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
			foreach (TaskCompletionSource<string> p in pending)
			{
				try
				{
					p.TrySetResult(string.Empty);
				}
				catch
				{
				}
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
					await Task.Delay(100, token).ConfigureAwait(continueOnCapturedContext: false);
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
				}
				else
				{
					if (!TryGetMessageId(responseJson, out var responseId))
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
					pending?.TrySetResult(responseJson);
				}
			}
		}

		private async Task<string> ReceiveAsync(int timeoutMs)
		{
			if (!IsConnected)
			{
				return string.Empty;
			}
			byte[] buffer = new byte[8192];
			StringBuilder sb = new StringBuilder();
			try
			{
				WebSocketReceiveResult result;
				do
				{
					Task<WebSocketReceiveResult> receiveTask = _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
					if (await Task.WhenAny(receiveTask, Task.Delay(Math.Max(500, timeoutMs))) != receiveTask)
					{
						if (!IsConnected && Status == SidecarStatus.Connected)
						{
							MarkDisconnected("CDP receive timed out after socket close.");
						}
						return string.Empty;
					}
					result = await receiveTask;
					sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
				}
				while (!result.EndOfMessage);
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
				Dictionary<string, object> dict = _json.Deserialize<Dictionary<string, object>>(json);
				if (dict == null || !dict.ContainsKey("id") || dict["id"] == null)
				{
					return false;
				}
				object val = dict["id"];
				if (val is int)
				{
					id = (int)val;
					return true;
				}
				if (val is long)
				{
					id = (int)(long)val;
					return true;
				}
				if (val is double)
				{
					id = (int)(double)val;
					return true;
				}
				if (int.TryParse(val.ToString(), out var parsed))
				{
					id = parsed;
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private bool IsAiTab(dynamic tab)
		{
			dynamic type = GetTabType(tab);
			if ((!string.Equals(type, "page", StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}
			dynamic title = GetTabTitle(tab);
			dynamic url = GetTabUrl(tab);
			return ContainsIgnoreCase(title, "ChatGPT") || ContainsIgnoreCase(title, "Gemini") || ContainsIgnoreCase(title, "Claude") || ContainsIgnoreCase(url, "chatgpt.com") || ContainsIgnoreCase(url, "gemini.google.com") || ContainsIgnoreCase(url, "claude.ai");
		}

		private async Task<List<dynamic>> GetTabsAsync()
		{
			try
			{
				string json = await _http.GetStringAsync("http://localhost:9222/json");
				return _json.Deserialize<List<object>>(json) ?? new List<object>();
			}
			catch
			{
				return new List<object>();
			}
		}

		private async Task<bool> EnsureProviderTabAsync(AiProvider provider)
		{
			if (provider == AiProvider.Unknown)
			{
				return false;
			}
			if ((await GetTabsAsync()).Any((dynamic t) => DetectProvider(GetTabTitle(t), GetTabUrl(t)) == provider))
			{
				return true;
			}
			string url = GetProviderStartUrl(provider);
			bool created = await CreateTabAsync(url);
			if (created)
			{
				Log("Opened background tab for " + ProviderToServiceName(provider) + ".");
			}
			return created;
		}

		private async Task<List<dynamic>> EnsureAnyAiTabAsync(AiProvider preferredProvider)
		{
			List<AiProvider> providers = new List<AiProvider>();
			if (preferredProvider != AiProvider.Unknown)
			{
				providers.Add(preferredProvider);
			}
			if (!providers.Contains(AiProvider.ChatGpt))
			{
				providers.Add(AiProvider.ChatGpt);
			}
			if (!providers.Contains(AiProvider.Gemini))
			{
				providers.Add(AiProvider.Gemini);
			}
			if (!providers.Contains(AiProvider.Claude))
			{
				providers.Add(AiProvider.Claude);
			}
			for (int attempt = 0; attempt < 3; attempt++)
			{
				foreach (AiProvider provider in providers)
				{
					await EnsureProviderTabAsync(provider);
				}
				await Task.Delay(300 + attempt * 250);
				List<object> aiTabs = (await GetTabsAsync()).Where(IsAiTab).ToList();
				if (aiTabs.Count > 0)
				{
					return aiTabs;
				}
				if (attempt < 1)
				{
					continue;
				}
				foreach (AiProvider provider2 in providers)
				{
					await TryCreateProviderFallbackTabAsync(provider2);
				}
			}
			return new List<object>();
		}

		private async Task TryCreateProviderFallbackTabAsync(AiProvider provider)
		{
			foreach (string url in GetProviderFallbackUrls(provider))
			{
				if (await CreateTabAsync(url))
				{
					Log("Opened fallback tab for " + ProviderToServiceName(provider) + " at " + url + ".");
					return;
				}
			}
		}

		private List<string> GetProviderFallbackUrls(AiProvider provider)
		{
			switch (provider)
			{
			case AiProvider.ChatGpt:
				return new List<string> { "https://chatgpt.com/", "https://chat.openai.com/" };
			case AiProvider.Gemini:
				return new List<string> { "https://gemini.google.com/", "https://gemini.google.com/app" };
			case AiProvider.Claude:
				return new List<string> { "https://claude.ai/new", "https://claude.ai/" };
			default:
				return new List<string> { "https://chatgpt.com/", "https://gemini.google.com/", "https://claude.ai/new" };
			}
		}

		private string GetProviderStartUrl(AiProvider provider)
		{
			switch (provider)
			{
			case AiProvider.ChatGpt:
				return "https://chatgpt.com/";
			case AiProvider.Gemini:
				return "https://gemini.google.com/";
			case AiProvider.Claude:
				return "https://claude.ai/new";
			default:
				return "https://chatgpt.com/";
			}
		}

		private async Task<bool> CreateTabAsync(string url)
		{
			try
			{
				string encoded = Uri.EscapeDataString(url ?? string.Empty);
				string endpoint = "http://localhost:9222/json/new?" + encoded;
				try
				{
					HttpRequestMessage put = new HttpRequestMessage(HttpMethod.Put, endpoint);
					try
					{
						HttpResponseMessage putResp = await _http.SendAsync(put);
						try
						{
							if (putResp.IsSuccessStatusCode)
							{
								return true;
							}
						}
						finally
						{
							((IDisposable)putResp)?.Dispose();
						}
					}
					finally
					{
						((IDisposable)put)?.Dispose();
					}
				}
				catch
				{
				}
				HttpResponseMessage getResp = await _http.GetAsync(endpoint);
				try
				{
					return getResp.IsSuccessStatusCode;
				}
				finally
				{
					((IDisposable)getResp)?.Dispose();
				}
			}
			catch
			{
				return false;
			}
		}

		private string GetTabTitle(dynamic tab)
		{
			try
			{
				return (tab["title"] as string) ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private string GetTabId(dynamic tab)
		{
			try
			{
				return (tab["id"] as string) ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private string GetTabType(dynamic tab)
		{
			try
			{
				return (tab["type"] as string) ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private string GetTabUrl(dynamic tab)
		{
			try
			{
				return (tab["url"] as string) ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private string GetWebSocketDebuggerUrl(dynamic tab)
		{
			try
			{
				return (tab["webSocketDebuggerUrl"] as string) ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private async Task<bool> ActivateTabAsync(dynamic tab)
		{
			dynamic id = GetTabId(tab);
			if (string.IsNullOrWhiteSpace(id))
			{
				return false;
			}
			try
			{
				dynamic awaiter = _http.GetAsync("http://localhost:9222/json/activate/" + Uri.EscapeDataString(id)).GetAwaiter();
				if (!(bool)awaiter.IsCompleted)
				{
					ICriticalNotifyCompletion awaiter2 = awaiter as ICriticalNotifyCompletion;
					_003CActivateTabAsync_003Ed__78 stateMachine = (_003CActivateTabAsync_003Ed__78)/*Error near IL_035b: stateMachine*/;
					AsyncTaskMethodBuilder<bool> asyncTaskMethodBuilder = default(AsyncTaskMethodBuilder<bool>);
					if (awaiter2 == null)
					{
						INotifyCompletion awaiter3 = (INotifyCompletion)(object)awaiter;
						asyncTaskMethodBuilder.AwaitOnCompleted(ref awaiter3, ref stateMachine);
					}
					else
					{
						asyncTaskMethodBuilder.AwaitUnsafeOnCompleted(ref awaiter2, ref stateMachine);
					}
					/*Error near IL_0392: leave MoveNext - await not detected correctly*/;
				}
				object result = awaiter.GetResult();
				dynamic resp = result;
				using ((IDisposable)resp)
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
			string t = (title ?? string.Empty).ToLowerInvariant();
			string u = (url ?? string.Empty).ToLowerInvariant();
			if (u.Contains("chatgpt.com") || t.Contains("chatgpt"))
			{
				return AiProvider.ChatGpt;
			}
			if (u.Contains("gemini.google.com") || t.Contains("gemini"))
			{
				return AiProvider.Gemini;
			}
			if (u.Contains("claude.ai") || t.Contains("claude"))
			{
				return AiProvider.Claude;
			}
			return AiProvider.Unknown;
		}

		private AiProvider ParsePreferredProvider(string preferredService)
		{
			string s = (preferredService ?? string.Empty).Trim().ToLowerInvariant();
			if (s.Contains("chatgpt") || s.Contains("openai"))
			{
				return AiProvider.ChatGpt;
			}
			if (s.Contains("gemini") || s.Contains("google"))
			{
				return AiProvider.Gemini;
			}
			if (s.Contains("claude") || s.Contains("anthropic"))
			{
				return AiProvider.Claude;
			}
			return AiProvider.Unknown;
		}

		private string ProviderToServiceName(AiProvider provider)
		{
			switch (provider)
			{
			case AiProvider.ChatGpt:
				return "ChatGPT";
			case AiProvider.Gemini:
				return "Gemini";
			case AiProvider.Claude:
				return "Claude";
			default:
				return "Unknown";
			}
		}

		private string DetectModelName(string title, AiProvider provider)
		{
			string t = (title ?? string.Empty).Trim();
			string lower = t.ToLowerInvariant();
			if (lower.Contains("gpt-5"))
			{
				return "GPT-5";
			}
			if (lower.Contains("gpt-4.1"))
			{
				return "GPT-4.1";
			}
			if (lower.Contains("gpt-4"))
			{
				return "GPT-4";
			}
			if (lower.Contains("o3"))
			{
				return "o3";
			}
			if (lower.Contains("o1"))
			{
				return "o1";
			}
			if (lower.Contains("gemini 2.5 pro"))
			{
				return "Gemini 2.5 Pro";
			}
			if (lower.Contains("gemini 2.5 flash"))
			{
				return "Gemini 2.5 Flash";
			}
			if (lower.Contains("gemini 2.0"))
			{
				return "Gemini 2.0";
			}
			if (lower.Contains("claude 3.5 sonnet"))
			{
				return "Claude 3.5 Sonnet";
			}
			if (lower.Contains("claude 3.7 sonnet"))
			{
				return "Claude 3.7 Sonnet";
			}
			if (lower.Contains("claude sonnet 4"))
			{
				return "Claude Sonnet 4";
			}
			if (lower.Contains("claude opus"))
			{
				return "Claude Opus";
			}
			switch (provider)
			{
			case AiProvider.ChatGpt:
				return "Auto";
			case AiProvider.Gemini:
				return "Auto";
			case AiProvider.Claude:
				return "Auto";
			default:
				return "Unknown";
			}
		}

		private string BuildSourceLabel(string service, string model)
		{
			string s = (string.IsNullOrWhiteSpace(service) ? "Unknown" : service.Trim());
			string m = (string.IsNullOrWhiteSpace(model) ? "Auto" : model.Trim());
			return s + " (" + m + ")";
		}

		private string EscapeJsString(string input)
		{
			return (input ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", string.Empty)
				.Replace("\n", "\\n");
		}

		private bool IsError(string value)
		{
			return !string.IsNullOrEmpty(value) && value.StartsWith("error:", StringComparison.OrdinalIgnoreCase);
		}

		private bool IsNoSendStatus(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
			string v = value.Trim();
			return v.IndexOf("no-send", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private bool IsPendingSendStatus(string value)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
			string v = value.Trim();
			return v.IndexOf("pending", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private async Task<string> ResolvePendingSendStatusAsync(AiProvider provider, string injectResult)
		{
			if (provider != AiProvider.Gemini && provider != AiProvider.Claude && provider != AiProvider.ChatGpt)
			{
				return injectResult;
			}
			string current = injectResult ?? string.Empty;
			if (!IsPendingSendStatus(current) && !string.Equals(current.Trim(), "ok", StringComparison.OrdinalIgnoreCase))
			{
				return current;
			}
			for (int i = 0; i < 12; i++)
			{
				await Task.Delay(150);
				string status = ((await EvaluateJsAsync("(function(){return window.__cdts_last_send_status||'';})();", 2000)) ?? string.Empty).Trim();
				if (!string.IsNullOrWhiteSpace(status))
				{
					if (IsNoSendStatus(status))
					{
						return "ok:no-send";
					}
					if (status.IndexOf("sent", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return "ok:" + status;
					}
				}
			}
			return current;
		}

		private bool LooksLikeThinkingState(string value)
		{
			string v = (value ?? string.Empty).ToLowerInvariant();
			return v.Contains("thinking") || v.Contains("generating") || v.Contains("drafting") || v.Contains("analyzing") || v.Contains("continue generating");
		}

		private bool IsLikelyAssistantContent(string value)
		{
			string v = (value ?? string.Empty).Trim();
			if (v.Length < 16)
			{
				return false;
			}
			string lower = v.ToLowerInvariant();
			switch (lower)
			{
			default:
				if (!(lower == "claude"))
				{
					if (lower.StartsWith("you\n") || lower.StartsWith("chatgpt\n") || lower.StartsWith("gemini\n") || lower.StartsWith("claude\n"))
					{
						return false;
					}
					return true;
				}
				goto case "you";
			case "you":
			case "chatgpt":
			case "gemini":
				return false;
			}
		}

		private string BuildInjectScript(AiProvider provider, string escapedPrompt)
		{
			switch (provider)
			{
			case AiProvider.Claude:
				return "(function(){var p=\"" + escapedPrompt + "\";window.__cdts_last_send_status='pending';var box=document.querySelector('div[contenteditable=\"true\"][data-testid*=\"chat-input\"]')||document.querySelector('div[contenteditable=\"true\"][aria-label*=\"Talk to Claude\"]')||document.querySelector('div[contenteditable=\"true\"][aria-label*=\"Message Claude\"]')||document.querySelector('div[contenteditable=\"true\"][data-slate-editor=\"true\"]')||document.querySelector('div[contenteditable=\"true\"][role=\"textbox\"]')||document.querySelector('div.ProseMirror[contenteditable=\"true\"]')||document.querySelector('textarea');if(!box)return 'error: claude input not found';var getBoxText=function(){try{if((box.tagName||'').toLowerCase()==='textarea'){return (box.value||'').trim();}return (box.innerText||box.textContent||'').trim();}catch(e){return '';}};var beforeText=getBoxText();try{box.focus();}catch(e){}if((box.tagName||'').toLowerCase()==='textarea'){box.value='';box.value=p;box.dispatchEvent(new Event('input',{bubbles:true}));box.dispatchEvent(new Event('change',{bubbles:true}));}else{try{document.execCommand('selectAll',false,null);}catch(e){}var inserted=false;try{inserted=document.execCommand('insertText',false,p)===true;}catch(e){inserted=false;}if(!inserted){box.innerText=p;box.textContent=p;}try{box.dispatchEvent(new InputEvent('beforeinput',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){}try{box.dispatchEvent(new InputEvent('input',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){box.dispatchEvent(new Event('input',{bubbles:true}));}box.dispatchEvent(new Event('change',{bubbles:true}));}var afterInsertText=getBoxText();var inserted=afterInsertText.length>0 && afterInsertText!==beforeText;if(!inserted){window.__cdts_last_send_status='no-send';return 'ok:no-send';}var findSend=function(){var sels=['button[aria-label*=\"Send Message\"]','button[aria-label*=\"Send message\"]','button[data-testid=\"send-button\"]','button[data-testid*=\"send\"]','button[data-testid*=\"submit\"]','button[title*=\"Send\"]','button[aria-label*=\"Send\"]','button[type=\"submit\"]'];for(var si=0;si<sels.length;si++){var nodes=document.querySelectorAll(sels[si]);for(var ni=0;ni<nodes.length;ni++){var n=nodes[ni];if(n && !n.disabled && n.getAttribute('aria-disabled')!=='true'){return n;}}}return null;};var trySend=function(attempt){var method='none';var send=findSend();if(send){try{send.click();method='button';}catch(e){}if(method==='none'){try{send.dispatchEvent(new MouseEvent('click',{view:window,bubbles:true,cancelable:true}));method='button';}catch(e){}}}if(method==='none'){try{var form=box.closest('form');if(form){if(form.requestSubmit){form.requestSubmit();method='form';}else if(form.submit){form.submit();method='form';}}}catch(e){}}if(method==='none'){var target=document.activeElement||box;try{target.focus();}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keypress',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}method='keyboard';}setTimeout(function(){var afterSendText=getBoxText();var send2=findSend();var sendStillEnabled=!!send2;var probablySent=(afterSendText.length===0 || (afterSendText!==afterInsertText) || !sendStillEnabled);if(probablySent){window.__cdts_last_send_status='sent:'+method;return;}if(attempt>=10){window.__cdts_last_send_status='no-send';return;}setTimeout(function(){trySend(attempt+1);},120);},140);};setTimeout(function(){trySend(0);},80);return 'ok:pending';})();";
			case AiProvider.Gemini:
				return "(function(){var p=\"" + escapedPrompt + "\";window.__cdts_last_send_status='pending';var box=document.querySelector('div[contenteditable=\"true\"][aria-label*=\"Enter a prompt\"]')||document.querySelector('div[contenteditable=\"true\"][aria-label*=\"prompt\"]')||document.querySelector('div.ql-editor[contenteditable=\"true\"]')||document.querySelector('rich-textarea div[contenteditable=\"true\"]')||document.querySelector('div[contenteditable=\"true\"]')||document.querySelector('textarea');if(!box)return 'error: gemini input not found';var getBoxText=function(){try{if((box.tagName||'').toLowerCase()==='textarea'){return (box.value||'').trim();}return (box.innerText||box.textContent||'').trim();}catch(e){return '';}};var beforeText=getBoxText();try{box.focus();}catch(e){}if((box.tagName||'').toLowerCase()==='textarea'){box.value='';box.value=p;box.dispatchEvent(new Event('input',{bubbles:true}));box.dispatchEvent(new Event('change',{bubbles:true}));}else{try{document.execCommand('selectAll',false,null);}catch(e){}var inserted=false;try{inserted=document.execCommand('insertText',false,p)===true;}catch(e){inserted=false;}if(!inserted){box.innerText=p;box.textContent=p;}try{box.dispatchEvent(new InputEvent('beforeinput',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){}try{box.dispatchEvent(new InputEvent('input',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){box.dispatchEvent(new Event('input',{bubbles:true}));}box.dispatchEvent(new Event('change',{bubbles:true}));}var afterInsertText=getBoxText();var inserted=afterInsertText.length>0 && afterInsertText!==beforeText;if(!inserted){window.__cdts_last_send_status='no-send';return 'ok:no-send';}var findSend=function(){var sels=['button[aria-label*=\"Send\"]','button[aria-label*=\"send\"]','button[data-test-id*=\"send\"]','button[data-testid*=\"send\"]','button[mattooltip*=\"Send\"]','button[mattooltip*=\"send\"]','button[aria-label*=\"Submit\"]','button[type=\"submit\"]'];for(var si=0;si<sels.length;si++){var nodes=document.querySelectorAll(sels[si]);for(var ni=0;ni<nodes.length;ni++){var n=nodes[ni];if(n && !n.disabled && n.getAttribute('aria-disabled')!=='true'){return n;}}}return null;};var trySend=function(attempt){var method='none';var send=findSend();if(send){try{send.click();method='button';}catch(e){}if(method==='none'){try{send.dispatchEvent(new MouseEvent('click',{view:window,bubbles:true,cancelable:true}));method='button';}catch(e){}}}if(method==='none'){try{var form=box.closest('form');if(form){if(form.requestSubmit){form.requestSubmit();method='form';}else if(form.submit){form.submit();method='form';}}}catch(e){}}if(method==='none'){var target=document.activeElement||box;try{target.focus();}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keypress',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}if(method==='none'){try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,ctrlKey:true,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,ctrlKey:true,bubbles:true}));}catch(e){}}method='keyboard';}setTimeout(function(){var afterSendText=getBoxText();var send2=findSend();var sendStillEnabled=!!send2;var probablySent=(afterSendText.length===0 || (afterSendText!==afterInsertText) || !sendStillEnabled);if(probablySent){window.__cdts_last_send_status='sent:'+method;return;}if(attempt>=10){window.__cdts_last_send_status='no-send';return;}setTimeout(function(){trySend(attempt+1);},130);},160);};setTimeout(function(){trySend(0);},80);return 'ok:pending';})();";
			default:
				return "(function(){var p=\"" + escapedPrompt + "\";window.__cdts_last_send_status='pending';var ta=document.querySelector('#prompt-textarea')||document.querySelector('textarea')||document.querySelector('div[contenteditable=\"true\"][id*=\"prompt\"]')||document.querySelector('div[contenteditable=\"true\"][data-testid*=\"composer\"]')||document.querySelector('div[contenteditable=\"true\"]');if(!ta)return 'error: chatgpt input not found';var getText=function(){try{if((ta.tagName||'').toLowerCase()==='textarea'){return (ta.value||'').trim();}return (ta.innerText||ta.textContent||'').trim();}catch(e){return '';}};var beforeText=getText();try{ta.focus();}catch(e){}var isTextArea=((ta.tagName||'').toLowerCase()==='textarea');if(isTextArea){ta.value='';ta.value=p;ta.dispatchEvent(new Event('input',{bubbles:true}));ta.dispatchEvent(new Event('change',{bubbles:true}));}else{try{document.execCommand('selectAll',false,null);}catch(e){}var inserted=false;try{inserted=document.execCommand('insertText',false,p)===true;}catch(e){inserted=false;}if(!inserted){ta.innerText=p;ta.textContent=p;}try{ta.dispatchEvent(new InputEvent('beforeinput',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){}try{ta.dispatchEvent(new InputEvent('input',{bubbles:true,data:p,inputType:'insertText'}));}catch(e){ta.dispatchEvent(new Event('input',{bubbles:true}));}ta.dispatchEvent(new Event('change',{bubbles:true}));}var afterInsertText=getText();var inserted=afterInsertText.length>0 && afterInsertText!==beforeText;if(!inserted){window.__cdts_last_send_status='no-send';return 'ok:no-send';}var findSend=function(){var sels=['[data-testid=\"send-button\"]','button[data-testid*=\"send\"]','button[data-testid*=\"composer-send\"]','button[aria-label*=\"Send\"]','button[aria-label*=\"send\"]','button[type=\"submit\"]'];for(var si=0;si<sels.length;si++){var nodes=document.querySelectorAll(sels[si]);for(var ni=0;ni<nodes.length;ni++){var n=nodes[ni];if(n && !n.disabled && n.getAttribute('aria-disabled')!=='true'){return n;}}}return null;};var trySend=function(attempt){var method='none';var btn=findSend();if(btn){try{btn.click();method='button';}catch(e){}if(method==='none'){try{btn.dispatchEvent(new MouseEvent('click',{view:window,bubbles:true,cancelable:true}));method='button';}catch(e){}}}if(method==='none'){try{var form=ta.closest('form');if(form){if(form.requestSubmit){form.requestSubmit();method='form';}else if(form.submit){form.submit();method='form';}}}catch(e){}}if(method==='none'){var target=document.activeElement||ta;try{target.focus();}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keypress',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}try{target.dispatchEvent(new KeyboardEvent('keyup',{key:'Enter',code:'Enter',keyCode:13,which:13,bubbles:true}));}catch(e){}method='keyboard';}setTimeout(function(){var afterSendText=getText();var send2=findSend();var sendStillEnabled=!!send2;var probablySent=(afterSendText.length===0 || (afterSendText!==afterInsertText) || !sendStillEnabled);if(probablySent){window.__cdts_last_send_status='sent:'+method;return;}if(attempt>=10){window.__cdts_last_send_status='no-send';return;}setTimeout(function(){trySend(attempt+1);},120);},150);};setTimeout(function(){trySend(0);},80);return 'ok:pending';})();";
			}
		}

		private string BuildReadScript(AiProvider provider)
		{
			switch (provider)
			{
			case AiProvider.Claude:
				return "(function(){\r\n                    var out=[];\r\n                    var sels=[\r\n                        '[data-testid=\"assistant-turn\"]',\r\n                        '[data-testid=\"assistant-turn\"] .prose',\r\n                        'div[data-testid*=\"assistant\"].prose',\r\n                        'article[data-testid*=\"assistant\"]',\r\n                        'main .prose',\r\n                        'main article'\r\n                    ];\r\n                    for(var s=0;s<sels.length;s++){\r\n                        var nodes=document.querySelectorAll(sels[s]);\r\n                        for(var i=0;i<nodes.length;i++){\r\n                            var txt=(nodes[i].innerText||'').trim();\r\n                            if(txt.length>8)out.push(txt);\r\n                        }\r\n                    }\r\n                    if(out.length===0){\r\n                        var all=document.querySelectorAll('article,div');\r\n                        for(var j=0;j<all.length;j++){\r\n                            var t=(all[j].innerText||'').trim();\r\n                            if(t.length>120 && t.toLowerCase().indexOf('you said')<0)out.push(t);\r\n                        }\r\n                    }\r\n                    if(out.length===0)return '';\r\n                    var dedup=[];\r\n                    for(var k=0;k<out.length;k++){\r\n                        if(dedup.indexOf(out[k])<0)dedup.push(out[k]);\r\n                    }\r\n                    return dedup[dedup.length-1];\r\n                })();";
			case AiProvider.Gemini:
				return "(function(){\r\n                    var candidates=[];\r\n                    var sels=['message-content .markdown','message-content','.model-response-text','div.response-content','.response-content','.markdown','model-response message-content','div[data-message-author-role=\"model\"]'];\r\n                    for(var s=0;s<sels.length;s++){\r\n                        var nodes=document.querySelectorAll(sels[s]);\r\n                        for(var i=0;i<nodes.length;i++){\r\n                            var txt=(nodes[i].innerText||'').trim();\r\n                            if(txt.length>8)candidates.push(txt);\r\n                        }\r\n                    }\r\n                    if(candidates.length===0){\r\n                        var all=document.querySelectorAll('article,[role=\"article\"],div');\r\n                        for(var j=0;j<all.length;j++){\r\n                            var t=(all[j].innerText||'').trim();\r\n                            if(t.length>120)candidates.push(t);\r\n                        }\r\n                    }\r\n                    if(candidates.length===0)return '';\r\n                    return candidates[candidates.length-1];\r\n                })();";
			default:
				return "(function(){\r\n                var out=[];\r\n                var sels=[\r\n                    '[data-message-author-role=\"assistant\"]',\r\n                    '[data-message-author-role=\"assistant\"] .markdown',\r\n                    '[data-message-author-role=\"assistant\"] .prose',\r\n                    '[data-testid=\"conversation-turn-assistant\"] .markdown',\r\n                    '[data-testid=\"conversation-turn-assistant\"] .prose',\r\n                    '[data-testid=\"conversation-turn-assistant\"]',\r\n                    'article[data-testid*=\"conversation-turn\"] [data-message-author-role=\"assistant\"] .markdown',\r\n                    'article[data-testid*=\"conversation-turn\"] [data-message-author-role=\"assistant\"]',\r\n                    'article[data-testid*=\"conversation-turn\"] .markdown',\r\n                    'article[data-testid*=\"conversation-turn\"] .prose',\r\n                    'main article .markdown',\r\n                    'main article .prose'\r\n                ];\r\n                for(var s=0;s<sels.length;s++){\r\n                    var nodes=document.querySelectorAll(sels[s]);\r\n                    for(var i=0;i<nodes.length;i++){\r\n                        var txt=(nodes[i].innerText||'').trim();\r\n                        if(txt.length>8)out.push(txt);\r\n                    }\r\n                }\r\n                if(out.length===0){\r\n                    var arts=document.querySelectorAll('main article,[data-message-author-role=\"assistant\"]');\r\n                    for(var j=0;j<arts.length;j++){\r\n                        var t=(arts[j].innerText||'').trim();\r\n                        if(t.length>0)out.push(t);\r\n                    }\r\n                }\r\n                if(out.length===0)return '';\r\n                var dedup=[];\r\n                for(var k=0;k<out.length;k++){\r\n                    if(dedup.indexOf(out[k])<0)dedup.push(out[k]);\r\n                }\r\n                return dedup[dedup.length-1];\r\n            })();";
			}
		}

		private string BuildGenericReadScript()
		{
			return "(function(){\r\n                var candidates=[];\r\n                var nodes=document.querySelectorAll('.markdown,[role=\"article\"],article,div');\r\n                for(var i=0;i<nodes.length;i++){\r\n                    var t=(nodes[i].innerText||'').trim();\r\n                    if(t.length>80)candidates.push(t);\r\n                }\r\n                if(candidates.length===0)return '';\r\n                return candidates[candidates.length-1];\r\n            })();";
		}

		private string BuildStartFreshChatScript(AiProvider provider)
		{
			switch (provider)
			{
			case AiProvider.Gemini:
				return "(function(){\r\n                    try{\r\n                        var clicked=false;\r\n                        var sels=[\r\n                            'a[href*=\"/app\"]',\r\n                            'button[aria-label*=\"New chat\"]',\r\n                            'button[aria-label*=\"New conversation\"]',\r\n                            'button[data-test-id*=\"new-chat\"]',\r\n                            '[data-test-id*=\"new-chat\"] button'\r\n                        ];\r\n                        for(var s=0;s<sels.length;s++){\r\n                            var nodes=document.querySelectorAll(sels[s]);\r\n                            for(var i=0;i<nodes.length;i++){\r\n                                var n=nodes[i];\r\n                                if(n && !n.disabled){ n.click(); clicked=true; }\r\n                            }\r\n                        }\r\n\r\n                        var btns=document.querySelectorAll('button');\r\n                        for(var b=0;b<btns.length;b++){\r\n                            var txt=(btns[b].innerText||'').trim().toLowerCase();\r\n                            if(txt==='ok' || txt==='got it' || txt==='accept' || txt==='continue'){\r\n                                btns[b].click();\r\n                            }\r\n                        }\r\n                        return clicked ? 'ok:new-chat' : 'ok:no-new-chat';\r\n                    }catch(e){\r\n                        return 'error: '+(e && e.message ? e.message : 'unknown');\r\n                    }\r\n                })();";
			case AiProvider.Claude:
				return "(function(){\r\n                    try{\r\n                        var clicked=false;\r\n                        var sels=[\r\n                            'a[href*=\"/new\"]',\r\n                            'button[aria-label*=\"New chat\"]',\r\n                            'button[data-testid*=\"new-chat\"]'\r\n                        ];\r\n                        for(var s=0;s<sels.length;s++){\r\n                            var nodes=document.querySelectorAll(sels[s]);\r\n                            for(var i=0;i<nodes.length;i++){\r\n                                var n=nodes[i];\r\n                                if(n && !n.disabled){ n.click(); clicked=true; }\r\n                            }\r\n                        }\r\n                        return clicked ? 'ok:new-chat' : 'ok:no-new-chat';\r\n                    }catch(e){\r\n                        return 'error: '+(e && e.message ? e.message : 'unknown');\r\n                    }\r\n                })();";
			default:
				return "(function(){return 'ok';})();";
			}
		}

		private bool ContainsIgnoreCase(string value, string needle)
		{
			return (value ?? string.Empty).IndexOf(needle ?? string.Empty, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private bool IsActiveTab(dynamic tab)
		{
			try
			{
				if (tab == null)
				{
					return false;
				}
				if (!(tab is IDictionary<string, object> dict) || !dict.ContainsKey("active") || dict["active"] == null)
				{
					return false;
				}
				object activeObj = dict["active"];
				bool active;
				if (!(activeObj is bool result))
				{
					return bool.TryParse(activeObj.ToString(), out active) && active;
				}
				return result;
			}
			catch
			{
				return false;
			}
		}

		private void Log(string msg)
		{
			string fullMsg = "[ChromeSidecar] " + msg;
			CryptoDayTraderSuite.Util.Log.Debug(fullMsg, "Log", "C:\\Users\\admin\\Documents\\Visual Studio 2022\\Projects\\CryptoDayTraderSuite\\Services\\ChromeSidecar.cs", 1650);
			this.OnLog?.Invoke(fullMsg);
		}

		public void Dispose()
		{
			MarkDisconnected("Chrome sidecar disposed.");
			StopReceivePump();
			try
			{
				_ws?.Dispose();
			}
			catch
			{
			}
			try
			{
				HttpClient http = _http;
				if (http != null)
				{
					((HttpMessageInvoker)http).Dispose();
				}
			}
			catch
			{
			}
			try
			{
				_sendLock?.Dispose();
			}
			catch
			{
			}
			try
			{
				_queryLock?.Dispose();
			}
			catch
			{
			}
		}
	}
}
