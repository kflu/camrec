using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

namespace ISpyService
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new IspyService());
        }
    }

    class IspyService : ServiceBase
    {
        private const string EventSource = "iSpyService";

        private readonly string exePath = @"C:\Program Files (x86)\iSpy\iSpy\iSpy.exe";

        private readonly Process proc;

        private readonly object gcl;

        private readonly Thread procMon;

        public IspyService()
        {
            gcl = new object();

            this.procMon = new Thread(() =>
            {
                this.proc.WaitForExit();
                EventLog.WriteEntry(IspyService.EventSource, "ispy.exe terminated");
            });

            proc = new Process();
            proc.StartInfo.FileName = this.exePath;

            // Create event log source
            if (!EventLog.SourceExists(IspyService.EventSource))
            {
                EventLog.CreateEventSource(IspyService.EventSource, string.Empty);
            }
        }

        protected override void OnStart(string[] args)
        {
            this.proc.Start();
            this.procMon.Start();
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry(IspyService.EventSource, "service stop requested, closing ispy.exe");
            if (!this.proc.CloseMainWindow())
            {
                EventLog.WriteEntry(IspyService.EventSource, "CloseMainWindow signal not sent");
                this.SendShutdownCommand();
            }

            EventLog.WriteEntry(IspyService.EventSource, "Waiting for procMon to join");
            if (!this.procMon.Join(TimeSpan.FromMinutes(2)))
            {
                EventLog.WriteEntry(IspyService.EventSource, "Timeout. Killing procMon");
                this.procMon.Abort();
            }

            EventLog.WriteEntry(IspyService.EventSource, "procMon returned. Now exiting.");
        }

        private void SendShutdownCommand()
        {
            Process cmdproc = new Process();
            cmdproc.StartInfo.FileName = this.exePath;
            cmdproc.StartInfo.Arguments = "commands \"shutdown\"";
            cmdproc.Start();
            EventLog.WriteEntry(IspyService.EventSource, "wait for command to finish");
            if (!cmdproc.WaitForExit(60 * 1000)) // wait for 60sec
            {
                EventLog.WriteEntry(IspyService.EventSource, "command not terminating. Killing..");
                cmdproc.Kill();
            }

            EventLog.WriteEntry(IspyService.EventSource, "Killing ispy.exe");
            this.proc.Kill();
        }
    }
}