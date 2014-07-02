using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Configuration;

namespace MPEIService
{
    public class Log
    {
        object lockobject = new object();
        static private Log _instance = null;

        private List<string> _lines;

        public List<string> Lines
        {
            get { return _lines; }
        }
        private string _logfile;


        private Log()
        {
            _logfile = Config.GetFile(Config.Dir.Log, "MPEIService.log");
            _lines = new List<string>();
        }

        static public Log Instance()
        {
            if (_instance == null)
            {
                _instance = new Log();
            }

            return _instance;
        }

        public void Print(string line)
        {
            lock (lockobject)
            {
                TextWriter writer = new StreamWriter(_logfile, true);
                writer.WriteLine(string.Format("[{0} {1}] [MPEIService] {2}", DateTime.Now.ToShortTimeString(), DateTime.Now.ToShortDateString(), line));
                writer.Close();

                _lines.Insert(0, line);

                while (_lines.Count > 100)
                {
                    _lines.RemoveAt(_lines.Count - 1);
                }
            }
        }

        internal void Clear()
        {
            _lines.Clear();
        }

        internal void Print(Exception e)
        {
            Print(string.Format("\nMessage ---\n{0}", e.Message));
            Print(string.Format("\nHelpLink ---\n{0}", e.HelpLink));
            Print(string.Format("\nSource ---\n{0}", e.Source));
            Print(string.Format("\nStackTrace ---\n{0}", e.StackTrace));
            Print(string.Format("\nTargetSite ---\n{0}", e.TargetSite));
        }
    }
}