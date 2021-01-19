using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ghosts.tools.converttotimeline.contracts
{
    public static class JsonFilesReader
    {

        public static List<Task> GetJsonObjects(string pathToIcoFile)
        {
            List<Task> items = new List<Task>();
            using (StreamReader r = new StreamReader(pathToIcoFile))
            {
                string json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<Task>>(json);
            }
            return items;
        }
    }
}
