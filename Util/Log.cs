/* File: Util/Log.cs */
/* async rolling logger - .net 4.8 / c# 7.3 */
/* writes to %LocalAppData%\CryptoDayTraderSuite\logs */
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace CryptoDayTraderSuite.Util
{
    public enum LogLevel { Trace=0, Debug=1, Info=2, Warn=3, Error=4, Fatal=5 }

    public static class Log
    {
        private static readonly BlockingCollection<string> _q = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private static LogLevel _level = LogLevel.Info;
        private static string _dir;
        private static string _file;
        private static Task _writer;
        private static CancellationTokenSource _cts;

        public static event Action<string> OnLine; /* UI hook */

        public static void Init(LogLevel level)
        {
            _level = level;
            _dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CryptoDayTraderSuite", "logs");
            if (!Directory.Exists(_dir)) Directory.CreateDirectory(_dir);
            _file = NewFilePath();
            _cts = new CancellationTokenSource();
            _writer = Task.Factory.StartNew(() => WriterLoop(_cts.Token), TaskCreationOptions.LongRunning);
            Info("logging started: " + _file);
        }

        public static void Shutdown()
        {
            try { if (_cts != null) _cts.Cancel(); } catch { }
            try { if (_writer != null) _writer.Wait(1000); } catch { }
        }

        private static string NewFilePath()
        {
            var date = DateTime.UtcNow.ToString("yyyyMMdd");
            int i = 0;
            while (true)
            {
                var path = Path.Combine(_dir, "log_" + date + (i==0 ? "" : "_" + i) + ".txt");
                if (!File.Exists(path)) return path;
                i++;
            }
        }

        private static void WriterLoop(CancellationToken ct)
        {
            FileStream fs = null;
            StreamWriter sw = null;
            long max = 10 * 1024 * 1024; /* ~10MB */
            try
            {
                fs = new FileStream(_file, FileMode.Append, FileAccess.Write, FileShare.Read);
                sw = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
                while (!ct.IsCancellationRequested)
                {
                    string line;
                    if (!_q.TryTake(out line, 200))
                        continue;
                    sw.WriteLine(line);
                    if (fs.Length > max)
                    {
                        sw.Flush();
                        sw.Dispose(); fs.Dispose();
                        _file = NewFilePath();
                        fs = new FileStream(_file, FileMode.Append, FileAccess.Write, FileShare.Read);
                        sw = new StreamWriter(fs, new UTF8Encoding(false)) { AutoFlush = true };
                    }
                }
            }
            catch { }
            finally
            {
                try { if (sw != null) sw.Flush(); } catch { }
                try { if (sw != null) sw.Dispose(); } catch { }
                try { if (fs != null) fs.Dispose(); } catch { }
            }
        }
        private static void Enqueue(LogLevel lvl, string msg, Exception ex, string member, string filePath, int lineNum)
        {
            if (lvl < _level) return;
            var ts = DateTime.Now.ToString("HH:mm:ss");
            var cleanFile = Path.GetFileName(filePath);
            
            // Format: HH:mm:ss [LEVEL] [File:Line] [Member] Message
            var sb = new StringBuilder();
            sb.Append(ts);
            sb.Append(" [").Append(lvl.ToString().ToUpper()).Append("] ");
            sb.Append("[").Append(cleanFile).Append(":").Append(lineNum).Append("] ");
            sb.Append("[").Append(member).Append("] ");
            sb.Append(msg);

            if (ex != null)
            {
                sb.Append(Environment.NewLine).Append(ex.ToString());
            }

            var line = sb.ToString();

            // Debug/Console Output
            try 
            {
                System.Diagnostics.Debug.WriteLine(line);
                Console.WriteLine(line);
            } 
            catch { }

            try { _q.Add(line); } catch { }
            try { var h = OnLine; if (h != null) h(line); } catch { }
        }

        public static void SetLevel(LogLevel lvl) { _level = lvl; }

        public static void Trace(string m, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) 
        { Enqueue(LogLevel.Trace, m, null, member, file, line); }

        public static void Debug(string m, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) 
        { Enqueue(LogLevel.Debug, m, null, member, file, line); }

        public static void Info(string m, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) 
        { Enqueue(LogLevel.Info, m, null, member, file, line); }

        public static void Warn(string m, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) 
        { Enqueue(LogLevel.Warn, m, null, member, file, line); }

        public static void Error(string m, Exception ex = null, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) 
        { Enqueue(LogLevel.Error, m, ex, member, file, line); }

        public static void Fatal(string m, Exception ex = null, [CallerMemberName] string member = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) 
        { Enqueue(LogLevel.Fatal, m, ex, member, file, line); }
    }
}