using System.IO;
using System.Text;
using Akka.Actor;

namespace WinTail
{
    public class TailActor : UntypedActor
    {
        #region Message types
        public class FileWrite
        {
            public FileWrite(string fileName)
            {
                this.FileName = fileName;
            }

            public string FileName { get; }
        }

        public class FileError
        {
            public FileError(string fileName, string reason)
            {
                this.FileName = fileName;
                this.Reason = reason;
            }

            public string FileName { get; }
            public string Reason { get; }
        }

        public class InitialRead
        {
            public InitialRead(string fileName, string text)
            {
                this.FileName = fileName;
                this.Text = text;
            }

            public string FileName { get; }
            public string Text { get; }
        }

        #endregion


        private readonly string _filePath;
        private readonly IActorRef _reporterActor;
        private FileObserver _observer;
        private Stream _fileStream;
        private StreamReader _fileStreamReader;

        public TailActor(IActorRef reporterActor, string filePath)
        {
            this._reporterActor = reporterActor;
            this._filePath = filePath;
        }

        protected override void PreStart()
        {
            this._observer = new FileObserver(Self, Path.GetFullPath(_filePath));
            _observer.Start();

            this._fileStream = new FileStream(
                Path.GetFullPath(this._filePath),
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            this._fileStreamReader = new StreamReader(this._fileStream, Encoding.UTF8);

            var text = this._fileStreamReader.ReadToEnd();
            Self.Tell(new InitialRead(this._filePath, text));
        }

        protected override void OnReceive(object message)
        {
            if (message is FileWrite)
            {
                var text = this._fileStreamReader.ReadToEnd();
                if (!string.IsNullOrEmpty(text))
                {
                    this._reporterActor.Tell(text);
                }
            }
            else if (message is FileError fileErrorMessage)
            {
                this._reporterActor.Tell($"Tail error: {fileErrorMessage.Reason}");
            }
            else if (message is InitialRead initialReadMessage)
            {
                this._reporterActor.Tell(initialReadMessage.Text);
            }
        }

        protected override void PostStop()
        {
            this._observer.Dispose();
            this._observer.Dispose();
            this._fileStreamReader.Close();
            this._fileStreamReader.Dispose();
            base.PostStop();
        }
    }
}
