using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ResponderApps.EditorDisplay
{
    public abstract class BaseEditorItem
    {
        private EditorControl _currentEditorControl = null;

        private System.Windows.Controls.Grid _layoutGrid = null;
        private string _itemTitle;
        private System.Windows.Thickness _itemMargin;
        private bool _isEnabled=true;
        private Visibility _visibility=  Visibility.Visible;

        public BaseEditorItem(string itemKey) : this(itemKey, string.Empty) { }
        public BaseEditorItem(string itemKey, string itemTitle)
        {
            _itemTitle = itemTitle;
            this.ItemKey = itemKey;
            _itemMargin = DefaultItemMargin();
        }

        public string ItemKey { get; private set; }
        public string ItemTitle
        {
            get { return _itemTitle; }
            set
            {
                if (_itemTitle != value)
                {
                    _itemTitle = value;
                    OnItemTitleChanged();
                }
            }
        }
        public System.Windows.Thickness ItemMargin
        {
            get { return _itemMargin; }
            set
            {
                if (_itemMargin != value)
                {
                    _itemMargin = value;
                    OnItemMarginChanged();
                }
            }
        }

        internal void SetEditorControl(EditorControl ctrl)
        {
            _currentEditorControl = ctrl;
        }

        protected void RaiseValueChanged()
        {
            if (_currentEditorControl != null)
            {
                _currentEditorControl.EditValueChanged(this.ItemKey);
            }
        }

        protected virtual System.Windows.Thickness DefaultItemMargin()
        {
            return new System.Windows.Thickness(4);
        }
        protected virtual void OnItemTitleChanged() { }
        protected virtual void OnIsEnabledChanged() { }
        protected virtual void OnItemMarginChanged()
        {
            if (_layoutGrid != null)
            {
                _layoutGrid.Margin = ItemMargin;
            }
        }
        protected virtual System.Windows.Controls.Grid GenerateBaseGrid()
        {
            if (_layoutGrid == null)
            {
                _layoutGrid = new System.Windows.Controls.Grid();
                _layoutGrid.ColumnDefinitions.Clear();
                _layoutGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition() { SharedSizeGroup = EditorControl.EDITOR_NAME_COL, Width = System.Windows.GridLength.Auto });
                _layoutGrid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition() { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                _layoutGrid.Margin = ItemMargin;
                _layoutGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                _layoutGrid.Visibility = this.Visibility;
            }
            return _layoutGrid;
        }

        public abstract System.Windows.FrameworkElement GenerateItem();

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnIsEnabledChanged();
                }
            }
        }
        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                if (_visibility != value)
                {
                    _visibility = value;
                    if (_layoutGrid != null)
                    {
                        _layoutGrid.Visibility = _visibility;
                    }
                }
            }
        }
    }

    public abstract class BaseTitledEditorItem : BaseEditorItem
    {
        private TextBlock _tbTitle = null;
        private Thickness _titleMargin = new Thickness(2);

        public BaseTitledEditorItem(string itemKey, string itemTitle)
            : base(itemKey, itemTitle)
        {
            _titleMargin = DefaultItemTitleMargin();
        }

        protected virtual System.Windows.Thickness DefaultItemTitleMargin()
        {
            return new System.Windows.Thickness(2);
        }

        public Thickness TitleMargin
        {
            get { return _titleMargin; }
            set
            {
                if (_titleMargin != value)
                {
                    _titleMargin = value;
                    if (_tbTitle != null)
                    {
                        _tbTitle.Margin = _titleMargin;
                    }
                }
            }
        }

        protected abstract FrameworkElement GenerateEditorItem();
        protected override void OnItemTitleChanged()
        {
            base.OnItemTitleChanged();
            if (_tbTitle != null)
            {
                _tbTitle.Text = this.ItemTitle;
            }
        }
        public override System.Windows.FrameworkElement GenerateItem()
        {
            if (_tbTitle == null)
            {
                _tbTitle = new TextBlock();
                _tbTitle.Margin = _titleMargin;
                _tbTitle.HorizontalAlignment = HorizontalAlignment.Right;
                _tbTitle.VerticalAlignment = VerticalAlignment.Center;
                _tbTitle.Text = this.ItemTitle;
                _tbTitle.FontWeight = FontWeights.Bold;
                Grid.SetColumn(_tbTitle, 0);
                Grid.SetRow(_tbTitle, 0);
            }
            Grid grd = GenerateBaseGrid();
            grd.Children.Add(_tbTitle);
            FrameworkElement feEditor = GenerateEditorItem();
            if (feEditor != null)
            {
                Grid.SetColumn(feEditor, 1);
                Grid.SetRow(feEditor, 0);
                grd.Children.Add(feEditor);
            }
            return grd;
        }
    }
}
