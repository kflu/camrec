using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace camrec
{
    abstract class CameraRecorder : ICameraRecorder
    {
        // TODO: in the future we would support multiple schedules. 
        // The recorder is considered enabled if anyone of them is enabled
        public readonly Schedule schedule;
        public readonly CameraInfo cameraInfo;

        /* TODO: need to add support for them to be configurable. They needs to be configed in the subclass.
        public readonly int height;
        public readonly int width;
        public readonly int[] framerate;
         */

        protected CameraRecorder(XElement recorderElement)
        {
            Util.ThrowIfNull(recorderElement, "recorderElement");
            this.schedule = Schedule.FromXml(recorderElement.Element(Schedule.XName));
            this.cameraInfo = CameraInfo.FromXml(recorderElement.Element(CameraInfo.XName));
            Util.ThrowIfNull(this.schedule, "schedule");
            Util.ThrowIfNull(this.cameraInfo, "cameraInfo");
        }

        public virtual bool IsStopped()
        {
            throw new NotImplementedException();
        }

        public virtual void RequestStop()
        {
            throw new NotImplementedException();
        }

        public virtual void Start()
        {
            throw new NotImplementedException();
        }

        public virtual bool ConfirmStop()
        {
            throw new NotImplementedException();
        }
    }
}
