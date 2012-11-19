using System;
using System.Windows;
using System.Windows.Navigation;
using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone
{
    public partial class MultiSelectPage : TaskFilterPage
    {
        public MultiSelectPage()
        {
            InitializeComponent();
        }

        private bool _navigationInProgress;

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if(_navigationInProgress)
            {
                e.Cancel = true;
                return;
            }
            _navigationInProgress = true;
            base.OnNavigatingFrom(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            _navigationInProgress = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            BroadCastFilter();
        }

        private void MarkComplete_Click(object sender, EventArgs e)
        {
            ((MainViewModel) DataContext).MarkSelectedTasksCompleteCommand.Execute(null);

            if(NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void Delete_Click(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Are you sure?", "Delete Tasks", MessageBoxButton.OKCancel);

            if(result == MessageBoxResult.OK)
            {
                ((MainViewModel) DataContext).RemoveSelectedTasksCommand.Execute(null);
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
        }
    }
}