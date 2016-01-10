using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RadioLog
{
    [DisplayName("Waveform Timeline")]
    [ToolboxItem(true)]
    [TemplatePart(Name = "PART_Waveform", Type = typeof(Canvas))]
    public class WaveformTimeline : Control
    {
        private Canvas waveformCanvas;
        private readonly Path soundPath = new Path();
        private readonly Line centerLine = new Line();

        private float[] soundData = null;

        private const int mouseMoveTolerance = 3;
        private const int indicatorTriangleWidth = 4;
        private const int majorTickHeight = 10;
        private const int minorTickHeight = 3;
        private const int timeStampMargin = 5;

        static WaveformTimeline()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WaveformTimeline), new FrameworkPropertyMetadata(typeof(WaveformTimeline)));
        }

        #region Sound Level
        public static readonly DependencyProperty SoundLevelBrushProperty = DependencyProperty.Register("SoundLevelBrush", typeof(Brush), typeof(WaveformTimeline), new UIPropertyMetadata(new SolidColorBrush(Colors.Red), OnSoundLevelBrushChanged, OnCoerceSoundLevelBrush));

        [Category("Brushes")]
        public Brush SoundLevelBrush
        {
            get { return (Brush)GetValue(SoundLevelBrushProperty); }
            set { SetValue(SoundLevelBrushProperty, value); }
        }

        private static object OnCoerceSoundLevelBrush(DependencyObject o, object value)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                return waveformTimeline.OnCoerceSoundLevelBrush((Brush)value);
            else
                return value;
        }
        private static void OnSoundLevelBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
            {
                waveformTimeline.OnSoundLevelBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
            }
        }

        protected virtual Brush OnCoerceSoundLevelBrush(Brush value)
        {
            return value;
        }
        protected virtual void OnSoundLevelBrushChanged(Brush oldValue, Brush newValue)
        {
            soundPath.Fill = SoundLevelBrush;
            UpdateWaveform();
        }
        #endregion

        #region Center Line
        public static readonly DependencyProperty CenterLineBrushProperty = DependencyProperty.Register("CenterLineBrush", typeof(Brush), typeof(WaveformTimeline), new UIPropertyMetadata(new SolidColorBrush(Colors.Black), OnCenterLineBrushChanged, OnCoerceCenterLineBrush));
        public static readonly DependencyProperty CenterLineThicknessProperty = DependencyProperty.Register("CenterLineThickness", typeof(double), typeof(WaveformTimeline), new UIPropertyMetadata(1.0d, OnCenterLineThicknessChanged, OnCoerceCenterLineThickness));

        [Category("Brushes")]
        public Brush CenterLineBrush
        {
            get { return (Brush)GetValue(CenterLineBrushProperty); }
            set { SetValue(CenterLineBrushProperty, value); }
        }

        [Category("Common")]
        public double CenterLineThickness
        {
            get { return (double)GetValue(CenterLineThicknessProperty); }
            set { SetValue(CenterLineThicknessProperty, value); }
        }

        private static object OnCoerceCenterLineBrush(DependencyObject o, object value)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                return waveformTimeline.OnCoerceCenterLineBrush((Brush)value);
            else
                return value;
        }
        private static void OnCenterLineBrushChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                waveformTimeline.OnCenterLineBrushChanged((Brush)e.OldValue, (Brush)e.NewValue);
        }
        private static object OnCoerceCenterLineThickness(DependencyObject o, object value)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                return waveformTimeline.OnCoerceCenterLineThickness((double)value);
            else
                return value;
        }
        private static void OnCenterLineThicknessChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                waveformTimeline.OnCenterLineThicknessChanged((double)e.OldValue, (double)e.NewValue);
        }

        protected virtual Brush OnCoerceCenterLineBrush(Brush value)
        {
            return value;
        }
        protected virtual void OnCenterLineBrushChanged(Brush oldValue, Brush newValue)
        {
            centerLine.Stroke = CenterLineBrush;
            UpdateWaveform();
        }
        protected virtual double OnCoerceCenterLineThickness(double value)
        {
            value = Math.Max(value, 0.0d);
            return value;
        }
        protected virtual void OnCenterLineThicknessChanged(double oldValue, double newValue)
        {
            centerLine.StrokeThickness = CenterLineThickness;
            UpdateWaveform();
        }
        #endregion

        #region Auto Scale
        public static readonly DependencyProperty AutoScaleWaveformCacheProperty = DependencyProperty.Register("AutoScaleWaveformCache", typeof(bool), typeof(WaveformTimeline), new UIPropertyMetadata(false, OnAutoScaleWaveformCacheChanged, OnCoerceAutoScaleWaveformCache));

        private static object OnCoerceAutoScaleWaveformCache(DependencyObject o, object value)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                return waveformTimeline.OnCoerceAutoScaleWaveformCache((bool)value);
            else
                return value;
        }
        private static void OnAutoScaleWaveformCacheChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            WaveformTimeline waveformTimeline = o as WaveformTimeline;
            if (waveformTimeline != null)
                waveformTimeline.OnAutoScaneWaveformCacheChanged((bool)e.OldValue, (bool)e.NewValue);
        }

        protected virtual bool OnCoerceAutoScaleWaveformCache(bool value)
        {
            return value;
        }
        protected virtual void OnAutoScaneWaveformCacheChanged(bool oldValue, bool newValue)
        {
            UpdateWaveformCacheScaling();
        }

        [Category("Common")]
        public bool AutoScaleWaveformCache
        {
            get { return (bool)GetValue(AutoScaleWaveformCacheProperty); }
            set { SetValue(AutoScaleWaveformCacheProperty, value); }
        }
        #endregion

        private double GetTotalTransformScale()
        {
            double totalTransform = 1.0d;
            DependencyObject currentVisualTreeElement = this;
            do
            {
                Visual visual = currentVisualTreeElement as Visual;
                if (visual != null)
                {
                    Transform transform = VisualTreeHelper.GetTransform(visual);
                    if ((transform != null) &&
                        (transform.Value.M12 == 0) &&
                        (transform.Value.M21 == 0) &&
                        (transform.Value.OffsetX == 0) &&
                        (transform.Value.OffsetY == 0) &&
                        (transform.Value.M11 == transform.Value.M22))
                    {
                        totalTransform *= transform.Value.M11;
                    }
                }
                currentVisualTreeElement = VisualTreeHelper.GetParent(currentVisualTreeElement);
            }
            while (currentVisualTreeElement != null);
            return totalTransform;
        }
        private void UpdateWaveformCacheScaling()
        {
            if (waveformCanvas == null)
                return;
            BitmapCache waveformCache = (BitmapCache)waveformCanvas.CacheMode;
            if (AutoScaleWaveformCache)
            {
                double totalTransformScale = GetTotalTransformScale();
                if (waveformCache.RenderAtScale != totalTransformScale)
                {
                    waveformCache.RenderAtScale = totalTransformScale;
                }
            }
            else
            {
                waveformCache.RenderAtScale = 1.0d;
            }
        }
        private void UpdateWaveform()
        {
            if (soundData == null || soundData.Length <= 1)
            {
                soundPath.Data = null;
                return;
            }

            const double minValue = 0;
            const double maxValue = 1.5;
            const double dbScale = (maxValue - minValue);
            int pointCount = soundData.Length;
            double pointThickness = waveformCanvas.RenderSize.Width / pointCount;
            double waveformSideHeight = waveformCanvas.RenderSize.Height / 2.0d;
            double centerHeight = waveformSideHeight;
            double soundRenderHeight;

            //centerline
            centerLine.X1 = 0;
            centerLine.X2 = waveformCanvas.RenderSize.Width;
            centerLine.Y1 = centerHeight;
            centerLine.Y2 = centerHeight;

            PolyLineSegment soundWaveformPolyline = new PolyLineSegment();
            soundWaveformPolyline.Points.Add(new Point(0, centerHeight));

            double xLocation = 0.0d;
            for (int i = 0; i < soundData.Length; i++)
            {
                xLocation = (i * pointThickness);
                soundRenderHeight = ((soundData[i] - minValue) / dbScale) * waveformSideHeight;
                soundWaveformPolyline.Points.Add(new Point(xLocation, centerHeight - soundRenderHeight));
            }

            soundWaveformPolyline.Points.Add(new Point(xLocation, centerHeight));
            soundWaveformPolyline.Points.Add(new Point(0, centerHeight));

            PathGeometry soundGeometry = new PathGeometry();
            PathFigure soundPathFigure = new PathFigure();
            soundPathFigure.Segments.Add(soundWaveformPolyline);
            soundGeometry.Figures.Add(soundPathFigure);

            soundPath.Data = soundGeometry;
        }
        private void UpdateAllRegions()
        {
            UpdateWaveform();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            waveformCanvas = GetTemplateChild("PART_Waveform") as Canvas;
            waveformCanvas.CacheMode = new BitmapCache();

            waveformCanvas.Background = new SolidColorBrush(Colors.Transparent);

            waveformCanvas.Children.Add(centerLine);
            waveformCanvas.Children.Add(soundPath);

            UpdateWaveformCacheScaling();
        }

        protected override void OnTemplateChanged(ControlTemplate oldTemplate, ControlTemplate newTemplate)
        {
            base.OnTemplateChanged(oldTemplate, newTemplate);

            if (waveformCanvas != null)
                waveformCanvas.Children.Clear();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            UpdateWaveformCacheScaling();
            UpdateAllRegions();
        }

        public void SetSoundData(float[] newData)
        {
            soundData = newData;
            UpdateAllRegions();
        }
    }
}
