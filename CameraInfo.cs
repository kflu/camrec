using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;

namespace camrec
{
    class CameraInfo
    {
        public const string XName = "Camera";
        private const string XNameId = "id";
        private const string XNameUrl = "url";
        private const string XNameUser = "username";
        private const string XNamePass = "password";

        public readonly string name = "camera";
        public readonly string url;
        public readonly string username;
        public readonly string password;
        
        private CameraInfo(string name, string url, string username, string password)
        {
            Debug.Assert(!string.IsNullOrEmpty(url));

            this.url = url;

            if (!string.IsNullOrEmpty(name))
            {
                this.name = name;
            }

            this.username = username;
            this.password = password;
        }

        public static CameraInfo FromXml(XElement cameraElement)
        {
            Debug.Assert(cameraElement.Name == CameraInfo.XName);
            string name = Util.IfNullThen(cameraElement.Attribute(CameraInfo.XNameId), null);
            string url = Util.IfNullThen(cameraElement.Attribute(CameraInfo.XNameUrl), null);
            string user = Util.IfNullThen(cameraElement.Attribute(CameraInfo.XNameUser), null);
            string pass = Util.IfNullThen(cameraElement.Attribute(CameraInfo.XNamePass), null);
            return new CameraInfo(name, url, user, pass);
        }
    }
}
