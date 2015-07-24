using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using GalaSoft.MvvmLight;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    public class ApplicationSettings : ViewModelBase
    {
        private string _archiveFileName;
        private string _archiveFilePath;
        private List<PriorityColor> _priorityColors;
        private bool _syncOnStartup;
        private string _todoFileName;
        private string _todoFilePath;

        public string ArchiveFilePath
        {
            get
            {
                if (String.IsNullOrEmpty(_archiveFilePath))
                {
                    _archiveFilePath = GetSetting("archiveFilePath", "/todo");
                }

                return _archiveFilePath;
            }
            set
            {
                _archiveFilePath = value;
                IsolatedStorageSettings.ApplicationSettings["archiveFilePath"] = _archiveFilePath;
            }
        }

        public string ArchiveFileName
        {
            get
            {
                if (String.IsNullOrEmpty(_archiveFileName))
                {
                    _archiveFileName = GetSetting("archiveFileName", "done.txt");
                }

                return _archiveFileName;
            }
            set
            {
                _archiveFileName = value;
                IsolatedStorageSettings.ApplicationSettings["archiveFileName"] = _archiveFileName;
            }
        }

        public String TodoFilePath
        {
            get
            {
                if (String.IsNullOrEmpty(_todoFilePath))
                {
                    _todoFilePath = GetSetting("todoFilePath", "/todo");
                }

                return _todoFilePath;
            }
            set
            {
                _todoFilePath = value;
                IsolatedStorageSettings.ApplicationSettings["todoFilePath"] = _todoFilePath;
            }
        }

        public String TodoFileName
        {
            get
            {
                if (String.IsNullOrEmpty(_todoFileName))
                {
                    _todoFileName = GetSetting("todoFileName", "todo.txt");
                }

                return _todoFileName;
            }
            set
            {
                _todoFileName = value;

                IsolatedStorageSettings.ApplicationSettings["todoFileName"] = _todoFileName;
            }
        }

        public bool SyncOnStartup
        {
            get
            {
                _syncOnStartup = GetSetting("syncOnStartup", false);

                return _syncOnStartup;
            }
            set
            {
                _syncOnStartup = value;
                IsolatedStorageSettings.ApplicationSettings["syncOnStartup"] = _syncOnStartup;
            }
        }

        public void ResetColors()
        {
            foreach (var priorityColor in _priorityColors)
            {
                priorityColor.PropertyChanged -= PriorityColorOnPropertyChanged;
            }

            IsolatedStorageSettings.ApplicationSettings.Remove("priorityColors");
            _priorityColors = null;
        }

        private List<PriorityColor> GetDefaultColors()
        {
           var defaultColors = new List<PriorityColor>
                {
                    // TODO Clean up the look and feel of the color picker page
                    // TODO Test the color picker dialog in the light theme
                    new PriorityColor {ColorOption = ColorOptions.Yellow, Priority = "A"},
                    new PriorityColor {ColorOption = ColorOptions.Green, Priority = "B"},
                    new PriorityColor {ColorOption = ColorOptions.Cyan, Priority = "C"}
                };

            for (int i = 68; i < 91; i++)
            {
                var priority = string.Empty + (char)i;

                defaultColors.Add(new PriorityColor {ColorOption = ColorOptions.Default, Priority = priority});
            }

            return defaultColors;
        }

        public List<PriorityColor> PriorityColors
        {
            get
            {
                if (_priorityColors == null)
                {
                    _priorityColors = GetSetting("priorityColors", GetDefaultColors());
                    foreach (var priorityColor in _priorityColors)
                    {
                        priorityColor.PropertyChanged += PriorityColorOnPropertyChanged;
                    }
                }

                return _priorityColors;
            }
        }

        private void PriorityColorOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            IsolatedStorageSettings.ApplicationSettings["priorityColors"] = _priorityColors;
        }

        private static T GetSetting<T>(string setting, T defaultValue)
        {
            T value;
            return IsolatedStorageSettings.ApplicationSettings.TryGetValue(setting, out value) ? value : defaultValue;
        }
    }
}