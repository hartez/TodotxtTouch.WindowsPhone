using System.Windows;
using Microsoft.Phone.Controls;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
    public partial class CustomizeColorsPage : PhoneApplicationPage
    {
        public CustomizeColorsPage()
        {
            InitializeComponent();
        }

        private void ResetColors_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("This will reset the priority colors to their default values. Are you sure?",
                "Reset Colors", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                ((ApplicationSettingsViewModel)DataContext).ResetColorsCommand.Execute(null);
            }
        }
    }
}