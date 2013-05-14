using System.Xml.Linq;
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace camrec
{
    class CameraRecorder
    {
        public const string XName = "CameraRecorder";
        private const string ExePath = @"C:\Cygwin\bin\gst-launch-1.0.exe"; // FIXME: needs to be in global config
        private const string pipelineArgTemplate = " -e souphttpsrc location={0} {1} {2} do-timestamp=true is_live=true timeout=5 !  multipartdemux ! \"image/jpeg,framerate=(fraction)5/1\" ! jpegparse !  jpegdec !  clockoverlay time-format=\"%c\" !  theoraenc !  oggmux !  filesink location=\"{3}\"";
        public readonly CameraInfo cameraInfo;

        // TODO: in the future we would support multiple schedules. 
        // The recorder is considered enabled if anyone of them is enabled
        public readonly Schedule schedule; 
        private Process pipelineProc;
        public Thread pipelineThread;

        private int stopRequested = 0;
        
        /// <summary>
        /// Recording storage directory
        /// </summary>
        private readonly string storageDirectory = @"/cygdrive/c/camrec";

        public static CameraRecorder FromXml(XElement recorderElement)
        {
            XAttribute locationAttr = recorderElement.Attribute("storage_directory");
            string location = locationAttr == null ? null : locationAttr.Value;
            CameraInfo cameraInfo = CameraInfo.FromXml(recorderElement.Element(CameraInfo.XName));
            Schedule schedule = Schedule.FromXml(recorderElement.Element(Schedule.XName));
            return new CameraRecorder(location, cameraInfo, schedule);
        }

        private CameraRecorder(string storageDirectory, CameraInfo cameraInfo, Schedule schedule)
        {
            Util.ThrowIfNull(cameraInfo, "cameraInfo");
            if (!string.IsNullOrEmpty(storageDirectory))
            {
                this.storageDirectory = storageDirectory;
            }

            this.cameraInfo = cameraInfo;
            this.schedule = schedule;
        }

        private void StopPipelineProcess()
        {
            if (this.pipelineProc != null && !this.pipelineProc.HasExited)
            {
                this.pipelineProc.Kill();
                this.pipelineProc.WaitForExit();
                this.pipelineProc.Dispose();
                this.pipelineProc = null;
            }
        }

        public void Start()
        {
            this.pipelineThread = new Thread(() =>
                {
                    DateTime start = DateTime.UtcNow;
                    while (true)
                    {
                        if (this.stopRequested == 1)
                        {
                            Util.Log("Stop requested. Now stopping pipeline process");
                            this.StopPipelineProcess();
                            Interlocked.Exchange(ref this.stopRequested, 0);
                            break;
                        }

                        if (this.schedule.IsEnabled())
                        {
                            DateTime now = DateTime.UtcNow;
                            if (now - start > TimeSpan.FromMinutes(15)) // TODO: needs to be configurable
                            {
                                Util.Log("Timer expired. Terminating current pipeline.");
                                this.StopPipelineProcess();
                            }

                            if (this.pipelineProc == null || this.pipelineProc.HasExited)
                            {
                                Util.Log("Starting new pipeline process.");
                                this.pipelineProc = this.BuildPipelineProc();
                                start = DateTime.UtcNow;
                                this.pipelineProc.Start();
                            }
                        }
                        else // pipeline is disabled by schedule
                        {
                            this.StopPipelineProcess();

                            // This is to prevent video file cutting to be run when pipeline has been disabled 
                            // for a long time and just gets enabled. (FIXME) But this approach won't work when the main 
                            // loop sleep time is longer then the max video file duration.
                            start = DateTime.UtcNow;
                            continue;
                        }

                        Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                });

            Util.Log("Starting pipeline thread.");
            this.pipelineThread.Start();
        }

        public void RequestStop()
        {
            Util.Log("Send request to stop the pipeline");
            Interlocked.CompareExchange(ref this.stopRequested, 1, 0);
        }

        public bool IsStopped()
        {
            return this.pipelineProc == null || this.pipelineProc.HasExited;
        }

        private Process BuildPipelineProc()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = CameraRecorder.ExePath;
            proc.StartInfo.Arguments = string.Format(
                CameraRecorder.pipelineArgTemplate,
                this.cameraInfo.url, // 0
                this.cameraInfo.username != null ? "user-id=" + this.cameraInfo.username : "", // 1
                this.cameraInfo.password != null ? "user-pw=" + this.cameraInfo.password : "", // 2
                string.Format(
                    "{0}/{1}.ogg", // /cygdrive/c/camrec/{3}.ogg FIXME: now accepts cygwin path. This is easily broken.
                    this.storageDirectory,
                    string.Format("camrec_{0}", DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"))));

            Util.Log(string.Format("Building pipeline process: filename: {0}, args: {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments));
            return proc;
        }
    }
}
