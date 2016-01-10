using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Windows
{
    public class BaseRadioLogWindow:MahApps.Metro.Controls.MetroWindow
    {
        public BaseRadioLogWindow()
            : base()
        {
            Cinch.Mediator.Instance.NotifyColleagues<bool>("OVERLAY_VISIBLE_CHANGED", true);

            WindowTransitionsEnabled = false;
            Topmost = true;
            WindowStyle = System.Windows.WindowStyle.None;
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            ShowCloseButton = false;
            ShowMaxRestoreButton = false;
            ShowMinButton = false;
            ShowIconOnTitleBar = false;

            this.Loaded += (s, e) =>
            {
                switch (ScreenSizeType)
                {
                    case Common.PopupScreenType.Percent80:
                        {
                            Common.ScreenHelper.Setup80PercentScreenRect(this);
                            break;
                        }
                    case Common.PopupScreenType.Percent60:
                        {
                            if (Common.ScreenHelper.IsSmallScreenSize)
                                Common.ScreenHelper.Setup80PercentScreenRect(this);
                            else
                                Common.ScreenHelper.Setup60PercentScreenRect(this);
                            break;
                        }
                }
            };
        }

        public virtual RadioLog.Common.PopupScreenType ScreenSizeType { get { return RadioLog.Common.PopupScreenType.Percent80; } }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Cinch.Mediator.Instance.NotifyColleagues<bool>("OVERLAY_VISIBLE_CHANGED", false);
            base.OnClosing(e);
        }
    }
}
