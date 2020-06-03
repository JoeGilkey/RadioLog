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
    /// Interaction logic for NewSourceDialog.xaml
    /// </summary>
    public partial class NewSourceDialog : BaseRadioLogWindow
    {
        private List<SourceListHolder> _channelList = new List<SourceListHolder>();
        private List<string> _serialPorts = new List<string>();
        private bool _initDone = false;
        
        private ViewModels.MainViewModel GetMainViewModel()
        {
            return (App.Current.MainWindow.DataContext as ViewModels.MainViewModel);
        }

        public NewSourceDialog()
        {
            InitializeComponent();

            LoadDeviceLists();
            cbMultiLineIn.ItemsSource = _channelList;
            cbGroup.ItemsSource = GetMainViewModel().SignalGroups;
            cbGroup.SelectedValue = Guid.Empty;
            cbGroup.IsEnabled = Common.AppSettings.Instance.UseGroups;

            _initDone = true;

            if (_channelList.Count > 0)
            {
                cbMultiLineIn.SelectedIndex = 0;
                rbLineIn.IsEnabled = true;
            }
            else
            {
                rbLineIn.IsEnabled = false;
            }

            RadioLog.WPFCommon.BrushSelectionHolder[] colors = RadioLog.WPFCommon.ColorHelper.GetBrushSelectionItems();
            System.Collections.ObjectModel.ObservableCollection<RadioLog.WPFCommon.BrushSelectionHolder> colorList = new System.Collections.ObjectModel.ObservableCollection<WPFCommon.BrushSelectionHolder>(colors);
            colorList.Insert(0, new WPFCommon.BrushSelectionHolder("Default", null));
            cbSourceColor.ItemsSource = colorList;
            cbSourceColor.SelectedIndex = 0;
            sDisplayOrder.Value = 10;

            SelectedSourceType = Common.SignalingSourceType.Streaming;

            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                string strUrl = Clipboard.GetText(TextDataFormat.Html);
                if (!string.IsNullOrWhiteSpace(strUrl))
                {
                    strUrl = strUrl.Trim();
                    if (strUrl.EndsWith(".m3u", StringComparison.InvariantCultureIgnoreCase))
                    {
                        tbStreamURL.Text = strUrl;
                    }
                }
            }
        }

        private void LoadDeviceLists()
        {
            int iTotalCnt = NAudio.Wave.WaveIn.DeviceCount;
            for (int i = 0; i < iTotalCnt; i++)
            {
                NAudio.Wave.WaveInCapabilities caps = NAudio.Wave.WaveIn.GetCapabilities(i);
                if (caps.Channels > 0)
                {
                    _channelList.Add(new SourceListHolder() { DeviceListNum = i, DeviceName = caps.ProductName, ChannelCount = caps.Channels });
                }
            }

            for(int i = 0; i < 100; i++)
            {
                _serialPorts.Add($"COM{i + 1}");
            }
            //JGcore
            /*
            string[] serialPorts= System.IO.Ports.SerialPort.GetPortNames();
            if (serialPorts != null && serialPorts.Length > 0)
            {
                foreach (string s in serialPorts)
                {
                    _serialPorts.Add(s);
                }
            }
            */
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifySourceInputs())
                return;
            string colorName = string.Empty;
            if (cbSourceColor.SelectedIndex > 0)
            {
                colorName = cbSourceColor.SelectedValue as string;
            }
            int displayOrder = (int)sDisplayOrder.Value;
            RadioLog.Common.SignalSource newSrc = null;
            switch (SelectedSourceType)
            {
                case Common.SignalingSourceType.File:
                    {
                        //newSrc.SourceURL
                        break;
                    }
                case Common.SignalingSourceType.WaveInChannel:
                    {
                        newSrc = new Common.SignalSource()
                        {
                            SourceName = tbSourceName.Text,
                            SourceType = Common.SignalingSourceType.WaveInChannel,
                            IsEnabled = true,
                            SourceLocation = _channelList[cbMultiLineIn.SelectedIndex].DeviceName,
                            SourceChannel = ((int)(nudChannelNumber.Value ?? 1) - 1)
                        };
                        break;
                    }
                case Common.SignalingSourceType.Streaming:
                    {
                        string url;
                        if (GetCorrectedStreamURL(tbStreamURL.Text, out url))
                        {
                            newSrc = new Common.SignalSource()
                            {
                                SourceName = tbSourceName.Text,
                                SourceType = Common.SignalingSourceType.Streaming,
                                IsEnabled = true,
                                SourceLocation = url
                            };
                        }
                        break;
                    }
            }
            if (newSrc != null)
            {
                newSrc.SourceColor = colorName;
                newSrc.DisplayOrder = displayOrder;
                newSrc.NoiseFloor = Common.NoiseFloor.Normal;
                newSrc.CustomNoiseFloor = 20;
                newSrc.RemoveNoise = true;
                newSrc.Volume = 50;
                newSrc.MaxVolume = 100;
                if (cbGroup.SelectedValue != null)
                    newSrc.GroupId = (Guid)cbGroup.SelectedValue;
                RadioLog.Common.AppSettings.Instance.AddNewSignalSource(newSrc);
                Cinch.Mediator.Instance.NotifyColleagues<RadioLog.Common.SignalSource>("NEW_SIGNAL_SOURCE", newSrc);
                DialogResult = true;
            }
        }

        public static void ShowNewSourceDialog(string streamURL = null, string streamName = null)
        {
            NewSourceDialog dlg = new NewSourceDialog();
            if (!string.IsNullOrWhiteSpace(streamURL))
            {
                dlg.tbStreamURL.Text = streamURL;
            }
            if (!string.IsNullOrWhiteSpace(streamName))
            {
                dlg.tbSourceName.Text = streamName;
            }
            dlg.ShowDialog();
        }

        private bool GetCorrectedStreamURL(string inUrl, out string outUrl)
        {
            outUrl = inUrl;
            Uri test;
            if (Uri.TryCreate(inUrl, UriKind.Absolute, out test))
                return true;
            string fixedStr = Uri.UriSchemeHttp + Uri.SchemeDelimiter + inUrl;
            if (Uri.TryCreate(fixedStr, UriKind.Absolute, out test))
            {

                outUrl = fixedStr;
                return true;
            }
            else
                return false;
        }

        private bool VerifyChannelInputsValid(int iChannelsVisible)
        {
            if (iChannelsVisible <= 0)
                return true;
            int chanNum = (int)(nudChannelNumber.Value ?? 1);
            return (chanNum > 0 & chanNum <= iChannelsVisible);
        }
        private bool VerifySourceInputs()
        {
            bool bInputsValid = !string.IsNullOrEmpty(tbSourceName.Text);
            if (bInputsValid)
            {
                bool bExists = Common.AppSettings.Instance.SignalSources.FirstOrDefault(s => s.SourceName == tbSourceName.Text) != null;
                if (bExists)
                {
                    bInputsValid = false;
                    tbSourceNameWarning.Text = string.Format("A source with the name {0} already exists!", tbSourceName.Text);
                    tbSourceNameWarning.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    tbSourceNameWarning.Visibility = System.Windows.Visibility.Collapsed;
                    tbSourceNameWarning.Text = string.Empty;
                }
            }
            if (bInputsValid)
            {
                switch (SelectedSourceType)
                {
                    case Common.SignalingSourceType.File:
                        {
                            bInputsValid = false;
                            break;
                        }
                    case Common.SignalingSourceType.WaveInChannel:
                        {
                            bInputsValid = (_channelList.Count > 0 && cbMultiLineIn.SelectedIndex >= 0);
                            if (bInputsValid)
                            {
                                int iChannelsVisible = _channelList[cbMultiLineIn.SelectedIndex].ChannelCount;
                                bInputsValid = VerifyChannelInputsValid(iChannelsVisible);
                            }
                            break;
                        }
                    case Common.SignalingSourceType.Streaming:
                        {
                            bInputsValid = !string.IsNullOrWhiteSpace(tbStreamURL.Text);
                            if (bInputsValid)
                            {
                                bool bExists = Common.AppSettings.Instance.SignalSources.FirstOrDefault(s => s.SourceLocation == tbStreamURL.Text && s.SourceType == Common.SignalingSourceType.Streaming) != null;
                                if (bExists)
                                {
                                    bInputsValid = false;
                                    tbStreamURLWarning.Text = "A source with this URL already exists!";
                                    tbStreamURLWarning.Visibility = System.Windows.Visibility.Visible;
                                }
                                else
                                {
                                    tbStreamURLWarning.Visibility = System.Windows.Visibility.Collapsed;
                                    tbStreamURLWarning.Text = string.Empty;
                                }
                            }
                            break;
                        }
                }
            }
            btnOk.IsEnabled = bInputsValid;
            return bInputsValid;
        }
        private void SetupDisplayForCurrentSourceType()
        {
            if (!_initDone)
                return;
            switch (SelectedSourceType)
            {
                case Common.SignalingSourceType.File:
                    {
                        grdStreamingSource.Visibility = System.Windows.Visibility.Collapsed;
                        grdMultiLineIn.Visibility = System.Windows.Visibility.Collapsed;
                        break;
                    }
                case Common.SignalingSourceType.WaveInChannel:
                    {
                        grdStreamingSource.Visibility = System.Windows.Visibility.Collapsed;
                        grdMultiLineIn.Visibility = System.Windows.Visibility.Visible;
                        if (cbMultiLineIn.SelectedIndex >= 0)
                        {
                            nudChannelNumber.Maximum = _channelList[cbMultiLineIn.SelectedIndex].ChannelCount;
                            int chanNum = (int)(nudChannelNumber.Value ?? 1);
                            if (chanNum > _channelList[cbMultiLineIn.SelectedIndex].ChannelCount || chanNum < 1)
                                nudChannelNumber.Value = 1;
                            lbLineIn.Text = string.Format("Channel should be a value between 1 and {0}, typically, 1=Left, 2=Right, etc.", _channelList[cbMultiLineIn.SelectedIndex].ChannelCount);
                        }
                        else
                        {
                            lbLineIn.Text = string.Empty;
                        }
                        break;
                    }
                case Common.SignalingSourceType.Streaming:
                    {
                        grdStreamingSource.Visibility = System.Windows.Visibility.Visible;
                        grdMultiLineIn.Visibility = System.Windows.Visibility.Collapsed;
                        break;
                    }
            }
            VerifySourceInputs();
        }
        public RadioLog.Common.SignalingSourceType SelectedSourceType
        {
            get
            {
                if (rbStream.IsChecked == true)
                    return Common.SignalingSourceType.Streaming;
                else if (rbLineIn.IsChecked == true)
                    return Common.SignalingSourceType.WaveInChannel;
                else
                    return Common.SignalingSourceType.File;
            }
            set
            {
                if (!_initDone)
                    return;
                switch (value)
                {
                    case Common.SignalingSourceType.File: break;
                    case Common.SignalingSourceType.WaveInChannel: rbLineIn.IsChecked = true; break;
                    case Common.SignalingSourceType.Streaming: rbStream.IsChecked = true; break;
                }
                SetupDisplayForCurrentSourceType();
            }
        }

        private void rbSourceTypeClick(object sender, RoutedEventArgs e)
        {
            SetupDisplayForCurrentSourceType();
        }

        private void InputsChanged(object sender, RoutedEventArgs e)
        {
            VerifySourceInputs();
        }

        private void tbTextChanged(object sender, TextChangedEventArgs e)
        {
            VerifySourceInputs();
        }

        private void cbSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            VerifySourceInputs();
        }
    }

    public class SourceListHolder
    {
        public int DeviceListNum { get; set; }
        public string DeviceName { get; set; }
        public int ChannelCount { get; set; }
    }

    public class NoiseFloorHolder
    {
        public Common.NoiseFloor NoiseFloorValue { get; set; }
        public string NoiseFloorName { get; set; }
    }
}
