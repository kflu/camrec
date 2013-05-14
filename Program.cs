using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Xml.Linq;

namespace camrec
{
    class Program
    {
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                Console.WriteLine("press anykey to continue.");
                Console.ReadLine();
                (new CamRecorderService()).StartFromConsole();
                Console.WriteLine("Press ctrl-c to stop");
                while (true)
                {
                }
            }
            else
            {
                ServiceBase.Run(new CamRecorderService());
            }
        }
    }

    class CamRecorderService : ServiceBase
    {
        private const string configFilePath = @"C:\camrec\config.xml";
        private readonly XContainer config;
        private readonly CameraRecorder[] cameraRecorders;
        private const string ConfigRootXName = "config";

        public CamRecorderService()
        {
            if (!File.Exists(CamRecorderService.configFilePath))
            {
                EventLog.WriteEntry(CamRecorderService.configFilePath + " doesn't exist", EventLogEntryType.Error);
                throw new ArgumentException("config file doesn't exist");
            }

            this.config = XDocument.Load(CamRecorderService.configFilePath).Element(CamRecorderService.ConfigRootXName);
            Util.ThrowIfNull(this.config, "Config object");

            CameraRecorder[] cameraRecorders = (from recorder in this.config.Elements(CameraRecorder.XName)
                                                select CameraRecorder.FromXml(recorder)).ToArray();

            this.cameraRecorders = cameraRecorders;
        }

        public void StartFromConsole()
        {
            this.OnStart(Environment.GetCommandLineArgs());
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                foreach (CameraRecorder recorder in this.cameraRecorders)
                {
                    Util.Log("Starting recorder for camera: " + recorder.cameraInfo.name);
                    recorder.Start();
                }
            }
            catch (Exception e)
            {
                Util.Log("Exception encountered. Stopping: " + e.ToString());
                base.Stop();
            }
        }

        protected override void OnStop()
        {
            Util.Log("service stop requested, requesting stop");

            foreach (CameraRecorder recorder in this.cameraRecorders)
            {
                Util.Log("Requesting to stop camera: " + recorder.cameraInfo.name);
                recorder.RequestStop();
            }

            DateTime start = DateTime.UtcNow;
            bool allStopped = false;
            while (DateTime.UtcNow - start < TimeSpan.FromSeconds(10))
            {
                if (allStopped = !(this.cameraRecorders.Count(recorder => !recorder.IsStopped()) > 0))
                {
                    break;
                }
            }

            if (!allStopped)
            {
                // This is impossible
                Util.Log(
                    "There are cameras that can't be stopped. Count: " + this.cameraRecorders.Count(recorder => !recorder.IsStopped()),
                    EventLogEntryType.Error);
            }

            foreach (CameraRecorder recorder in this.cameraRecorders)
            {
                if (!recorder.pipelineThread.Join(TimeSpan.FromSeconds(2)))
                {
                    EventLog.WriteEntry("Timeout. Aborting thread for camera: " + recorder.cameraInfo.name, EventLogEntryType.Warning);
                    recorder.pipelineThread.Abort();
                }
            }

            Util.Log("Service is now exiting");
        }
    }
}
