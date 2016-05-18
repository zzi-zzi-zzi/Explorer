using Buddy.BladeAndSoul.Game;
using Buddy.BladeAndSoul.Game.Objects;
using Buddy.BladeAndSoul.Game.UnrealEngine.Core;
using Buddy.BladeAndSoul.Infrastructure;
using Buddy.Common.Mvvm;
using Buddy.Engine;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;

namespace Explorer
{
    public class Explorer : IPlugin, IUIButtonProvider
    {
        private static ILog Log = LogManager.GetLogger("[Explorer]");
        #region IPLUGIN
        public string Author { get { return "zzi"; } }
        public bool CanBePulsed { get { return true; } }
        public bool Enabled { get; set; }
        public string Name { get { return "Explorer"; } }
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

                    _gui.Closed += (s, e) =>
                    {
                        _gui = null;
                    };
                }
            }
            catch { }

            _gui.Show();
        }

        public override Task CoroutineImplementation()
        {
            throw new NotImplementedException();
        }
    }

    public class Binding : INotifyPropertyChanged
    {
        private static ILog Log = LogManager.GetLogger("[Explorer]");
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
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

        private ObservableCollection<TreeObject> _objectlist;
        public ObservableCollection<TreeObject> Items
        {
            get
            {
                return _objectlist;
            }
            set
            {
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
                    Log.Info("Refresh");
                    Refresh();
                });
            }
        }

        public void SelectedItemChanged(object child)
        {

            if (child is long)
            {
                Log.InfoFormat("Show Actor: {0}", child);
                GameEngine.AttachedProcess.Memory.ClearCache();
                using (GameEngine.AttachedProcess.Memory.AcquireFrame(true))
                {
                    var actor = GameManager.GetActorById((long)child);
                    if (actor != null && actor.IsValid)
                        SelectedObjectView = actor.DumpInternal(true);
                    else
                        SelectedObjectView = "Not Valid";
                }
                return;
            }

            SelectedObjectView = string.Format("Unknown Type {0}", child.GetType().ToString());

        }

        public ICommand RefreshView
        {
            get
            {
                return new RelayCommand(param =>
                {
                    if (TreeObject.SelectedItem != null)
                        SelectedItemChanged(TreeObject.SelectedItem.Tag);
                });
            }
        }

        /// <summary>
        /// rebuild the treeview with new objects
        /// </summary>
        public void Refresh()
        {
            ObservableCollection<TreeObject> root = new ObservableCollection<TreeObject>();

            root.Add(Objects(string.Format("Actors ({0})", GameManager.Actors.Count()), GameManager.Actors));
            //root.Add(Objects(String.Format("Objects ({0})", GameManager.Objects.Count), GameManager.Objects))); //this is generally really big and won't be able to open it.
            root.Add(Objects(String.Format("Pulsators ({0})", GameEngine.BotPulsator.PulsableObjects.Count), GameEngine.BotPulsator.PulsableObjects));
            Items = root;
        }
        public TreeObject Objects<T>(string header, IEnumerable<T> objects)
        {
            var ret = new TreeObject { Header = header };

            foreach (var obj in objects)
            {
                string Head;
                object Value;
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
                }
                catch
                {
                    Head = obj.ToString();
                }

                try
                {
                    var f = typeof(T).GetProperty("IsValid");
                    bool valid = (bool)f.GetValue(obj);
                    var id = typeof(T).GetProperty("Id");
                    long aid = (long)id.GetValue(obj);
                    if (valid)
                        Value = aid;
                    else
                        Value = "obj is not IsValid";
                }
                catch
                {
                    Value = obj.ToString();
                }

                ret.Items.Add(new TreeObject { Header = Head, Tag = Value });
            }
            return ret;
        }

        public ICommand ExportSelected
        {
            get
            {
                return new RelayCommand(param =>
                {
                    Stream myStream;
                    SaveFileDialog saveFileDialog1 = new SaveFileDialog();

                    saveFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog1.FilterIndex = 2;
                    saveFileDialog1.RestoreDirectory = true;

                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        if ((myStream = saveFileDialog1.OpenFile()) != null)
                        {
                            var b = System.Text.Encoding.UTF8.GetBytes(SelectedObjectView);
                            myStream.Write(b, 0, b.Length);
                            myStream.Close();
                        }
                    }
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
    public static class Extensions
    {
        private static ILog Log = LogManager.GetLogger("[Explorer]");
        public static string DumpInternal(this object obj, bool shortTypeName = true)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(obj))
            {
                Log.InfoFormat("[Export] trying type: {0}", propertyDescriptor.Name);
                object obj2;
                try
                {
                    object value = propertyDescriptor.GetValue(obj);
                    if (value is int)
                    {
                        int i = (int)value;
                        obj2 = i.ToString("X");
                    }
                    else if (value is uint)
                    {
                        obj2 = ((uint)value).ToString("X");
                    }
                    else if (value is long)
                    {
                        obj2 = ((long)value).ToString("X");
                    }
                    else if (value is ulong)
                    {
                        obj2 = ((ulong)value).ToString("X");
                    }
                    else if (value is IntPtr)
                    {
                        obj2 = ((IntPtr)value).ToString("X8");
                    }
                    else if (value is Array)
                    {
                        StringBuilder stringBuilder2 = new StringBuilder();
                        stringBuilder2.AppendLine();
                        Array array = value as Array;
                        for (int j = 0; j < array.Length; j++)
                        {
                            stringBuilder2.AppendLine(string.Format("\t[{0}] = ", j) + array.GetValue(j));
                        }
                        obj2 = stringBuilder2.ToString();
                    }
                    else
                    {
                        obj2 = propertyDescriptor.GetValue(obj).ToString();
                    }
                }
                catch (Exception ex)
                {
                    obj2 = "Exception: " + ex.Message;
                }
                stringBuilder.AppendLine(string.Concat(new object[]
                {
                    "[",
                    shortTypeName ? propertyDescriptor.PropertyType.Name : propertyDescriptor.PropertyType.FullName,
                    "] ",
                    propertyDescriptor.Name,
                    " = ",
                    obj2
                }));
            }
            FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo fieldInfo = fields[i];
                object obj3;
                try
                {
                    object value2 = fieldInfo.GetValue(obj);
                    if (value2 is int)
                    {
                        obj3 = ((int)value2).ToString("X");
                    }
                    else if (value2 is uint)
                    {
                        obj3 = ((uint)value2).ToString("X");
                    }
                    else if (value2 is long)
                    {
                        obj3 = ((long)value2).ToString("X");
                    }
                    else if (value2 is ulong)
                    {
                        obj3 = ((ulong)value2).ToString("X");
                    }
                    else if (value2 is IntPtr)
                    {
                        obj3 = ((IntPtr)value2).ToString("X8");
                    }
                    else if (value2 is Array)
                    {
                        StringBuilder stringBuilder3 = new StringBuilder();
                        stringBuilder3.AppendLine();
                        Array array2 = value2 as Array;
                        for (int k = 0; k < array2.Length; k++)
                        {
                            stringBuilder3.AppendLine(string.Format("\t[{0}] = ", k) + array2.GetValue(k));
                        }
                        obj3 = stringBuilder3.ToString();
                    }
                    else
                    {
                        obj3 = fieldInfo.GetValue(obj).ToString();
                    }
                }
                catch (Exception ex2)
                {
                    obj3 = "Exception: " + ex2.Message;
                }
                stringBuilder.AppendLine(string.Concat(new object[]
                {
                    "[",
                    shortTypeName ? fieldInfo.FieldType.Name : fieldInfo.FieldType.FullName,
                    "] ",
                    fieldInfo.Name,
                    " = ",
                    obj3
                }));
            }
            return stringBuilder.ToString();
        }
    }
}
