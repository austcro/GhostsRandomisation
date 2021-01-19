using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace ghosts.tools.converttotimeline.Functional
{

    public static class FileWriter
    {
        private static Object _locked = new Object();
        private static Object _safetyLocked = new Object();
        private static string configjsonfilepath;
        private static void WriteIntoTimeline(string text)
        {
            if (!Monitor.IsEntered(_safetyLocked))
            {
                lock (_locked)
                {
                    using (StreamWriter file = new StreamWriter(configjsonfilepath + @"\ghosts.tools.converttotimeline\config\timeline.json", true))
                    {
                        file.WriteLine(text);
                    }
                }
            }
            else
            {
                Thread.Sleep(5000);
            }
        }

        public static void ConsolidateToTimeline(string json, string solndirpath)
        {
            try
            {
                configjsonfilepath = solndirpath;
                WriteIntoTimeline("{");
                WriteIntoTimeline("\"TimeLineHandlers\": [");
                WriteIntoTimeline(json);
                WriteIntoTimeline("]");
                WriteIntoTimeline("}");

                MoveTimelineToConfig(solndirpath);
            }
            catch (Exception ex)
            {

            }
        }
        private static void MoveTimelineToConfig(string solndirpath)
        {
            string emutimelinefilepath = solndirpath + @"\Ghosts.Client\config\timeline.json";
            File.Delete(emutimelinefilepath);
            File.Move(solndirpath + @"\ghosts.tools.converttotimeline\config\timeline.json", solndirpath + @"\Ghosts.Client\config\timeline.json");
        }
    }
}

