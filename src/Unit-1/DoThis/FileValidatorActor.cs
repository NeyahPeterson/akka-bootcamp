using System.IO;
using Akka.Actor;

namespace WinTail
{
    public class FileValidatorActor : UntypedActor
    {
        private readonly IActorRef _consoleWriterActor;
        private readonly IActorRef _tailCoordinatorActor;

        public FileValidatorActor(IActorRef consoleWriterActor, IActorRef tailCoordinatorActor)
        {
            _consoleWriterActor = consoleWriterActor;
            _tailCoordinatorActor = tailCoordinatorActor;
        }

        protected override void OnReceive(object message)
        {
            var msg = message as string;
            if (string.IsNullOrEmpty(msg))
            {
                _consoleWriterActor.Tell(new Messages.NullinputError("Input was blank.  Please try again.\r\n"));

                Sender.Tell(new Messages.ContinueProcessing());
            }
            else
            {
                if (IsFileUri(msg))
                {
                    _consoleWriterActor.Tell(new Messages.InputSuccess($"Starting processing for {msg}"));
                    this._tailCoordinatorActor.Tell(new TailCoordinatorActor.StartTail(msg, this._consoleWriterActor));
                }
                else
                {
                    _consoleWriterActor.Tell(new Messages.ValidationError($"{msg} is not an existing URI on disk."));

                    Sender.Tell(new Messages.ContinueProcessing());
                }
            }

        }
        private static bool IsFileUri(string path)
        {
            return File.Exists(path);
        }
    }
}