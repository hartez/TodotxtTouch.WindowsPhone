using System.ComponentModel;
using TodotxtTouch.WindowsPhone.Annotations;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    public class PriorityColor : INotifyPropertyChanged
    {
        private ColorOption _colorOption;
        public string Priority { get; set; }

        public ColorOption ColorOption
        {
            get { return _colorOption; }
            set
            {
                if (Equals(value, _colorOption))
                {
                    return;
                }
                _colorOption = value;
                OnPropertyChanged(nameof(ColorOption));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
	        handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}