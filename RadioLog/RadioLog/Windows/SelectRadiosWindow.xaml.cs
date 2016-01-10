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

using RadioLog.Common;

namespace RadioLog.Windows
{
    /// <summary>
    /// Interaction logic for SelectRadiosWindow.xaml
    /// </summary>
    public partial class SelectRadiosWindow : BaseRadioLogWindow
    {
        private System.Collections.ObjectModel.ObservableCollection<RadioInfo> _radios;

        public SelectRadiosWindow()
        {
            InitializeComponent();

            _radios = new System.Collections.ObjectModel.ObservableCollection<RadioInfo>(RadioInfoLookupHelper.Instance.RadioList.OrderBy(r => r.DisplayName));
            lbRadios.ItemsSource = _radios;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) { ProcessCheckUncheck(sender, false); }
        private void CheckBox_Checked(object sender, RoutedEventArgs e) { ProcessCheckUncheck(sender, true); }
        private void btnOk_Click(object sender, RoutedEventArgs e) { DialogResult = true; }
        private void btnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; }

        private void ProcessCheckUncheck(object sender, bool bChecked)
        {
            CheckBox cb = sender as CheckBox;
            if (cb == null || cb.Tag == null)
                return;
            RadioInfo rc = cb.Tag as RadioInfo;
            if (rc == null)
                return;
            if (bChecked && !lbRadios.SelectedItems.Contains(cb.Tag))
                lbRadios.SelectedItems.Add(cb.Tag);
            else if (!bChecked && lbRadios.SelectedItems.Contains(cb.Tag))
                lbRadios.SelectedItems.Remove(cb.Tag);
        }

        public RadioInfo[] SelectedRadios
        {
            get
            {
                List<RadioInfo> rslt = new List<RadioInfo>();
                foreach (object o in lbRadios.SelectedItems)
                {
                    RadioInfo ri = o as RadioInfo;
                    if (ri != null)
                    {
                        rslt.Add(ri);
                    }
                }
                return rslt.ToArray();
            }
        }

        public static RadioInfo[] DoRadioSelection()
        {
            SelectRadiosWindow win = new SelectRadiosWindow();
            if (win.ShowDialog() == true)
            {
                return win.SelectedRadios;
            }
            return null;
        }
    }
}
