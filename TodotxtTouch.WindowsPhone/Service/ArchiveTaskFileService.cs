using TodotxtTouch.WindowsPhone.ViewModel;

namespace TodotxtTouch.WindowsPhone.Service
{
    public class ArchiveTaskFileService : TaskFileService
    {
        public ArchiveTaskFileService(DropboxService dropBoxService, ApplicationSettings settings)
            : base(dropBoxService, settings)
        {
        }

        protected override string GetFilePath()
        {
            return Settings.ArchiveFilePath;
        }

        protected override string GetFileName()
        {
            return Settings.ArchiveFileName;
        }
    }
}