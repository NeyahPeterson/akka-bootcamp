using System;
using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileObserver : IDisposable
    {
        private readonly IActorRef tailActor;
        private readonly string absoluteFilePath;
        private readonly string fileDir;
        private readonly string fileNameOnly;
        private FileSystemWatcher watcher;

        public FileObserver(IActorRef tailActor, string absoluteFilePath)
        {
            this.tailActor = tailActor;
            this.absoluteFilePath = absoluteFilePath;
            this.fileDir = Path.GetDirectoryName(absoluteFilePath);
            this.fileNameOnly = Path.GetFileName(absoluteFilePath);
        }

        public void Start()
        {
            this.watcher = new FileSystemWatcher(this.fileDir, this.fileNameOnly);

            this.watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;

            this.watcher.Changed += OnFileChanged;
            this.watcher.Error += OnFileError;

            this.watcher.EnableRaisingEvents = true;
        }

        public void Dispose()
        {
            this.watcher.Dispose();
        }

        private void OnFileError(object sender, ErrorEventArgs e)
        {
            this.tailActor.Tell(
                new TailActor.FileError(this.fileNameOnly, e.GetException().Message), 
                ActorRefs.NoSender);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                this.tailActor.Tell(new TailActor.FileWrite(e.Name), ActorRefs.NoSender);
            }
        }


    }
}
