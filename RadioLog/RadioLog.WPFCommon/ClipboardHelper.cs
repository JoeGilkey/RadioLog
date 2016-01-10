using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace RadioLog.WPFCommon
{
    public class ClipboardHelper
    {
        private static ClipboardHelper _instance = null;
        public static ClipboardHelper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClipboardHelper();
                }
                return _instance;
            }
        }

        IDataObject _lastDataObj = null;
        string _lastText = string.Empty;
        System.Windows.Threading.DispatcherTimer _tProcess = null;
        string[] _goodFileExtensions = new string[] { ".m3u" };

        void _tProcess_Tick(object sender, EventArgs e)
        {
            try
            {
                bool bShouldCheck = false;
                if (_lastDataObj == null)
                {
                    _lastDataObj = Clipboard.GetDataObject();
                    bShouldCheck = true;
                }
                else if (!Clipboard.IsCurrent(_lastDataObj))
                {
                    bShouldCheck = true;
                }
                if (bShouldCheck)
                {
                    CheckClipboard();
                }
            }
            catch
            {
                Stop();
                Common.AppSettings.Instance.EnableClipboardStreamURLIntegration = false;
            }
        }

        public void Start()
        {
            _lastDataObj = null;
            _lastText = string.Empty;
            if (Common.AppSettings.Instance.EnableClipboardStreamURLIntegration)
            {
                System.Windows.Threading.DispatcherTimer _tProcess = new System.Windows.Threading.DispatcherTimer(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                _tProcess.Interval = TimeSpan.FromMilliseconds(1500);
                _tProcess.Tick += _tProcess_Tick;
                _tProcess.Start();
            }
            else
            {
                Stop();
            }
        }
        public void Stop()
        {
            if (_tProcess != null)
            {
                _tProcess.Stop();
                _tProcess = null;
            }
        }

        private void CheckClipboard()
        {
            if (Clipboard.ContainsText(TextDataFormat.Html) || Clipboard.ContainsText(TextDataFormat.Text) || Clipboard.ContainsText(TextDataFormat.UnicodeText))
            {
                string tmp = Clipboard.GetText();
                if (!string.IsNullOrEmpty(tmp) && !string.Equals(_lastText, tmp))
                {
                    _lastText = tmp;
                    tmp = _lastText.Trim();
                    bool bGood = false;
                    foreach (string strExt in _goodFileExtensions)
                    {
                        if (tmp.EndsWith(strExt, StringComparison.InvariantCultureIgnoreCase))
                        {
                            bGood = true;
                            break;
                        }
                    }
                    if (bGood)
                    {
                        Cinch.Mediator.Instance.NotifyColleaguesAsync<string>("CLIPBOARD_TEXT_CHANGED", tmp);
                    }
                }
            }
        }
    }
}
