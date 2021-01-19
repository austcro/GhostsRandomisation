﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using Ghosts.Client.Handlers;
using Ghosts.Client.Infrastructure;
using Ghosts.Client.TimelineManager;
using Ghosts.Domain.Code;
using Unity;
using NLog;

using Ghosts.Client.InterfaceImpl;
using Microsoft.Practices.Unity.Configuration;
using System.Configuration;

namespace Ghosts.Client
{
    class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SwHide = 0;
        private const int SwShow = 5;

        internal static ClientConfiguration Configuration { get; set; }
        internal static Options OptionFlags;
        internal static bool IsDebug;

        // minimize memory use
        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        static void MinimizeFootprint()
        {
            EmptyWorkingSet(Process.GetCurrentProcess().Handle);
        }

        private static void minimizeMemory()
        {
            GC.Collect(GC.MaxGeneration);
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle,
                (UIntPtr)0xFFFFFFFF, (UIntPtr)0xFFFFFFFF);
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process,
            UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);
        // end minimize memory use

        internal class Options
        {
            [Option('d', "debug", Default = false, HelpText = "Launch GHOSTS in debug mode")]
            public bool Debug { get; set; }

            [Option('h', "help", Default = false, HelpText = "Display this help screen")]
            public bool Help { get; set; }

            [Option('r', "randomize", Default = false, HelpText = "Create a randomized timeline")]
            public bool Randomize { get; set; }

            [Option('v', "version", Default = false, HelpText = "GHOSTS client version")]
            public bool Version { get; set; }

            [Option('i', "information", Default = false, HelpText = "GHOSTS client id information")]
            public bool Information { get; set; }
        }

        [STAThread]
        static void Main(string[] args)
        {
            MinimizeFootprint();
            minimizeMemory();
            //setup our DI
            //var serviceProvider = new ServiceCollection()
            //    .AddLogging()
            //    .AddSingleton<IWord, Word>()                
            //    .BuildServiceProvider();

            IUnityContainer container = RegisterInUnityContainer();

            try
            {
                Console.WriteLine("Miming timeline json file creation here");

                File.Delete(@"C:\Users\Palash\source\repos\Ghostsrefactored\src\Ghosts.Client\config\timeline.json");
                File.Delete(@"C:\Users\Palash\source\repos\Ghostsrefactored\src\Ghosts.Client\bin\Debug\config\timeline.json");
                // Start the child process.
                Process p = new Process();
                // Redirect the output stream of the child process.
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = @"C:\Users\Palash\source\repos\Ghostsrefactored\src\ghosts.tools.converttotimeline\bin\Debug\netcoreapp3.1\ghosts.tools.converttotimeline.exe";
                p.Start();
                // Do not wait for the child process to exit before
                // reading to the end of its redirected stream.
                // p.WaitForExit();
                // Read the output stream first and then wait.
                //string output = p.StandardOutput.ReadToEnd();
                //p.WaitForExit();
                //Console.WriteLine("End of Miming");
                Thread.Sleep(3000);               
                File.Copy(@"C:\Users\Palash\source\repos\Ghostsrefactored\src\Ghosts.Client\config\timeline.json", @"C:\Users\Palash\source\repos\Ghostsrefactored\src\Ghosts.Client\bin\Debug\config\timeline.json");
                Run(args, container);
            }
            catch (Exception e)
            {
                var s = $"Fatal exception in GHOSTS {ApplicationDetails.Version}: {e}";
                _log.Fatal(s);

                var handle = GetConsoleWindow();
                ShowWindow(handle, SwShow);

                Console.WriteLine(s);
                Console.ReadLine();
            }
        }

        private static IUnityContainer RegisterInUnityContainer()
        {
            IUnityContainer container = new UnityContainer();            
            container.LoadConfiguration("ContainerConfiguration");
            return container;
        }
        private static void Run(string[] args, IUnityContainer container)
        {
            // ignore all certs
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // parse program flags
            if (!CommandLineFlagManager.Parse(args))
                return;
            
            //attach handler for shutdown tasks
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _log.Trace($"Initiating Ghosts startup - Local time: {DateTime.Now.TimeOfDay} UTC: {DateTime.UtcNow.TimeOfDay}");

            //load configuration
            try
            {
                Configuration = ClientConfigurationLoader.Config;
            }
            catch (Exception e)
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var o = $"Exec path: {path} - configuration 404: {ApplicationDetails.ConfigurationFiles.Application} - exiting. Exception: {e}";
                _log.Fatal(o);
                Console.WriteLine(o);
                Console.ReadLine();
                return;
            }

            StartupTasks.CheckConfigs();

            Thread.Sleep(500);
            
            //show window if debugging or if --debug flag passed in
            var handle = GetConsoleWindow();
            if (!IsDebug)
            {
                ShowWindow(handle, SwHide);
                //add hook to manage processes running in order to never tip a machine over
                StartupTasks.CleanupProcesses();
            }

            //add ghosts to startup
            StartupTasks.SetStartup();

            //add listener on a port or ephemeral file watch to handle ad hoc commands
            ListenerManager.Run();

            //do we have client id? or is this first run?
            _log.Trace(Comms.CheckId.Id);

            //connect to command server for 1) client id 2) get updates and 3) sending logs/surveys
            Comms.Updates.Run();

            //local survey gathers information such as drives, accounts, logs, etc.
            if (Configuration.Survey.IsEnabled)
            {
                try
                {
                    Survey.SurveyManager.Run();
                }
                catch (Exception exc)
                {
                    _log.Error(exc);
                }
            }

            if (Configuration.HealthIsEnabled)
            {
                try
                {
                    var h = new Health.Check();
                    h.Run();
                }
                catch (Exception exc)
                {
                    _log.Error(exc);
                }
            }

            //timeline processing
            if (Configuration.HandlersIsEnabled)
            {
                try
                {
                    var o = new Orchestrator(container);
                    o.Run();
                }
                catch (Exception exc)
                {
                    _log.Error(exc);
                }
            }

            //ghosts singleton
            new ManualResetEvent(false).WaitOne();
        }

        //hook for shutdown tasks
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _log.Debug($"Initiating Ghosts shutdown - Local time: {DateTime.Now.TimeOfDay} UTC: {DateTime.UtcNow.TimeOfDay}");
            StartupTasks.CleanupProcesses();
        }
    }
}  