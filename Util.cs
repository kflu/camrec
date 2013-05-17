using System;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace camrec
{
    public static class Util
    {
        public const string EventSource = "camrec";
        public static void Log(string msg, EventLogEntryType entryType = EventLogEntryType.Information)
        {
            if (!EventLog.SourceExists(Util.EventSource))
            {
                EventLog.CreateEventSource(Util.EventSource, string.Empty);
            }

            EventLog.WriteEntry(Util.EventSource, msg, entryType);
        }

        public static void ThrowIfNullOrEmpty(string s, string argName = "")
        {
            if (string.IsNullOrEmpty(s))
            {
                throw new ArgumentException("String is null: " + argName);
            }
        }

        public static void ThrowIfNull(object arg, string argName = "")
        {
            if (arg == null)
            {
                throw new ArgumentException("Argument is null: " + argName);
            }
        }

        public static string IfNullThen(XAttribute obj, string value)
        {
            if (obj == null)
            {
                return value;
            }
            return obj.Value;
        }

        public static string IfNullThen(XElement obj, string value)
        {
            if (obj == null)
            {
                return value;
            }
            return obj.Value;
        }

        public static string GetExeDirectory()
        {
            Process proc = Process.GetCurrentProcess();
            return Path.GetDirectoryName(proc.MainModule.FileName);           
        }
    }
}