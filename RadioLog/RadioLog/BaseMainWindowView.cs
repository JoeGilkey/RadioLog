using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace RadioLog
{
    public class BaseMainWindowView:System.Windows.Controls.UserControl
    {
        protected Common.IMainWindowDisplayProvider GetMainWindow(FrameworkElement obj)
        {
            if (obj == null)
                return null;
            Common.IMainWindowDisplayProvider mw = obj as Common.IMainWindowDisplayProvider;
            if (mw != null)
                return mw;
            return GetMainWindow(obj.Parent as FrameworkElement);
        }
        protected ViewModels.MainViewModel MainViewModel
        {
            get
            {
                Common.IMainWindowDisplayProvider mw = GetMainWindow(this);
                if (mw == null)
                    return null;
                else
                    return mw.DataContext as ViewModels.MainViewModel;
            }
        }

        public virtual void SetupDisplay() { }
        public virtual void SetupColumnVisibility() { }
        public virtual void SetupColumnWidths() { }
        public virtual void SaveColumnWidths() { }
        public virtual void ClearSelectedItem() { }
    }
}
