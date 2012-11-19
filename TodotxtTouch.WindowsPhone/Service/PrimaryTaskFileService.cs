using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
    public class PrimaryTaskFileService : TaskFileService
    {
        public PrimaryTaskFileService(DropboxService dropBoxService, ApplicationSettings settings)
            : base(dropBoxService, settings)
        {
        }

        protected override string GetFilePath()
        {
            return Settings.TodoFilePath;
        }

        protected override string GetFileName()
        {
            return Settings.TodoFileName;
        }
    }
}