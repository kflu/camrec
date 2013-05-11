using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace KFL.CamRecorder
{
    class Program
    {
        static void Main(string[] args)
        {
            ServiceBase.Run(new CamRecorderService());
        }
    }

    class CamRecorderService : ServiceBase
    {
        private const string EventSource = "KflCamRec";

        private readonly string exePath = @"C:\Cygwin\bin\gst-launch-1.0.exe";

        private Process proc;

        private readonly object gcl;

        private Thread procMon;

        private int serviceStopRequested = 0;

        private readonly string url;
        private readonly string user;
        private readonly string pass;

        public CamRecorderService()
        {
            gcl = new object();

            // Create event log source
            if (!EventLog.SourceExists(CamRecorderService.EventSource))
            {
                EventLog.CreateEventSource(CamRecorderService.EventSource, string.Empty);
            }

            using (FileStream fs = new FileStream(@"C:\camrec\config.txt", FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    this.url = reader.ReadLine();
                    this.user = reader.ReadLine();
                    this.pass = reader.ReadLine();

                    if (this.url == null || this.user == null || this.pass == null)
                    {
                        EventLog.WriteEntry(CamRecorderService.EventSource, "url or user or pass is null", EventLogEntryType.Error);
                        throw new ArgumentException("can't read config file");
                    }
                }
            }
        }

        protected override void OnStart(string[] args)
        {
            this.procMon = new Thread(() =>
            {
                EventLog.WriteEntry(CamRecorderService.EventSource, "entering main loop");
                DateTime start = DateTime.UtcNow;
                while (true)
                {
                    if (this.serviceStopRequested != 0)
                    {
                        Interlocked.Decrement(ref this.serviceStopRequested);
                        EventLog.WriteEntry(CamRecorderService.EventSource, "stop requested");
                        this.proc.Kill();
                        break;
                    }

                    DateTime now = DateTime.UtcNow;
                    if (now - start > TimeSpan.FromMinutes(15))
                    {
                        EventLog.WriteEntry(CamRecorderService.EventSource, "Timer expires, killing");
                        if (!this.proc.HasExited)
                        {
                            this.proc.Kill();
                        }
                    }

                    if (this.proc == null || this.proc.HasExited)
                    {
                        EventLog.WriteEntry(CamRecorderService.EventSource, "proc terminated. start new.");
                        this.proc = new Process();
                        this.proc.StartInfo.FileName = this.exePath;
                        start = DateTime.UtcNow;
                        this.proc.StartInfo.Arguments = string.Format(
                            " -e souphttpsrc location={0} user-id={1} user-pw={2} do-timestamp=true is_live=true timeout=5 !  multipartdemux !  jpegparse !  jpegdec !  clockoverlay time-format=\"%c\" !  theoraenc !  oggmux !  filesink location=\"/cygdrive/c/camrec/{3}.ogg\"",
                            this.url,
                            this.user,
                            this.pass,
                            string.Format("camrec_{0}.ogg", start.ToString("yyyyMMdd_HHmmss")));
                        this.proc.Start();
                    }

                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            });

            EventLog.WriteEntry(CamRecorderService.EventSource, "starting proc mon thread");
            this.procMon.Start();
        }

        protected override void OnStop()
        {
            EventLog.WriteEntry(CamRecorderService.EventSource, "service stop requested, requesting stop");

            Interlocked.Increment(ref this.serviceStopRequested);

            EventLog.WriteEntry(CamRecorderService.EventSource, "Waiting for procMon to join");
            if (!this.procMon.Join(TimeSpan.FromMinutes(2)))
            {
                EventLog.WriteEntry(CamRecorderService.EventSource, "Timeout. Killing process, Killing procMon");
                this.procMon.Abort();
                this.proc.Kill(); // FIXME: not thread safe
            }

            EventLog.WriteEntry(CamRecorderService.EventSource, "procMon returned/killed. Now exiting.");
        }
    }
}
