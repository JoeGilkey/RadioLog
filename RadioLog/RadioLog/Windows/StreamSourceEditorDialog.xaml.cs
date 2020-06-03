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

using Gma.QrCodeNet.Encoding.Windows.Render;
using Gma.QrCodeNet.Encoding;

namespace RadioLog.Windows
{
    /// <summary>
    /// Interaction logic for StreamSourceEditorDialog.xaml
    /// </summary>
    public partial class StreamSourceEditorDialog : BaseRadioLogWindow
    {
        private RadioLog.Common.SignalSource _src;
        private List<SourceListHolder> _waveOutList = new List<SourceListHolder>();
        private List<NoiseFloorHolder> _noiseFloorList = new List<NoiseFloorHolder>();

        public override Common.PopupScreenType ScreenSizeType { get { return Common.PopupScreenType.Percent60; } }

        public StreamSourceEditorDialog(RadioLog.Common.SignalSource src)
        {
            if (src == null)
                throw new ArgumentNullException();

            InitializeComponent();

            _src = src;

            RadioLog.WPFCommon.BrushSelectionHolder[] colors = RadioLog.WPFCommon.ColorHelper.GetBrushSelectionItems();
            System.Collections.ObjectModel.ObservableCollection<RadioLog.WPFCommon.BrushSelectionHolder> colorList = new System.Collections.ObjectModel.ObservableCollection<WPFCommon.BrushSelectionHolder>(colors);
            colorList.Insert(0, new WPFCommon.BrushSelectionHolder("Default", null));
            cbSourceColor.ItemsSource = colorList;
            if (string.IsNullOrEmpty(_src.SourceColor))
                cbSourceColor.SelectedIndex = 0;
            else
                cbSourceColor.SelectedValue = _src.SourceColor;

            tbSourceName.Text = _src.SourceName;
            tbStreamURL.Text = _src.SourceLocation;
            sRecordingKickTime.Value = _src.RecordingKickTime;
            tsRecordingEnabled.IsOn = _src.RecordAudio;
            tsPriority.IsOn = _src.IsPriority;
            cbDecodeMDC1200.IsChecked = _src.DecodeMDC1200;
            cbDecodeGEStar.IsChecked = _src.DecodeGEStar;
            cbDecodeFleetSync.IsChecked = _src.DecodeFleetSync;
            cbRemoveNoise.IsChecked = _src.RemoveNoise;
            sDisplayOrder.Value = _src.DisplayOrder;

            LoadDeviceLists();
            cbNoiseFloor.ItemsSource = _noiseFloorList;
            cbNoiseFloor.SelectedValue = _src.NoiseFloor;
            sCustomNoiseFloor.Value = _src.CustomNoiseFloor;
            cbWaveOutDev.ItemsSource = _waveOutList;
            if (_waveOutList.Count > 0 && !string.IsNullOrWhiteSpace(_src.WaveOutDeviceName))
            {
                cbWaveOutDev.SelectedItem = (_waveOutList.FirstOrDefault(l => l.DeviceName == _src.WaveOutDeviceName));
            }
            if (string.IsNullOrWhiteSpace(_src.WaveOutDeviceName) || cbWaveOutDev.SelectedIndex < 0)
            {
                cbWaveOutDev.SelectedIndex = 0;
            }

            switch (_src.RecordingType)
            {
                case Common.SignalRecordingType.Fixed: cbRecordingStyle.SelectedIndex = 1; break;
                default: cbRecordingStyle.SelectedIndex = 0; break;
            }
            UpdateForSelectedRecordingStyle();

            DisplayQRCode(_src.SourceLocation);
        }

        private void LoadDeviceLists()
        {
            _waveOutList.Clear();
            _waveOutList.Add(new SourceListHolder() { DeviceListNum = -1, DeviceName = "Default" });

            int iTotalCnt = NAudio.Wave.WaveOut.DeviceCount;
            for (int i = 0; i < iTotalCnt; i++)
            {
                NAudio.Wave.WaveOutCapabilities caps = NAudio.Wave.WaveOut.GetCapabilities(i);
                _waveOutList.Add(new SourceListHolder() { DeviceListNum = i, DeviceName = caps.ProductName });
            }

            _noiseFloorList.Clear();
            _noiseFloorList.Add(new NoiseFloorHolder() { NoiseFloorName = "Low", NoiseFloorValue = Common.NoiseFloor.Low });
            _noiseFloorList.Add(new NoiseFloorHolder() { NoiseFloorName = "Normal", NoiseFloorValue = Common.NoiseFloor.Normal });
            _noiseFloorList.Add(new NoiseFloorHolder() { NoiseFloorName = "High", NoiseFloorValue = Common.NoiseFloor.High });
            _noiseFloorList.Add(new NoiseFloorHolder() { NoiseFloorName = "Extra High", NoiseFloorValue = Common.NoiseFloor.ExtraHigh });
            _noiseFloorList.Add(new NoiseFloorHolder() { NoiseFloorName = "Custom", NoiseFloorValue = Common.NoiseFloor.Custom });
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!VerifySourceInputs())
                return;
            string url;
            if (!Common.UrlHelper.GetCorrectedStreamURL(tbStreamURL.Text, out url))
                return;

