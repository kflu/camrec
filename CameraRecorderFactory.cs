using System.Xml.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace camrec
{
    class CameraRecorderFactory
    {
        public const string CameraRecordersXName = "CameraRecorders";

        public static Lazy<CameraRecorderFactory> Instance = new Lazy<CameraRecorderFactory>();

        public ICameraRecorder CreateCameraRecorder(XElement recorderElement)
        {
            ICameraRecorder camRecorder = null;
            switch (recorderElement.Name.LocalName)
            {
                case CygGstCameraRecorder.XName:
                    camRecorder = new CygGstCameraRecorder(recorderElement);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("No such recorder type: " + recorderElement.Name.LocalName);
            }

            return camRecorder;
        }

        public ICameraRecorder[] CreateCameraRecorders(XElement recordersElement)
        {
            List<ICameraRecorder> recs = new List<ICameraRecorder>(2);
            foreach (XElement element in recordersElement.Elements())
            {
                ICameraRecorder rec = this.CreateCameraRecorder(element);
                if (rec != null)
                {
                    recs.Add(rec);
                }
            }

            return recs.ToArray();
        }
    }
}
