using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Gma.QrCodeNet.Encoding.Windows.Render;
using Gma.QrCodeNet.Encoding;

namespace ResponderApps.EditorDisplay
{
    public class QRCodeEditorItem : BaseEditorItem
    {
        private string _itemUrl = string.Empty;
        private Border _borderQRCode = null;
        private System.Windows.Shapes.Rectangle _qrCodeRect = null;

        public QRCodeEditorItem(string itemKey, string itemTitle, string itemURL)
            : base(itemKey, itemTitle)
        {
            _itemUrl = itemURL;
        }

        public string ItemURL
        {
            get { return _itemUrl; }
            set
            {
                if (_itemUrl != value)
                {
                    _itemUrl = value;

                    if (_qrCodeRect != null)
                    {
                        GenerateQRCode();
                    }
                }
            }
        }

        protected void GenerateQRCode()
        {
            if (_borderQRCode == null || _qrCodeRect == null)
                return;

            if (string.IsNullOrWhiteSpace(ItemURL))
            {
                _borderQRCode.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                QrEncoder encoder = new QrEncoder(ErrorCorrectionLevel.M);
                QrCode qrCode;
                if (encoder.TryEncode(ItemURL, out qrCode))
                {
                    DrawingBrushRenderer dRenderer = new DrawingBrushRenderer(new FixedModuleSize(2, QuietZoneModules.Two), System.Windows.Media.Brushes.Black, System.Windows.Media.Brushes.White);
                    System.Windows.Media.DrawingBrush dBrush = dRenderer.DrawBrush(qrCode.Matrix);
                    _qrCodeRect.Fill = dBrush;
                    _borderQRCode.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    _borderQRCode.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            catch
            {
                _borderQRCode.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        protected override System.Windows.Thickness DefaultItemMargin()
        {
            return new System.Windows.Thickness(4, 8, 4, 2);
        }
        public override System.Windows.FrameworkElement GenerateItem()
        {
            if (_borderQRCode == null)
            {
                _borderQRCode = new Border();
                _borderQRCode.Background = System.Windows.Media.Brushes.LightGray;
                _borderQRCode.HorizontalAlignment = HorizontalAlignment.Center;
                _borderQRCode.VerticalAlignment = VerticalAlignment.Center;
                if (RadioLog.Common.ScreenHelper.IsSmallScreenSize)
                {
                    _borderQRCode.Width = 120;
                    _borderQRCode.Height = 120;
                    _borderQRCode.Margin = new Thickness(12);
                }
                else
                {
                    _borderQRCode.Width = 180;
                    _borderQRCode.Height = 180;
                    _borderQRCode.Margin = new Thickness(24);
                }
                Grid.SetColumnSpan(_borderQRCode, 2);
            }
            if (_qrCodeRect == null)
            {
                _qrCodeRect = new System.Windows.Shapes.Rectangle();
                _qrCodeRect.HorizontalAlignment = HorizontalAlignment.Stretch;
                _qrCodeRect.VerticalAlignment = VerticalAlignment.Stretch;
                _qrCodeRect.Margin = new Thickness(8);
                _borderQRCode.Child = _qrCodeRect;
            }

            GenerateQRCode();

            Grid grd = GenerateBaseGrid();
            grd.Children.Add(_borderQRCode);
            return grd;
        }
    }
}
