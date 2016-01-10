using System;
using System.Windows.Data;

namespace RadioLog.WPFCommon
{
    public abstract class BaseValueConverter : IValueConverter
    {
        public abstract object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture);

        public virtual object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PropertyEnabledToVisibilityConverter : BaseValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return System.Windows.Visibility.Collapsed;
            bool b=false;
            if (value.GetType() == typeof(bool?))
                b = ((bool?)value == true);
            else if (value.GetType() == typeof(bool))
                b = (bool)value;
            if (b)
                return System.Windows.Visibility.Visible;
            else
                return System.Windows.Visibility.Collapsed;
        }
    }

    public class TimestampViewConverter : BaseValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;
            DateTime? dt = (DateTime?)value;
            if (dt == null || !dt.HasValue)
                return string.Empty;
            return dt.Value.ToString("H:mm:ss");
        }
    }

    public abstract class AccentColorBrushConverter : BaseValueConverter
    {
        protected static System.Windows.Media.Brush _disabledBrush = null;
        protected static System.Windows.Media.Brush _disconnectedBrush = null;
        protected static System.Windows.Media.Brush _normalBrush = null;
        protected static System.Windows.Media.Brush _highlightdBrush = null;

        protected static void SetupBrushes()
        {
            if (_highlightdBrush == null)
            {
                object o = System.Windows.Application.Current.FindResource("AccentColorBrush");
                if (o == null)
                    _highlightdBrush = System.Windows.Media.Brushes.Transparent;
                else
                    _highlightdBrush = (System.Windows.Media.Brush)o;
            }
            if (_normalBrush == null)
            {
                object o = System.Windows.Application.Current.FindResource("ControlBackgroundBrush");
                if (o == null)
                    _normalBrush = System.Windows.Media.Brushes.Transparent;
                else
                    _normalBrush = (System.Windows.Media.Brush)o;
            }
            if (_disabledBrush == null)
            {
                object o = System.Windows.Application.Current.FindResource("GrayBrush4");
                if (o == null)
                    _disabledBrush = System.Windows.Media.Brushes.Transparent;
                else
                    _disabledBrush = (System.Windows.Media.Brush)o;
            }
            if (_disconnectedBrush == null)
            {
                object o = System.Windows.Application.Current.FindResource("GrayBrush8");
                if (o == null)
                    _disconnectedBrush = System.Windows.Media.Brushes.Transparent;
                else
                    _disconnectedBrush = (System.Windows.Media.Brush)o;
            }
        }
        public static void CleanupBrushes()
        {
            _disabledBrush = null;
            _disconnectedBrush = null;
            _normalBrush = null;
            _highlightdBrush = null;
        }
        protected static System.Windows.Media.Brush GetHighlighBrushFromColorName(string strColor)
        {
            if (string.IsNullOrWhiteSpace(strColor))
                return _highlightdBrush;
            System.Windows.Media.Brush rslt = ColorHelper.GetBrushByKnownName(strColor);
            if (rslt == null)
                return _highlightdBrush;
            else
                return rslt;
        }
        public static System.Windows.Media.Brush GetStatusBrush(RadioLog.Common.SignalingSourceStatus stat, string colorName)
        {
            SetupBrushes();
            switch (stat)
            {
                case Common.SignalingSourceStatus.AudioActive: return GetHighlighBrushFromColorName(colorName);
                case Common.SignalingSourceStatus.Disabled: return _disabledBrush;
                case Common.SignalingSourceStatus.Disconnected: return _disconnectedBrush;
                case Common.SignalingSourceStatus.FeedNotActive: return _disconnectedBrush;
                case Common.SignalingSourceStatus.Muted: return _disconnectedBrush;
                default: return _normalBrush;
            }
        }
    }

    public class GroupColorToBrushConverter : AccentColorBrushConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SetupBrushes();
            if (value == null)
                return _highlightdBrush;
            string strBrushName = value as string;
            if (string.IsNullOrWhiteSpace(strBrushName))
                return _highlightdBrush;
            System.Windows.Media.Brush rslt = ColorHelper.GetBrushByKnownName(strBrushName);
            if (rslt == null)
                return _highlightdBrush;
            else
                return rslt;
        }
    }
    public class SourceColorToBrushConverter : AccentColorBrushConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            SetupBrushes();
            if (value == null)
                return _disabledBrush;
            string strBrushName = value as string;
            if (string.IsNullOrWhiteSpace(strBrushName))
                return _highlightdBrush;
            System.Windows.Media.Brush rslt = ColorHelper.GetBrushByKnownName(strBrushName);
            if (rslt == null)
                return _highlightdBrush;
            else
                return rslt;
        }
    }

    public class RadioLogItemEditorVisible : BaseValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return System.Windows.Visibility.Collapsed;
            Common.RadioSignalingItem item = value as Common.RadioSignalingItem;
            if (item == null)
                return System.Windows.Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(item.UnitId) || item.SourceType == Common.SignalingSourceType.StreamingTag)
                return System.Windows.Visibility.Collapsed;
            return System.Windows.Visibility.Visible;
        }
    }

    public class SourceWidthConverter : BaseValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return 200;
            double dWidth = (double)value;
            return (dWidth - 24);
        }
    }

    public class NotCheckedValueConverter : BaseValueConverter
    {
        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = false;
            if (value != null)
            {
                if (value.GetType() == typeof(bool?))
                {
                    b = (bool?)value == true;
                }
                else if (value.GetType() == typeof(bool))
                {
                    b = (bool)value;
                }
                else
                    b = true;
            }
            return !b;
        }
    }
}
