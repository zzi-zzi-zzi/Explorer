using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Explorer
{
    public class TreeObject : INotifyPropertyChanged
    {
        public static event PropertyChangedEventHandler selectedItemChanged;
        private static TreeObject _selectedItem = null;
        // This is public get-only here but you could implement a public setter which also selects the item.
        // Also this should be moved to an instance property on a VM for the whole tree, otherwise there will be conflicts for more than one tree.
        public static TreeObject SelectedItem
        {
            get { return _selectedItem; }
            private set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    if (selectedItemChanged != null)
                        selectedItemChanged(_selectedItem.Tag, new PropertyChangedEventArgs("SelectedItem"));
                }
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                    if (_isSelected)
                    {
                        SelectedItem = this;
                    }
                }
            }
        }

        public string Header { get; set; }
        public List<TreeObject> _items = new List<TreeObject>();
        public List<TreeObject> Items { get { return _items; } set { _items = value; OnPropertyChanged("Items"); } }
        public object Tag { get; internal set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
