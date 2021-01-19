using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ghosts.tools.converttotimeline.contracts
{
    public class TaskSchema
    {
        public string HandlerType { get; set; }        
        public string Initial { get; set; }
        public string UtcTimeOn { get; set; }
        public string UtcTimeOff { get; set; }
        public bool Loop { get; set; }
        public TimeLineEvent[] TimeLineEvents { get; set; }

    }
    public class TimeLineEvent
    {
        public string Command { get; set; }
        //public string[] CommandArgs { get; set; }
        public List<string> CommandArgs { get; set; }
        public int DelayAfter { get; set; }
        public int DelayBefore { get; set; }

    }
}
