using ghosts.tools.converttotimeline.contracts;
using ghosts.tools.converttotimeline.Functional;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;

using System.Text;
using System.Threading;

namespace ghosts.tools.converttotimeline
{
    class Program
    {
        static void Main(string[] args)
        {
            int indexsolutiondir = AppDomain.CurrentDomain.BaseDirectory.IndexOf("\\ghosts.tools.converttotimeline");
            var solndirpath = AppDomain.CurrentDomain.BaseDirectory.Substring(0, indexsolutiondir);
            //C:\Users\Palash\source\repos\Ghostsrefactored\src
            int indexofbindir = AppDomain.CurrentDomain.BaseDirectory.IndexOf("\\bin");
            if (indexofbindir < 0)
                return;
            var basepath = AppDomain.CurrentDomain.BaseDirectory.Substring(0, indexofbindir);
            var jsonfilepath = basepath + "\\assets\\";
            var timelinefilepath = basepath + "\\config\\timeline.json";
            //C:\Users\Palash\source\repos\Ghostsrefactored\src\ghosts.tools.converttotimeline\bin\Debug\netcoreapp3.1\
            //string path = @"C:\Users\Palash\source\repos\Ghostsrefactored\src\ghosts.tools.converttotimeline\assets\";
            string[] fileEntries = Directory.GetFiles(jsonfilepath);

            string sb = "";
            foreach (string fileName in fileEntries)
            {
                List<Task> tasks = JsonFilesReader.GetJsonObjects(fileName);
                foreach (Task task in tasks)
                {
                    sb += TaskObjectGenerator.GenerateDynamicTaskObject(task.task)
                        .ToString() + "," + Environment.NewLine;
                }
            }
            if (!File.Exists(timelinefilepath))
            {
                using (var file = File.Create(timelinefilepath))
                {
                    file.Close();
                }
            }

            try
            {
                FileWriter.ConsolidateToTimeline(sb, solndirpath);               
            }
            catch(Exception ex)
            {

            }
            Console.WriteLine(sb);
            Console.ReadLine();

        }

    }
}
