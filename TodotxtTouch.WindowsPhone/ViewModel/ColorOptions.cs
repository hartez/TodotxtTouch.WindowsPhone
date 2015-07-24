using System.Collections.Generic;
using System.Windows.Media;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    public static class ColorOptions
    {
        // TODO Adjust the look and feel of the color options
        // TODO Adjust the look and feel of the Priority Colors

        static ColorOptions()
        {
            Default = new ColorOption {Name = "Default", Color = null};
            Yellow = new ColorOption {Name = "Yellow", Color = Colors.Yellow};
            Cyan = new ColorOption {Name = "Cyan", Color = Colors.Cyan};
            Green = new ColorOption {Name = "Green", Color = Colors.Green};

            All = new List<ColorOption>
            {
                Default,
                Green,
                Yellow,
                Cyan,
                new ColorOption { Name = "Black", Color = Colors.Black },
                new ColorOption { Name = "Blue", Color = Colors.Blue },
                new ColorOption { Name = "Brown", Color = Colors.Brown },
                new ColorOption { Name = "Dark Gray", Color = Colors.DarkGray },
                new ColorOption { Name = "Gray", Color = Colors.Gray },
                new ColorOption { Name = "Light Gray", Color = Colors.LightGray },
                new ColorOption { Name = "Magenta", Color = Colors.Magenta },
                new ColorOption { Name = "Orange", Color = Colors.Orange },
                new ColorOption { Name = "Purple", Color = Colors.Purple },
                new ColorOption { Name = "Red", Color = Colors.Red },
                new ColorOption { Name = "White", Color = Colors.White }
            };
        }

        public static ColorOption Default { get; private set; }
        public static ColorOption Yellow { get; private set; }
        public static ColorOption Cyan { get; private set; }
        public static ColorOption Green { get; private set; }
        public static List<ColorOption> All { get; private set; }
    }
}