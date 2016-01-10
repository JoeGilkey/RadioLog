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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RadioLog.Views
{
    /// <summary>
    /// Interaction logic for SignalingProcessorView.xaml
    /// </summary>
    public partial class SignalingProcessorView : UserControl
    {
        public SignalingProcessorView()
        {
            InitializeComponent();
        }

        private void ProcessStreamSourceSettingsEditor(ViewModels.StreamingSourceModel model)
        {
            if (model == null)
                return;
            RadioLog.Windows.StreamSourceEditorDialog editor = new Windows.StreamSourceEditorDialog(model.SrcInfo);
            editor.ShowDialog();
        }

        private void ProcessLineInSettingsEditor(ViewModels.WaveInChannelSourceModel model)
        {
            if (model == null)
                return;
            RadioLog.Windows.WaveInChannelSourceEditorDialog editor = new Windows.WaveInChannelSourceEditorDialog(model.SrcInfo);
            editor.ShowDialog();
        }

        private void btnStreamSettings_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.BaseSourceModel baseProcessor = this.DataContext as ViewModels.BaseSourceModel;
            if (baseProcessor == null)
                return;
            switch (baseProcessor.SourceType)
            {
                case Common.SignalingSourceType.Streaming: ProcessStreamSourceSettingsEditor(baseProcessor as ViewModels.StreamingSourceModel); break;
                case Common.SignalingSourceType.WaveInChannel: ProcessLineInSettingsEditor(baseProcessor as ViewModels.WaveInChannelSourceModel); break;
            }
        }

        private void btnToggleMuteOthers_Click(object sender, RoutedEventArgs e)
        {
            ViewModels.BaseSourceModel baseProcessor = this.DataContext as ViewModels.BaseSourceModel;
            if (baseProcessor == null || baseProcessor.SignalSourceId == Guid.Empty)
                return;
            //Cinch.Mediator.Instance.NotifyColleagues<Guid>("TOGGLE_SOURCE_MUTE_OTHERS", baseProcessor.SignalSourceId);
            Services.AudioMuteService.Instance.ProcessToggleSourceMuteOthers(baseProcessor.SignalSourceId);
        }
    }
}
