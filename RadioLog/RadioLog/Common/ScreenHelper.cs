//#define SMALL_SCREEN_TEST

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadioLog.Common
{
    public static class ScreenHelper
    {
        public static System.Drawing.Rectangle GetProperScreenRect(int top, int left, int width, int height, bool bMaximized) { return GetProperScreenRect(new System.Drawing.Rectangle(left, top, width, height), bMaximized); }
        public static System.Drawing.Rectangle GetProperScreenRect(System.Drawing.Rectangle windowRect, bool bMaximized)
        {
            System.Windows.Forms.Screen sc = System.Windows.Forms.Screen.FromPoint(windowRect.Location);
            if (sc == null)
                sc = System.Windows.Forms.Screen.PrimaryScreen;
            System.Drawing.Rectangle rWorking = sc.Bounds;

            int top = windowRect.Top;
            int left = windowRect.Left;
            int width = windowRect.Width;
            int height = windowRect.Height;

            if (bMaximized)
            {
                top = rWorking.Top;
                left = rWorking.Left;
                width = rWorking.Width;
                height = rWorking.Height;
            }
            else
            {
                if (windowRect.Left > rWorking.Right || windowRect.Right < rWorking.Left)
                    left = rWorking.Right - windowRect.Width;
                if (windowRect.Top > rWorking.Bottom || windowRect.Bottom < rWorking.Top)
                    top = rWorking.Bottom - windowRect.Height;

                if (top + height > rWorking.Top + rWorking.Height)
                    height = rWorking.Height - (top - rWorking.Top);
                if (left + width > rWorking.Left + rWorking.Width)
                    width = rWorking.Width - (left - rWorking.Left);
            }
#if SMALL_SCREEN_TEST
            width = 1280;
            height = 800;
#endif
            return new System.Drawing.Rectangle(left, top, width, height);
        }

        private static int GetPercentage(int val, int pct)
        {
            int tmp = val / 100;
            return tmp * pct;
        }

        public static System.Drawing.Rectangle GetPercentageScreenRect(int top, int left, int width, int height, int widthPct, int heightPct) { return GetPercentageScreenRect(new System.Drawing.Rectangle(left, top, width, height), widthPct, heightPct); }
        public static System.Drawing.Rectangle GetPercentageScreenRect(System.Drawing.Rectangle windowRect, int widthPct, int heightPct)
        {
            System.Drawing.Rectangle fullRect = GetProperScreenRect(windowRect, true);
            int newWidth = GetPercentage(fullRect.Width, widthPct);
            int newHeight = GetPercentage(fullRect.Height, heightPct);
            int leftOffset = (int)((fullRect.Width - newWidth) / 2);
            int topOffset = (int)((fullRect.Height - newHeight) / 2);
            fullRect.Width = newWidth;
            fullRect.Height = newHeight;
            fullRect.X += leftOffset;
            fullRect.Y += topOffset;
            return fullRect;
        }

        private static double FixDoubleProp(double test, double backup)
        {
            if (test == double.NaN)
                return backup;
            else
                return test;
        }
        public static void SetupPercentageScreenRect(System.Windows.Window wnd, int widthPct, int heightPct)
        {
            if (wnd == null)
                return;
            System.Windows.Window wMain = System.Windows.Application.Current.MainWindow;
            System.Drawing.Rectangle rWnd = GetPercentageScreenRect((int)FixDoubleProp(wnd.Top, wMain.Top), (int)FixDoubleProp(wnd.Left, wMain.Left), (int)FixDoubleProp(wnd.ActualWidth, wMain.ActualWidth), (int)FixDoubleProp(wnd.ActualHeight, wnd.ActualHeight), widthPct, heightPct);
            wnd.Top = rWnd.Top;
            wnd.Left = rWnd.Left;
            wnd.Width = rWnd.Width;
            wnd.Height = rWnd.Height;
        }
        public static void Setup80PercentScreenRect(System.Windows.Window wnd)
        {
            SetupPercentageScreenRect(wnd, 80, 80);
        }
        public static void Setup60PercentScreenRect(System.Windows.Window wnd)
        {
            SetupPercentageScreenRect(wnd, 60, 60);
        }

        public static bool IsSmallScreenSize
        {
            get
            {
                System.Windows.Forms.Screen sc = System.Windows.Forms.Screen.PrimaryScreen;
                if (sc == null)
                    return false;
                return (sc.WorkingArea.Width <= 1024 || sc.WorkingArea.Height <= 768);
            }
        }
    }
}
