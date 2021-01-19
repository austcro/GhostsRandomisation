﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using ghosts.client.linux.Infrastructure;
using ghosts.client.linux.timelineManager;
using Ghosts.Domain.Code;
using NLog;

namespace ghosts.client.linux
{
    class Program
    {
        
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        internal static ClientConfiguration Configuration { get; set; }
        internal static Options OptionFlags;
        internal static bool IsDebug;
        private static ListenerManager _listenerManager { get; set; }

        static void Main(string[] args)
        {
            ClientConfigurationLoader.UpdateConfigurationWithEnvVars();
            
            
            try
            {
                Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fatal exception in {ApplicationDetails.Name} {ApplicationDetails.Version}: {e}", Color.Red);
                _log.Fatal(e);
                Console.ReadLine();
            }
        }

        private static void Run(string[] args)
        {
            // ignore all certs
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // parse program flags
            if (!CommandLineFlagManager.Parse(args))
            {
                return;
            }

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            _log.Trace($"Initiating {ApplicationDetails.Name} startup - Local: {DateTime.Now.TimeOfDay} UTC: {DateTime.UtcNow.TimeOfDay}");

            //load configuration
            try
            {
                Program.Configuration = ClientConfigurationLoader.Config;
            }
            catch (Exception e)
            {
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var o = $"Exec path: {path} - configuration 404: {ApplicationDetails.ConfigurationFiles.Application} - exiting. Exception: {e}";
                _log.Fatal(o);
                Console.WriteLine(o, Color.Red);
                Console.ReadLine();
                return;
            }

            //linux clients do not catch stray processes or check for job duplication

            StartupTasks.SetStartup();

            _listenerManager = new ListenerManager();

            //check id
            _log.Trace(Comms.CheckId.Id);

            //connect to command server for updates and sending logs
            Comms.Updates.Run();

            //linux clients do not perform local survey

            if (Configuration.HealthIsEnabled)
            {
                var h = new Health.Check();
                h.Run();
            }

            if (Configuration.HandlersIsEnabled)
            {
                var o = new Orchestrator();
                o.Run();
            }

            new ManualResetEvent(false).WaitOne();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _log.Debug($"Initiating {ApplicationDetails.Name} shutdown - Local: {DateTime.Now.TimeOfDay} UTC: {DateTime.UtcNow.TimeOfDay}");
        }
    }
}