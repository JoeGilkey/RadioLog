using System;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection;

namespace RadioLog.Common
{
    public class DebugHelper
    {
        static readonly int MAX_LOG_FILE_SIZE = 1024 * 1024 * 50; //50mb //52428800

        static System.Threading.Thread _tDebug = null;
        static ConcurrentQueue<string> _debugQueue = new ConcurrentQueue<string>();
        static readonly object _logLock = new object();
        static bool _initialInfoOutput = false;
        static bool _shouldRun = true;
        static bool _shouldDoConsoleOutput = false;
        static bool _shouldOutputTiming = false;
        static bool _shouldOutputNonErrorsToEventLog = false;

        private static string _logFileName = string.Empty;
        internal static string LogFileName
        {
            get
            {
                if (string.IsNullOrEmpty(_logFileName))
                {
                    _logFileName = System.IO.Path.Combine(AppSettings.Instance.AppDataDir, System.IO.Path.ChangeExtension(System.IO.Path.GetFileName(Assembly.GetEntryAssembly().Location), ".txt"));
                }
                return _logFileName;
            }
        }

        static DebugHelper()
        {
            CheckLogFileSize();

            _tDebug = new System.Threading.Thread(() =>
            {
                while (_shouldRun)
                {
                    int debugQCnt = _debugQueue.Count;
                    int iCnt = debugQCnt;
                    System.Text.StringBuilder sb = new StringBuilder();
                    if (debugQCnt > 0 && !_initialInfoOutput)
                    {
                        _initialInfoOutput = true;
                        DumpLogHelper.AppendDumpHeaderInfo(sb);
                    }
                    string str = string.Empty;
                    for (int i = 0; i < iCnt; i++)
                    {
                        if (_debugQueue.TryDequeue(out str))
                        {
                            sb.AppendLine(str);
                        }
                    }
                    if (iCnt > 0)
                    {
                        lock (_logLock)
                        {
                            System.IO.File.AppendAllText(LogFileName, sb.ToString());
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                }
            });
            _tDebug.IsBackground = true;

            _tDebug.Start();
        }
        ~DebugHelper()
        {
            _shouldRun = false;
            if (_tDebug != null && _tDebug.ThreadState == System.Threading.ThreadState.Running)
            {
                System.Threading.Thread.Sleep(500);
            }
        }

        public static void SetShouldDoConsoleOutput(bool bOn) { _shouldDoConsoleOutput = bOn; }
        public static void SetShouldDoTimingOutput(bool bOn) { _shouldOutputTiming = bOn; }
        public static void SetShouldOutputNonErrorsToEventLog(bool bOn) { _shouldOutputNonErrorsToEventLog = bOn; }

        static System.Diagnostics.EventLog _eventLog = null;
        public static void SetupEventLog(System.Diagnostics.EventLog eventLog) { _eventLog = eventLog; }
        public static void WriteToEventLog(string strMsg, System.Diagnostics.EventLogEntryType entryType)
        {
            try
            {
                if (_eventLog != null)
                {
                    if ((entryType == System.Diagnostics.EventLogEntryType.Information || entryType == System.Diagnostics.EventLogEntryType.SuccessAudit) && !_shouldOutputNonErrorsToEventLog)
                        return;
                    _eventLog.WriteEntry(strMsg, entryType);
                }
            }
            catch { }
        }

        public static void SleepUntilLogEmpty()
        {
            while (_debugQueue.Count > 0)
            {
                System.Threading.Thread.Sleep(50);
            }
        }
        public static void CheckLogFileSize()
        {
            try
            {
                System.IO.FileInfo fi = new System.IO.FileInfo(LogFileName);
                if (fi.Exists && fi.Length > MAX_LOG_FILE_SIZE)
                {
                    fi.Delete();
                    _debugQueue.Enqueue(string.Format("LOG FILE CLEARED *** FILE SIZE WAS {0}bytes", fi.Length));
                }
            }
            catch { }
        }

        public static long TimingSectionStart(string sectionName)
        {
            if (_shouldOutputTiming)
            {
                WriteLine("Timing Start: {0}", sectionName);
            }
            return DateTime.Now.Ticks;
        }
        public static void TimingSectionStop(string sectionName, long startTick)
        {
            if (_shouldOutputTiming)
            {
                TimeSpan ts = TimeSpan.FromTicks(DateTime.Now.Ticks - startTick);
                WriteLine("Timing End: {0} - {1}", sectionName, ts);
            }
        }
        public static void WriteLine()
        {
            if (_shouldDoConsoleOutput)
            {
                Console.WriteLine();
            }
        }
        private static void EnqueueDebugLogMessage(string msg)
        {
            DateTime dt = DateTime.Now;
            _debugQueue.Enqueue(string.Format("{0} - {1} | {2}", dt.ToShortDateString(), dt.ToLongTimeString(), msg));
        }
        internal static void WriteColorOutputToLog(ConsoleColor color, string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                EnqueueDebugLogMessage(string.Format("[{0}] - {1}", color, message));
        }
        public static void WriteLine(string value)
        {
            WriteToEventLog(value, System.Diagnostics.EventLogEntryType.Information);
            if (_shouldDoConsoleOutput)
            {
                Console.WriteLine(value);
            }
            EnqueueDebugLogMessage(value);
        }
        public static string WriteExceptionToLog(string codeSection, Exception ex, bool writeToDumpFile, string details = null, bool addDumpHeaderInfo=false)
        {
            try
            {
                StringBuilder sbError = new StringBuilder();
                sbError.AppendLine("**************************************************************");
                sbError.AppendLine(string.Format("{0} - {1} | Exception generated by {2} | {3}", DateTime.Now.ToShortDateString(), DateTime.Now.ToLongTimeString(), codeSection, DumpLogHelper.AppVersionInfo));
                if (!string.IsNullOrEmpty(details))
                    sbError.AppendLine(string.Format("Details: {0}", details));
                if (addDumpHeaderInfo)
                {
                    DumpLogHelper.AppendDumpHeaderInfo(sbError);
                }
                sbError.AppendLine(GetFullExceptionMessage(ex));
                sbError.AppendLine("**************************************************************");

                if (writeToDumpFile)
                {
                    DumpLogHelper.WriteExceptionToDumpLog(ex, codeSection, details);
                }

                _debugQueue.Enqueue(sbError.ToString());
                if (_shouldDoConsoleOutput)
                {
                    Console.WriteLine(sbError.ToString());
                }
                WriteToEventLog(sbError.ToString(), System.Diagnostics.EventLogEntryType.Error);
                while (_debugQueue.Count > 0)
                {
                    System.Threading.Thread.Sleep(10);
                }
                return sbError.ToString();
            }
            catch
            {
                return GetFullExceptionMessage(ex);
            }
        }
        public static void WriteLine(string format, params object[] arg)
        {
            if (string.IsNullOrEmpty(format))
                return;
            WriteLine(string.Format(format, arg));
        }
        public static void LogSystemPacket(string msgType, string message) { WriteLine("{0}: {1}", msgType, message); }

        public static string GetFullExceptionMessage(Exception ex)
        {
            if (ex == null)
                return string.Empty;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(string.Format("Exception: {0}", ex.GetType().FullName));
            sb.AppendLine(string.Format("Message: {0}", ex.Message));
            if (!string.IsNullOrEmpty(ex.HelpLink))
                sb.AppendLine(string.Format("Help Link: {0}", ex.HelpLink));
            try
            {
                System.ComponentModel.Win32Exception we = ex as System.ComponentModel.Win32Exception;
                if (we != null)
                {
                    sb.AppendLine(string.Format("Native Error Code: {0}", we.NativeErrorCode));
                }
            }
            catch { }
            try
            {
                System.Net.Sockets.SocketException se = ex as System.Net.Sockets.SocketException;
                if (se != null)
                {
                    sb.AppendLine(string.Format("Error Code: {0}", se.ErrorCode));
                    sb.AppendLine(string.Format("Socket Error Code: {0}", se.SocketErrorCode));
                }
            }
            catch { }
            try
            {
                System.Net.WebException wex = ex as System.Net.WebException;
                if (wex != null)
                {
                    sb.AppendLine(string.Format("Web Exception Status: {0}", wex.Status));
                    System.Net.HttpWebResponse httpRes=wex.Response as System.Net.HttpWebResponse;
                    if (httpRes!=null)
                    {
                        sb.AppendLine(string.Format("HTTP Response Status: {0}", httpRes.StatusCode));
                    }
                }
            }
            catch { }
            try
            {
                if (ex.TargetSite != null)
                {
                    if (ex.TargetSite.Module != null)
                    {
                        sb.AppendLine(string.Format("Target Site: {0}, Module: {1}", ex.TargetSite.Name, ex.TargetSite.Module.FullyQualifiedName));
                    }
                    else
                    {
                        sb.AppendLine(string.Format("Target Site: {0}", ex.TargetSite.Name));
                    }
                }
            }
            catch { }
            sb.AppendLine(string.Format("Source: {0}", ex.Source));
            sb.AppendLine(string.Format("Stack Trace: {0}", ex.StackTrace));
            if (ex.InnerException != null)
            {
                sb.AppendLine("---------------------------------------------------------------");
                sb.AppendLine(GetFullExceptionMessage(ex.InnerException));
            }
            try
            {
                if (typeof(System.OutOfMemoryException).IsAssignableFrom(ex.GetType()))
                {
                    GC.Collect();
                }
            }
            catch { }
            return sb.ToString();
        }
    }
}
