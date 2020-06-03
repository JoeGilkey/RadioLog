using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace RadioLog.Common
{
    public static class MessageBoxHelper
    {
        private static IMessageBoxProvider _provider = null;

        public static void SetMessageBoxProvider(IMessageBoxProvider provider) { _provider = provider; }

        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            if (_provider != null)
                return _provider.Show(messageBoxText, caption);
            else
                return MessageBox.Show(messageBoxText, caption);
        }
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            if (_provider != null)
                return _provider.Show(messageBoxText, caption, button, icon);
            else
                return MessageBox.Show(messageBoxText, caption, button, icon);
        }
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultResult)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.None, defaultResult);
        }
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            if (_provider != null)
                return _provider.Show(messageBoxText, caption, button, icon, defaultResult);
            else
                return MessageBox.Show(messageBoxText, caption, button, icon, defaultResult);
        }
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultResult, bool showDoNotAsk, out bool doNotAskAgain)
        {
            doNotAskAgain = false;

            if (_provider != null)
                return _provider.Show(messageBoxText, caption, button, defaultResult, showDoNotAsk, out doNotAskAgain);
            else
                return Show(messageBoxText, caption, button, defaultResult);
        }

        public static void ShowNonModal(string messageBoxText, string caption)
        {
            if (_provider != null)
            {
                _provider.ShowNonModal(messageBoxText, caption);
            }
        }
    }

    public interface IMessageBoxProvider
    {
        MessageBoxResult Show(string messageBoxText, string caption);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon);
        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult);

        void ShowNonModal(string messageBoxText, string caption);

        MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultResult, bool showDoNotAsk, out bool doNotAskAgain);
    }
}
