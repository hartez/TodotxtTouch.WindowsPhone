using System.Windows.Media;

namespace TodotxtTouch.WindowsPhone.ViewModel
{
    public class ColorOption
    {
        public string Name { get; set; }
        public Color? Color { get; set; }

        protected bool Equals(ColorOption other)
        {
            return string.Equals(Name, other.Name) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((ColorOption) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name?.GetHashCode() ?? 0) * 397) ^ Color.GetHashCode();
            }
        }
    }
}