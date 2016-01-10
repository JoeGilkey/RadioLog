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
    /// Interaction logic for SingleRadioEditorWindow.xaml
    /// </summary>
    public partial class SingleRadioEditorWindow : BaseRadioLogWindow
    {
        public SingleRadioEditorWindow(ViewModels.RadioSignalItemModel radioSignal)
        {
            if (radioSignal == null)
                throw new ArgumentNullException();

            InitializeComponent();

            ViewModels.SingleRadioEditorViewModel vm = new ViewModels.SingleRadioEditorViewModel(radioSignal);
            this.DataContext = vm;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            (this.DataContext as ViewModels.SingleRadioEditorViewModel).PerformSave();
            DialogResult = true;
        }
    }
}
