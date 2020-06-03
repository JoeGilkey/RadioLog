using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace ResponderApps.EditorDisplay
{
    public class SectionHeadingEditorItem:BaseEditorItem
    {
        private System.Windows.Controls.TextBlock _headingTextBlock = null;

        public SectionHeadingEditorItem(string itemKey, string itemTitle)
            : base(itemKey, itemTitle)
        {
            //
        }

        protected override void OnItemTitleChanged()
        {
            base.OnItemTitleChanged();
            if (_headingTextBlock != null)
            {
                _headingTextBlock.Text = this.ItemTitle;
            }
        }
        protected override System.Windows.Thickness DefaultItemMargin()
        {
            return new System.Windows.Thickness(4, 8, 4, 2);
        }
        public override System.Windows.FrameworkElement GenerateItem()
        {
            if (_headingTextBlock == null)
            {
                _headingTextBlock = new System.Windows.Controls.TextBlock();
                _headingTextBlock.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                _headingTextBlock.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _headingTextBlock.Text = this.ItemTitle;
                _headingTextBlock.FontWeight = System.Windows.FontWeights.Bold;
                Grid.SetRowSpan(_headingTextBlock, 2);
            }
            Grid grd = GenerateBaseGrid();
            grd.Children.Add(_headingTextBlock);
            return grd;
        }
    }
}
