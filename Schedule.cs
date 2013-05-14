using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Diagnostics;

namespace camrec
{
    /// <summary>
    /// Schedule for a camera recording. Currently it only checks time range of a day
    /// 
    /// TODO: an interface needs to be extracted if necessary
    /// </summary>
    class Schedule
    {
        public const string XName = "Schedule";
        private readonly TimeSpan start;
        private readonly TimeSpan end;

        public static Schedule FromXml(XElement scheduleElement)
        {
            Schedule s;
            DateTime start,end;
            string startStr = Util.IfNullThen(scheduleElement.Attribute("startlocal"), null);
            string endStr = Util.IfNullThen(scheduleElement.Attribute("endlocal"), null);
            if (startStr == null && endStr == null)
            {
                start = new DateTime(1, 1, 1, 0, 0, 0);
                end = new DateTime(1, 1, 1, 23, 59, 59);
                s = new Schedule(start.TimeOfDay, end.TimeOfDay);
            }
            else if (DateTime.TryParse(startStr, out start) && 
                DateTime.TryParse(endStr, out end) &&
                // You can't specify a TZ as we only compare local time
                start.Kind == DateTimeKind.Unspecified &&
                end.Kind == DateTimeKind.Unspecified)
            {
                s = new Schedule(start.TimeOfDay, end.TimeOfDay);
            }
            else
            {
                Util.Log("Cannot parse Schedule from XML");
                s = null;
            }

            return s;
        }

        private Schedule(TimeSpan start, TimeSpan end)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Called to know if the recording should be enabled
        /// </summary>
        /// <remarks>
        /// There are two ways to specify start/end. 
        /// If start <= end. Then it's enabled during the same day.
        /// 
        /// -----Start+++++++++++End-------
        /// 
        /// If start >= end. Then it's cross day.
        /// 
        /// +++++End---------------Start+++++
        /// 
        /// where - means disabled, + means enabled
        /// </remarks>
        public bool IsEnabled()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;

            if (start <= end)
            {
                return start <= now && now <= end;
            }
            else
            {
                return now <= end || now >= start;
            }
        }
    }
}
