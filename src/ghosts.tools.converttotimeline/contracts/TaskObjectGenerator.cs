using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;

namespace ghosts.tools.converttotimeline.contracts
{
    public static class TaskObjectGenerator
    {
        public static string GenerateDynamicTaskObject(string tasktype)
        {
           
            string ttype = "";
            
            ttype = GetTaskFromTaskType(tasktype);
            if (ttype == "404")
                return null;

            Random rnd = new Random();
            DateTime dt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0);
            //put these vars in class when tidying up
            string tlcommand = "";
            List<string> tlCommandArgs = new List<string>();
            string tlinitial = "";

            switch (ttype)
            {
                case "BrowserIE":
                    tlcommand = "random";
                    tlCommandArgs.Add("https://www.google.com.au/"); // in future these would populate from randomized list
                    tlCommandArgs.Add("https://github.com/");
                    tlinitial = "about:blank";
                    break;
                case "Command":
                    tlcommand = "cd %homedrive%%homepath%\\Downloads";
                    tlCommandArgs.Add("powershell expand - archive - Path italian_breakfast.zip - destinationpath x");
                    tlCommandArgs.Add("cd x");
                    tlCommandArgs.Add("dir");
                    break;               
                default:
                    tlcommand = "create";
                    tlCommandArgs.Add("%homedrive%%homepath%\\Documents");
                    break;
            }

            TimeLineEvent te = new TimeLineEvent();
            te.Command = tlcommand;
            te.DelayAfter = rnd.Next(90000, 2000000);
            te.CommandArgs = tlCommandArgs;
            te.DelayBefore = 0;


            TaskSchema ts = new TaskSchema();
            ts.HandlerType = ttype;
            ts.Initial = tlinitial;
            ts.UtcTimeOn = dt.ToString("HH:mm:ss");
            ts.UtcTimeOff = dt.AddHours(rnd.Next(1, 24)).ToString("HH:mm:ss");
            ts.Loop = (rnd.Next(2) == 0); 
            ts.TimeLineEvents = new TimeLineEvent[] { te };
            

            string output = JsonConvert.SerializeObject(ts,Formatting.Indented);
            return output;

            /*
            dynamic parameters = new dynamic[2];

            parameters[0] = new ExpandoObject();
            parameters[0].HandlerType = ttype;
            parameters[0].Initial = "";
            parameters[0].UtcTimeOn = dt.ToString("HH:mm:ss");
            parameters[0].UtcTimeOff = dt.AddHours(rnd.Next(1, 24)).ToString("HH:mm:ss");
            parameters[0].Loop = (rnd.Next(2) == 0);

            parameters[0].TimeLineEvents = new dynamic[1];
            parameters[0].TimeLineEvents[0] = new ExpandoObject();

            parameters[0].TimeLineEvents[0].Command = "create";
            parameters[0].TimeLineEvents[0].CommandArgs = new dynamic[1];
            parameters[0].TimeLineEvents[0].CommandArgs = "% homedrive %% homepath %\\Documents";
            parameters[0].TimeLineEvents[0].DelayAfter = rnd.Next(90000, 2000000);
            parameters[0].TimeLineEvents[0].DelayBefore = "0";

            string json = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            return json;*/

            //dynamic timeline = new JObject();
            //dynamic child = new JObject();

            //timeline.HandlerType = ttype;
            //timeline.Initial = "";
            //timeline.UtcTimeOn = dt.ToString("HH:mm:ss");
            //timeline.UtcTimeOff = dt.AddHours(rnd.Next(1, 24)).ToString("HH:mm:ss");
            //timeline.Loop = (rnd.Next(2) == 0);
            //timeline.TimeLineEvents = new JArray(
            //child.Command = "create",
            //child.CommandArgs = new JArray(
            //    "% homedrive %% homepath %\\Documents"
            // ),
            //child.DelayAfter = rnd.Next(90000, 2000000),
            //child.DelayBefore = "0"
            //);

            //return (JObject)JToken.FromObject(timeline);

        }
        private static string GetTaskFromTaskType(string ttype)
        {
            //word, excel, outlook, chat, powershell
            Dictionary<string, string> tasklist = new Dictionary<string, string>()
            {
                { "Microsoft_Word", "Word"},
                { "Outlook_mail", "Outlook"},
                { "Microsoft_Excel", "Excel"},
                { "PowerShell_scripts", "Command"},
                { "IE_Browser", "BrowserIE"}
            };
            try
            {
                return tasklist[ttype];
            }
            catch(Exception ex)
            {
                return "404";
            }
        }
    }
}