            string colorName = string.Empty;
            if (cbSourceColor.SelectedIndex > 0)
            {
                colorName = cbSourceColor.SelectedValue as string;
            }
            _src.SourceColor = colorName;

            _src.SourceName = tbSourceName.Text;
            _src.SourceLocation = tbStreamURL.Text;
            _src.RecordingKickTime = (int)(sRecordingKickTime.Value);
            _src.RecordAudio = tsRecordingEnabled.IsOn == true;
            _src.IsPriority = tsPriority.IsOn == true;
            _src.DecodeMDC1200 = cbDecodeMDC1200.IsChecked == true;
            _src.DecodeGEStar = cbDecodeGEStar.IsChecked == true;
            _src.DecodeFleetSync = cbDecodeFleetSync.IsChecked == true;
            _src.RemoveNoise = cbRemoveNoise.IsChecked == true;
            _src.DisplayOrder = (int)sDisplayOrder.Value;
            if (cbWaveOutDev.SelectedIndex <= 0)
                _src.WaveOutDeviceName = string.Empty;
            else
            {
                SourceListHolder dev = cbWaveOutDev.SelectedItem as SourceListHolder;
                if (dev == null || dev.DeviceListNum < 0)
                    _src.WaveOutDeviceName = string.Empty;
                else
                    _src.WaveOutDeviceName = dev.DeviceName;
            }
            if (cbNoiseFloor.SelectedIndex < 0)
            {
                _src.NoiseFloor = Common.NoiseFloor.Normal;
            }
            else
            {
                NoiseFloorHolder nf = cbNoiseFloor.SelectedItem as NoiseFloorHolder;
                if (nf == null)
                    _src.NoiseFloor = Common.NoiseFloor.Normal;
                else
                    _src.NoiseFloor = nf.NoiseFloorValue;
            }
            if (sCustomNoiseFloor.Value.HasValue)
            {
                int cNoise = (int)sCustomNoiseFloor.Value.Value;
                _src.CustomNoiseFloor = Math.Min(1000, Math.Max(cNoise, 1));
            }
            if (cbRecordingStyle.SelectedIndex >= 1)
                _src.RecordingType = Common.SignalRecordingType.Fixed;
            else
                _src.RecordingType = Common.SignalRecordingType.VOX;
            RadioLog.Common.AppSettings.Instance.SaveSettingsFile();
            Cinch.Mediator.Instance.NotifyColleagues<Guid>("UPDATE_SIGNAL_SOURCE", _src.SourceId);
            DialogResult = true;
        }

        public static void ShowNewSourceDialog()
        {
            NewSourceDialog dlg = new NewSourceDialog();
            dlg.ShowDialog();
        }

        void DisplayQRCode(string imageUrl)
        {
            imageUrl = Common.UrlHelper.GetCorrectedStreamURL(imageUrl);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                brdrQRCode.Visibility = System.Windows.Visibility.Collapsed;
                return;
            }
            try
            {
                QrEncoder encoder = new QrEncoder(ErrorCorrectionLevel.M);
                QrCode qrCode;
                if (encoder.TryEncode(imageUrl, out qrCode))
                {
                    DrawingBrushRenderer dRenderer = new DrawingBrushRenderer(new FixedModuleSize(2, QuietZoneModules.Two), Brushes.Black, Brushes.White);
                    DrawingBrush dBrush = dRenderer.DrawBrush(qrCode.Matrix);
                    rectQRCode.Fill = dBrush;
                    brdrQRCode.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    brdrQRCode.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            catch
            {
                brdrQRCode.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private bool VerifySourceInputs()
        {
            DisplayQRCode(tbStreamURL.Text);
            return (!string.IsNullOrWhiteSpace(tbSourceName.Text) && !string.IsNullOrWhiteSpace(tbStreamURL.Text));
        }

        private void tbTextChanged(object sender, TextChangedEventArgs e)
        {
            VerifySourceInputs();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (Common.MessageBoxHelper.Show(string.Format("Are you sure you want to delete {0}?", _src.SourceName), "Delete Source", MessageBoxButton.YesNo, MessageBoxImage.Stop, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                Common.AppSettings.Instance.SignalSources.Remove(_src);
                Cinch.Mediator.Instance.NotifyColleagues<Guid>("DELETE_SIGNAL_SOURCE", _src.SourceId);
                DialogResult = false;
            }
        }

        private void UpdateForSelectedRecordingStyle()
        {
            if (cbRecordingStyle == null || tbRecordTimeLabel == null)
                return;
            switch (cbRecordingStyle.SelectedIndex)
            {
                case 1:
                    {
                        tbRecordTimeLabel.Text = "Time (min):";
                        break;
                    }
                default:
                    {
                        tbRecordTimeLabel.Text = "VOX Time (sec):";
                        break;
                    }
            }
        }
        private void cbRecordingStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateForSelectedRecordingStyle();
        }
    }
}