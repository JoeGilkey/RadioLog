using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ResponderApps.EditorDisplay
{
    /// <summary>
    /// Interaction logic for EditorControl.xaml
    /// </summary>
    public partial class EditorControl : UserControl
    {
        public const string EDITOR_NAME_COL = "EDITOR_NAME_COL";
        private List<BaseEditorItem> _items = new List<BaseEditorItem>();

        public event EventHandler<EditValueChangedArgs> OnEditValueChanged;

        public EditorControl()
        {
            InitializeComponent();
        }

        internal void EditValueChanged(string itemKey)
        {
            if (OnEditValueChanged != null)
            {
                OnEditValueChanged(this, new EditValueChangedArgs(itemKey));
            }
        }

        public void GenerateDisplay()
        {
            foreach (BaseEditorItem item in _items)
            {
                FrameworkElement elem = item.GenerateItem();
                if (elem != null)
                {
                    rootLayout.Children.Add(elem);
                }
            }
        }

        public void AddEditorItem(BaseEditorItem item)
        {
            if (item == null)
                return;
            if (GetEditorItem(item.ItemKey) == null)
            {
                item.SetEditorControl(this);
                _items.Add(item);
            }
        }
        public BaseEditorItem GetEditorItem(string itemKey)
        {
            return _items.FirstOrDefault(itm => itm.ItemKey == itemKey);
        }
    }

    public class EditValueChangedArgs : EventArgs
    {
        public string ItemKey { get; private set; }

        public EditValueChangedArgs(string itemKey)
        {
            this.ItemKey = itemKey;
        }
    }
}
