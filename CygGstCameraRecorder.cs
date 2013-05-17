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
    class CygGstCameraRecorder : CameraRecorder
    {
        public const string XName = "CygGstCameraRecorder";
        private const string GstPathXName = "gst-path";
        private const string LogFolderXName = "log-folder";
        private const string pipelineArgTemplate = " -e souphttpsrc location={0} {1} {2} do-timestamp=true is_live=true timeout=5 !  multipartdemux ! \"image/jpeg,framerate=(fraction)5/1\" ! jpegparse !  jpegdec !  clockoverlay time-format=\"%c\" !  theoraenc !  oggmux !  filesink location=\"{3}\"";

        /// <summary>
        /// Recording storage directory
        /// </summary>
        private readonly string storageDirectory;

        private readonly string ExePath;

        public Thread pipelineThread;
        private Process pipelineProc;
        private int stopRequested = 0;

        public CygGstCameraRecorder(XElement recorderElement) : base(recorderElement)
        {
            Util.ThrowIfNull(recorderElement, "recorderElement");
            this.ExePath = recorderElement.Attribute(CygGstCameraRecorder.GstPathXName).Value;
            this.storageDirectory = recorderElement.Attribute(CygGstCameraRecorder.LogFolderXName).Value;
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

        public override void Start()
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
                            // TODO: this can be simply fixed by introducing a state bool prevDisabled = true; before the while
                            // loop. file cutting only runs if prevDisabled == true. prevDisabled will be set to false in enabled
                            // branch; and set to true in disabled branch.
                            start = DateTime.UtcNow;
                        }

                        Thread.Sleep(1);
                    }
                });

            Util.Log("Starting pipeline thread.");
            this.pipelineThread.Start();
        }

        public override void RequestStop()
        {
            Util.Log("Send request to stop the pipeline");
            Interlocked.CompareExchange(ref this.stopRequested, 1, 0);
        }

        public override bool IsStopped()
        {
            // return this.pipelineProc == null || this.pipelineProc.HasExited;
            return !this.pipelineThread.IsAlive;
        }

        private Process BuildPipelineProc()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = this.ExePath;
            proc.StartInfo.Arguments = string.Format(
                CygGstCameraRecorder.pipelineArgTemplate,
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
