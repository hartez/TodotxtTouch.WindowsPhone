using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Windows.Media;
using GalaSoft.MvvmLight;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    public class PriorityColor
    {
        public string Priority { get; set; }
        public ColorOption Color { get; set; }
    }

    public static class ColorOptions
    {
        static ColorOptions()
        {
            Yellow = new ColorOption {Name = "Yellow", Color = Colors.Yellow};
            Cyan = new ColorOption {Name = "Cyan", Color = Colors.Cyan};
            Green = new ColorOption {Name = "Green", Color = Colors.Green};

            All = new List<ColorOption>
            {
                Green,
                Yellow,
                Cyan,
                new ColorOption { Name = "Blue", Color = Colors.Blue },
                new ColorOption { Name = "Red", Color = Colors.Red },
                new ColorOption { Name = "Magenta", Color = Colors.Magenta }
            };
        }

        public static ColorOption Yellow { get; private set; }
        public static ColorOption Cyan { get; private set; }
        public static ColorOption Green { get; private set; }
        public static List<ColorOption> All { get; private set; }
    }

    public class ColorOption
    {
        public string Name { get; set; }
        public Color? Color { get; set; }
    }

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

        public List<PriorityColor> PriorityColors
        {
            get
            {
                _priorityColors = GetSetting("priorityColors", new List<PriorityColor>
                {
                    // TODO Set up the rest of the default colors (including the blank ones)
                    // TODO Once default colors work, fix TaskValueConverter to get its values from this setting
                    // TODO Clean up the look and feel of the color picker page
                    // TODO Add the other possible colors to the color list and order them
                    // TODO Verify that the color settings actually persist to isolated storage
                    // TODO Test the color picker dialog in the light theme
                    new PriorityColor {Color = ColorOptions.Yellow, Priority = "A"},
                    new PriorityColor {Color = ColorOptions.Green, Priority = "B"},
                    new PriorityColor {Color = ColorOptions.Cyan, Priority = "C"}
                });

                return _priorityColors;
            }
            set
            {
                _priorityColors = value;

                IsolatedStorageSettings.ApplicationSettings["priorityColors"] = _priorityColors;
            }
        }

        private static T GetSetting<T>(string setting, T defaultValue)
        {
            T value;
            return IsolatedStorageSettings.ApplicationSettings.TryGetValue(setting,
                out value)
                ? value
                : defaultValue;
        }
    }
}