﻿// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using Ghosts.Domain;
using Ghosts.Domain.Code;
using NLog;
using Ghosts.Client.Handlers;
using Ghosts.Contracts.Interfaces;

namespace Ghosts.Client.InterfaceImpl
{
    public class Cmd : BaseHandler, ICmd
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        private const int WmKeyUp = 0x101;

        public Process Process;

        public bool CallHandlerAction(Timeline timeline, TimelineHandler handler)
        {
            try
            {
                EmulateCmdAction(handler);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        private void EmulateCmdAction(TimelineHandler handler)
        {
            try
            {
                _log.Trace("Spawning cmd.exe...");
                this.Process = Process.Start("cmd.exe");
            
                if (handler.Loop)
                {
                    while (true)
                    {
                        Ex(handler);
                    }
                }
                else
                {
                    Ex(handler);
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        public void Ex(TimelineHandler handler)
        {
            foreach (var timelineEvent in handler.TimeLineEvents)
            {
                WorkingHours.Is(handler);

                if (timelineEvent.DelayBefore > 0)
                    this.Sleep(timelineEvent.DelayBefore);

                _log.Trace($"Command line: {timelineEvent.Command} with delay after of {timelineEvent.DelayAfter}");

                switch (timelineEvent.Command)
                {
                    case "random":
                        while (true)
                        {
                            var cmd = timelineEvent.CommandArgs[new Random().Next(0, timelineEvent.CommandArgs.Count)];
                            if (!string.IsNullOrEmpty(cmd.ToString()))
                            {
                                this.Command(handler, timelineEvent, cmd.ToString());
                            }
                            Thread.Sleep(timelineEvent.DelayAfter);
                        }
                    default:
                        this.Command(handler, timelineEvent, timelineEvent.Command);

                        foreach (var cmd in timelineEvent.CommandArgs)
                            if (!string.IsNullOrEmpty(cmd.ToString()))
                                this.Command(handler, timelineEvent, cmd.ToString());
                        break;
                }

                if (timelineEvent.DelayAfter > 0)
                    this.Sleep(timelineEvent.DelayAfter);
            }
        }

        public void Command(TimelineHandler handler, TimelineEvent timelineEvent, string command)
        {
            this.Sleep(1000);

            SetForegroundWindow(this.Process.MainWindowHandle);
            var i = new InputSimulator();
            i.Keyboard.TextEntry(command);
            i.Keyboard.KeyPress(VirtualKeyCode.RETURN);

            this.Report(handler.HandlerType.ToString(), command, "", timelineEvent.TrackableId);
        }

        public void Sleep(int length)
        {
            Thread.Sleep(length);
        }

        public void Kill()
        {
            this.Process.Kill();
        }

      
    }
}