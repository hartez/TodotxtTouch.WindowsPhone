using System;

namespace TodotxtTouch.WindowsPhone.Service
{
    public class LocalHasChangesChangedEventArgs : EventArgs
    {
        public bool LocalHasChanges { get; private set; }

        public LocalHasChangesChangedEventArgs(bool localHasChanges)
        {
            LocalHasChanges = localHasChanges;
        }
    }
}