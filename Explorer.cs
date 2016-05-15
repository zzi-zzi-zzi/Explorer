using Buddy.BladeAndSoul.Game;
using Buddy.BladeAndSoul.Game.Objects;
using Buddy.BladeAndSoul.Game.UnrealEngine.Core;
using Buddy.BladeAndSoul.Infrastructure;
using Buddy.Common.Mvvm;
using Buddy.Engine;
using log4net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Explorer
{
    public class Explorer : IPlugin, IUIButtonProvider
    {
        private static ILog Log = LogManager.GetLogger("[Explorer]");
        #region IPLUGIN
        public string Author { get { return "zzi"; } }
        public bool CanBePulsed { get { return false; } }
        public bool Enabled { get; set; }
        public string Name { get { return "DevExplore"; } }
        public Version Version { get { return new Version(1, 0, 0); } }
        public void Initialize() { _settings = new Binding(); }
        public void OnRegistered()
        {
        }

        public void OnUnregistered()
        {
        }

        public void Pulse()
        {
        }

        public void Uninitialize()
        {
        }
        public string ButtonText { get { return "Explore"; } }
        #endregion
        
        private Window _gui;

        private Binding _settings;

        public void OnButtonClicked(object sender)
        {
            try
            {
                if (_gui == null)
                {
                    var uiPath = Path.Combine(AppSettings.Instance.FullPluginsPath, "Explorer");
                    _gui = new Window
                    {
                        DataContext = this._settings,
                        Content = WPFUtils.LoadWindowContent(Path.Combine(uiPath, "MainView.xaml")),
                        MinHeight = 400,
                        MinWidth = 200,
                        Title = "Explorer",
                        ResizeMode = ResizeMode.CanResizeWithGrip,

                        //SizeToContent = SizeToContent.WidthAndHeight,
                        SnapsToDevicePixels = true,
                        Topmost = false,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        WindowStyle = WindowStyle.SingleBorderWindow,
                        Owner = null,
                        Width = 550,
                        Height = 650,
                    };
                }
            }
            catch { }

            _gui.Show();
        }


    }

    public class Binding : INotifyPropertyChanged
    {
        private static ILog Log = LogManager.GetLogger("[Explorer]");
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if(PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public Binding()
        {
            Refresh();
            TreeObject.selectedItemChanged += (a, b) =>
            {
                SelectedItemChanged(a);
            };
        }

        private List<TreeObject> _objectlist;
        public List<TreeObject> Items
        {
            get
            {
                return _objectlist;
            }
            set
            {
                Log.Info("Updated object list");
                _objectlist = value;
                OnPropertyChanged("Items");
            }
        }
        public ICommand RefreshTree
        {
            get
            {
                return new RelayCommand(param =>
                {
                    Refresh();
                });
            }
        }

        public void SelectedItemChanged(object child)
        {
            
            if (child is Actor)
            {
                SelectedObjectView = child.Dump();
            }
        }

        public ICommand RefreshView
        {
            get
            {
                return new RelayCommand(param =>
                {
                    if(TreeObject.SelectedItem != null)
                        SelectedItemChanged(TreeObject.SelectedItem.Tag);
                });
            }
        }

        /// <summary>
        /// rebuild the treeview with new objects
        /// </summary>
        public void Refresh()
        {
            List<TreeObject> root = new List<TreeObject>();

            root.Add(Objects("Actors", GameManager.Actors));
            //root.Add(Objects("Objects", GameManager.Objects));

            Items = root;
        }
        public TreeObject Objects<T>(string header, IEnumerable<T> objects)
        {
            var ret = new TreeObject { Header = header };
            foreach(var obj in objects)
            {
                string Head;
                try
                {
                    var prop = typeof(T).GetProperty("Name");
                    System.Reflection.FieldInfo field;
                    if (prop != null)
                    {
                        Head = (string)prop.GetValue(obj);
                    }
                    else if ((field = typeof(T).GetField("Name")) != null)
                    {
                        Head = (string)field.GetValue(obj);
                    }
                    else
                    {
                        Head = obj.ToString();
                    }
                } catch{
                    Head = obj.ToString();
                }
                ret.Items.Add(new TreeObject { Header = Head, Tag = obj });
            }
            return ret;
        }

        public ICommand ExportSelected
        {
            get
            {
                return new RelayCommand(param =>
                {

                });
            }
        }
        public string _selectedObjectText;
        public string SelectedObjectView
        {
            get
            {
                return _selectedObjectText;
            }
            set
            {
                _selectedObjectText = value;
                OnPropertyChanged("SelectedObjectView");
            }
        }
    }
}
