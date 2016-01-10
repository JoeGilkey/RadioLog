using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog
{
    public class BasePositionSavingWindow:MahApps.Metro.Controls.MetroWindow
    {
        private bool _isMaximized = false;
        protected virtual string WindowSavingName { get { return string.Empty; } }
        protected virtual bool DefaultMaximized { get { return false; } }
        private string StorageName { get { return string.Format("WINDOW_{0}", WindowSavingName); } }

        public BasePositionSavingWindow()
        {
            _isMaximized = Common.AppSettings.Instance.ReadBool(StorageName, "Maximized", DefaultMaximized);

            this.Loaded += (s, e) =>
            {
                LoadPos();
            };
            this.StateChanged += (s, e) =>
            {
                _isMaximized = this.WindowState == System.Windows.WindowState.Maximized;
            };

            this.Closing += (s, e) =>
            {
                SavePos();
            };
        }

        protected void LoadPos()
        {
            System.Drawing.Rectangle rBounds = Common.ScreenHelper.GetProperScreenRect(
                Common.AppSettings.Instance.ReadInt32(StorageName, "Top", 40),
                Common.AppSettings.Instance.ReadInt32(StorageName, "Left", 40),
                Common.AppSettings.Instance.ReadInt32(StorageName, "Width", 400),
                Common.AppSettings.Instance.ReadInt32(StorageName, "Height", 300),
                _isMaximized);

            this.Top = rBounds.Top;
            this.Left = rBounds.Left;
            this.Width = rBounds.Width;
            this.Height = rBounds.Height;
        }

        protected void SavePos()
        {
            Common.AppSettings.Instance.WriteInt32(StorageName, "Top", (int)this.Top);
            Common.AppSettings.Instance.WriteInt32(StorageName, "Left", (int)this.Left);
            Common.AppSettings.Instance.WriteInt32(StorageName, "Width", (int)this.ActualWidth);
            Common.AppSettings.Instance.WriteInt32(StorageName, "Height", (int)this.ActualHeight);
            Common.AppSettings.Instance.WriteBool(StorageName, "Maximized", _isMaximized);
        }
    }
}
