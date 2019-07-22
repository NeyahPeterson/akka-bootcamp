using System;
using Akka.Actor;

namespace WinTail
{
    public class TailCoordinatorActor : UntypedActor
    {
        public class StartTail
        {
            public StartTail(string filePath, IActorRef reporterActor)
            {
                this.FilePath = filePath;
                this.ReporterActor = reporterActor;
            }

            public string FilePath { get; }
            public IActorRef ReporterActor { get; }
        }

        public class StopTail
        {
            public StopTail(string filePath)
            {
                this.FilePath = filePath;
            }

            public string FilePath { get; }
        }

        protected override void OnReceive(object message)
        {
            if (message is StartTail startMessage)
            {
                Context.ActorOf(Props.Create(() => new TailActor(startMessage.ReporterActor, startMessage.FilePath)));
            }
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(
                10, // max retries
                TimeSpan.FromSeconds(30), // within timerange
                x => // local only decider 
                {
                    if (x is ArithmeticException)
                    {
                        return Directive.Resume;
                    }
                    else if (x is NotSupportedException)
                    {
                        return Directive.Stop;
                    }
                    else return Directive.Restart;
                });
        }
    }
}
