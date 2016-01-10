using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RadioLog.Windows
{
    /// <summary>
    /// Interaction logic for MessageBoxWindow.xaml
    /// </summary>
    public partial class MessageBoxWindow : MahApps.Metro.Controls.MetroWindow
    {
        private Button _close;
        private MessageBoxResult _result;

        public static readonly DependencyProperty DefaultResultProperty = DependencyProperty.Register("DefaultResult", typeof(MessageBoxResult), typeof(MessageBoxWindow), new UIPropertyMetadata(MessageBoxResult.None));
        public static readonly DependencyProperty MessageBoxButtonProperty = DependencyProperty.Register("MessageBoxButton", typeof(MessageBoxButton), typeof(MessageBoxWindow), new UIPropertyMetadata(MessageBoxButton.OK));
        public static readonly DependencyProperty MessageBoxImageProperty = DependencyProperty.Register("MessageBoxImage", typeof(MessageBoxImage), typeof(MessageBoxWindow), new UIPropertyMetadata(MessageBoxImage.None));
        public static readonly DependencyProperty MessageProperty = DependencyProperty.Register("Message", typeof(string), typeof(MessageBoxWindow), new UIPropertyMetadata(string.Empty));
        public static readonly DependencyProperty DoNotAskVisibleProperty = DependencyProperty.Register("DoNotAskVisible", typeof(Visibility), typeof(MessageBoxWindow), new UIPropertyMetadata(Visibility.Collapsed));
        public static readonly DependencyProperty DoNotAskCheckedProperty = DependencyProperty.Register("DoNotAskChecked", typeof(bool), typeof(MessageBoxWindow), new UIPropertyMetadata(false));

        public MessageBoxWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                this._close = (Button)base.Template.FindName("PART_Close", this);
                if ((this._close != null) && !this._cancel.IsVisible)
                {
                    this._close.IsCancel = false;
                }
            };
        }

        public string Caption
        {
            get
            {
                return base.Title;
            }
            set
            {
                base.Title = value;
            }
        }
        public MessageBoxResult DefaultResult
        {
            get
            {
                return (MessageBoxResult)base.GetValue(DefaultResultProperty);
            }
            set
            {
                base.SetValue(DefaultResultProperty, value);
                switch (value)
                {
                    case MessageBoxResult.None:
                    case (MessageBoxResult.Cancel | MessageBoxResult.OK):
                    case ((MessageBoxResult)4):
                    case ((MessageBoxResult)5):
                        break;

                    case MessageBoxResult.OK:
                        this._ok.IsDefault = true;
                        return;

                    case MessageBoxResult.Cancel:
                        this._cancel.IsDefault = true;
                        return;

                    case MessageBoxResult.Yes:
                        this._yes.IsDefault = true;
                        break;

                    case MessageBoxResult.No:
                        this._no.IsDefault = true;
                        return;

                    default:
                        return;
                }
            }
        }
        public string Message
        {
            get
            {
                return (string)base.GetValue(MessageProperty);
            }
            set
            {
                base.SetValue(MessageProperty, value);
            }
        }
        public Visibility DoNotAskVisible
        {
            get { return (Visibility)base.GetValue(DoNotAskVisibleProperty); }
            set { base.SetValue(DoNotAskVisibleProperty, value); }
        }
        public bool DoNotAskChecked
        {
            get { return (bool)base.GetValue(DoNotAskCheckedProperty); }
            set { base.SetValue(DoNotAskCheckedProperty, value); }
        }
        public MessageBoxButton MessageBoxButton
        {
            get
            {
                return (MessageBoxButton)base.GetValue(MessageBoxButtonProperty);
            }
            set
            {
                base.SetValue(MessageBoxButtonProperty, value);
                switch (value)
                {
                    case MessageBoxButton.OK:
                        this._ok.Visibility = Visibility.Visible;
                        this._ok.IsCancel = true;
                        return;

                    case MessageBoxButton.OKCancel:
                        this._ok.Visibility = Visibility.Visible;
                        this._cancel.Visibility = Visibility.Visible;
                        return;

                    case MessageBoxButton.YesNoCancel:
                        this._yes.Visibility = Visibility.Visible;
                        this._no.Visibility = Visibility.Visible;
                        this._cancel.Visibility = Visibility.Visible;
                        return;

                    case MessageBoxButton.YesNo:
                        this._yes.Visibility = Visibility.Visible;
                        this._no.Visibility = Visibility.Visible;
                        return;
                }
                this._yes.Visibility = Visibility.Collapsed;
                this._no.Visibility = Visibility.Collapsed;
                this._cancel.Visibility = Visibility.Collapsed;
                this._ok.Visibility = Visibility.Collapsed;
            }
        }
        public MessageBoxImage MessageBoxImage
        {
            get
            {
                return (MessageBoxImage)base.GetValue(MessageBoxImageProperty);
            }
            set
            {
                base.SetValue(MessageBoxImageProperty, value);
            }
        }
        public MessageBoxResult MessageBoxResult
        {
            get
            {
                return this._result;
            }
            private set
            {
                this._result = value;
                if (MessageBoxResult.Cancel == this._result)
                {
                    base.DialogResult = false;
                }
                else
                {
                    base.DialogResult = true;
                }
            }
        }

        private void yes_Click(object sender, RoutedEventArgs e) { this.MessageBoxResult = System.Windows.MessageBoxResult.Yes; }
        private void no_Click(object sender, RoutedEventArgs e) { this.MessageBoxResult = System.Windows.MessageBoxResult.No; }
        private void ok_Click(object sender, RoutedEventArgs e) { this.MessageBoxResult = System.Windows.MessageBoxResult.OK; }
        private void cancel_Click(object sender, RoutedEventArgs e) { this.MessageBoxResult = System.Windows.MessageBoxResult.Cancel; }
    }

    public class MessageBoxProvider : Common.IMessageBoxProvider
    {
        public MessageBoxResult Show(string messageBoxText, string caption)
        {
            return Show(messageBoxText, caption, MessageBoxButton.OK, MessageBoxImage.None);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            return Show(messageBoxText, caption, button, icon, MessageBoxResult.OK);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            bool bDoNotAsk = false;
            return Show(messageBoxText, caption, button, icon, defaultResult, false, out bDoNotAsk);
        }

        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxResult defaultResult, bool showDoNotAsk, out bool doNotAskAgain)
        {
            return Show(messageBoxText, caption, button, MessageBoxImage.None, defaultResult, showDoNotAsk, out doNotAskAgain);
        }
        public MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, bool showDoNotAsk, out bool doNotAskAgain)
        {
            return Show(null, messageBoxText, caption, button, icon, defaultResult, showDoNotAsk, out doNotAskAgain);
        }

        public MessageBoxResult Show(Window owner, string messageBoxText, string caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, bool showDoNotAsk, out bool doNotAskAgain)
        {
            MessageBoxWindow view = new MessageBoxWindow { Caption = caption, Message = messageBoxText, DefaultResult = defaultResult, MessageBoxButton = button, MessageBoxImage = icon };
            view.DoNotAskVisible = showDoNotAsk ? Visibility.Visible : Visibility.Collapsed;
            try
            {
                if((owner!=null)&&owner.IsVisible)
                {
                    view.Owner = owner;
                }
            }
            catch { }
            if (view.ShowDialog() == false)
            {
                doNotAskAgain = false;
                return MessageBoxResult.Cancel;
            }
            doNotAskAgain = view.DoNotAskChecked;
            return view.MessageBoxResult;
        }

        public void ShowNonModal(string messageBoxText, string caption)
        {
            MessageBoxWindow view = new MessageBoxWindow { Caption = caption, Message = messageBoxText };
            try
            {
                if ((Application.Current.MainWindow != null) && Application.Current.MainWindow.IsVisible)
                {
                    view.Owner = Application.Current.MainWindow;
                }
            }
            catch { }
            view.Show();
        }
    }
}
