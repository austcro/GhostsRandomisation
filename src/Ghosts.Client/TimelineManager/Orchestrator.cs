// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Ghosts.Client.Infrastructure;
using Ghosts.Client.Handlers;
using Ghosts.Domain;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Security.Permissions;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Ghosts.Contracts.Interfaces;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Ghosts.Client.TimelineManager
{
    /// <summary>
    /// Translates timeline.config file events into their appropriate handler
    /// </summary>
    public class Orchestrator
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static DateTime _lastRead = DateTime.MinValue;
        private List<Thread> _threads { get; set; }
        private List<ThreadJob> _threadJobs { get; set; }
        private Thread MonitorThread { get; set; }
        private Timeline _timeline;
        private FileSystemWatcher timelineWatcher;
        private bool _isSafetyNetRunning = false;
        private bool _isTempCleanerRunning = false;

        private bool _isWordInstalled { get; set; }
        private bool _isExcelInstalled { get; set; }
        private bool _isPowerPointInstalled { get; set; }
        private bool _isOutlookInstalled { get; set; }

        private IUnityContainer _container;

        public Orchestrator()
        {

        }
        public Orchestrator(IUnityContainer container)
        {
            this._container = container;
        }
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Run()
        {
            try
            {
                if (_isSafetyNetRunning != true) //checking if safetynet has already been started
                {
                    this.StartSafetyNet(); //watch instance numbers
                    _isSafetyNetRunning = true;
                }

                if (_isTempCleanerRunning != true) //checking if tempcleaner has been started
                {
                    TempFiles.StartTempFileWatcher(); //watch temp directory on a diff schedule
                    _isTempCleanerRunning = true;
                }

                this._timeline = TimelineBuilder.GetLocalTimeline();

                // now watch that file for changes
                if (timelineWatcher == null) //you can change this to a bool if you want but checks if the object has been created
                {
                    _log.Trace("Timeline watcher starting and is null...");
                    timelineWatcher = new FileSystemWatcher(TimelineBuilder.TimelineFilePath().DirectoryName)
                    {
                        Filter = Path.GetFileName(TimelineBuilder.TimelineFilePath().Name)
                    };
                    _log.Trace($"watching {Path.GetFileName(TimelineBuilder.TimelineFilePath().Name)}");
                    timelineWatcher.NotifyFilter = NotifyFilters.LastWrite;
                    timelineWatcher.EnableRaisingEvents = true;
                    timelineWatcher.Changed += OnChanged;
                }

                _threadJobs = new List<ThreadJob>();

                //load into an managing object
                //which passes the timeline commands to handlers
                //and creates a thread to execute instructions over that timeline
                CancellationTokenSource tokenSource = new CancellationTokenSource();
                CancellationToken token;
                Task t;
                var tasks = new ConcurrentBag<Task>();

                if (this._timeline.Status == Timeline.TimelineStatus.Run)
                {
                    token = tokenSource.Token;
                    RunEx(this._timeline, token);
                }
                else if(this._timeline.Status == Timeline.TimelineStatus.Stop)
                {
                    tokenSource.Cancel(true);
                    token = tokenSource.Token;
                    //RunEx(this._timeline, token);
                    t = Task.Run(() => RunEx(this._timeline, token), token);
                    //Console.WriteLine("Task {0} executing", t.Id);
                    tasks.Add(t);
                }
                else
                {
                    if (MonitorThread != null)
                    {
                        MonitorThread.Abort();
                        MonitorThread = null;
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error($"Orchestrator.Run exception: {e}");
            }
        }

        public void Shutdown()
        {
            try
            {
                foreach (var thread in _threads)
                {
                    thread.Abort(null);
                }
            }
            catch { }
        }

        private void RunEx(Timeline timeline, CancellationToken token)
        {
            _threads = new List<Thread>();
            
            WhatsInstalled();

            Task t = Task.Run(() =>
            {
                // Create some cancelable child tasks.
                Task tc;
                foreach (TimelineHandler handler in timeline.TimeLineHandlers)
                {
                    tc = Task.Run(() => ThreadLaunch(timeline, handler));
                    //ThreadLaunch(timeline, handler);
                }
            });
            

            //foreach (TimelineHandler handler in timeline.TimeLineHandlers)
            //{
            //    ThreadLaunch(timeline, handler);
            //}
        }

        public void RunCommand(TimelineHandler handler)
        {
            WhatsInstalled();
            //ThreadLaunch(null, handler);
        }

        ///here lies technical debt
        //TODO clean up
        private void StartSafetyNet()
        {
            try
            {
                var t = new Thread(SafetyNet)
                {
                    IsBackground = true,
                    Name = "ghosts-safetynet"
                };
                t.Start();
            }
            catch (Exception e)
            {
                _log.Error($"SafetyNet thread launch exception: {e}");
            }
        }

        ///here lies technical debt
        //TODO clean up
        // if supposed to be one excel running, and there is more than 2, then kill race condition
        private static void SafetyNet()
        {
            while (true)
            {
                try
                {
                    _log.Trace("SafetyNet loop beginning");

                    FileListing.FlushList(); //Added 6/10 by AMV to clear clogged while loop.

                    var timeline = TimelineBuilder.GetLocalTimeline();

                    var handlerCount = timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.Excel);
                    var pids = ProcessManager.GetPids(ProcessManager.ProcessNames.Excel).ToList();
                    if (pids.Count > handlerCount + 1)
                    {
                        _log.Trace($"SafetyNet excel handlers: {handlerCount} pids: {pids.Count} - killing");
                        ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.Excel);
                    }

                    handlerCount = timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.PowerPoint);
                    pids = ProcessManager.GetPids(ProcessManager.ProcessNames.PowerPoint).ToList();
                    if (pids.Count > handlerCount + 1)
                    {
                        _log.Trace($"SafetyNet powerpoint handlers: {handlerCount} pids: {pids.Count} - killing");
                        ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.PowerPoint);
                    }

                    handlerCount = timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.Word);
                    pids = ProcessManager.GetPids(ProcessManager.ProcessNames.Word).ToList();
                    if (pids.Count > handlerCount + 1)
                    {
                        _log.Trace($"SafetyNet word handlers: {handlerCount} pids: {pids.Count} - killing");
                        ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.Word);
                    }
                    _log.Trace("SafetyNet loop ending");
                }
                catch (Exception e)
                {
                    _log.Trace($"SafetyNet exception: {e}");
                }
                finally
                {
                    Thread.Sleep(60000); //every 60 seconds clean up
                }
            }
        }

        private void WhatsInstalled()
        {
            using (RegistryKey regWord = Registry.ClassesRoot.OpenSubKey("Outlook.Application"))
            {
                if (regWord != null)
                {
                    _isOutlookInstalled = true;
                }

                _log.Trace($"Outlook is installed: {_isOutlookInstalled}");
            }

            using (RegistryKey regWord = Registry.ClassesRoot.OpenSubKey("Word.Application"))
            {
                if (regWord != null)
                {
                    _isWordInstalled = true;
                }

                _log.Trace($"Word is installed: {_isWordInstalled}");
            }

            using (RegistryKey regWord = Registry.ClassesRoot.OpenSubKey("Excel.Application"))
            {
                if (regWord != null)
                {
                    _isExcelInstalled = true;
                }

                _log.Trace($"Excel is installed: {_isExcelInstalled}");
            }

            using (RegistryKey regWord = Registry.ClassesRoot.OpenSubKey("PowerPoint.Application"))
            {
                if (regWord != null)
                {
                    _isPowerPointInstalled = true;
                }

                _log.Trace($"PowerPoint is installed: {_isPowerPointInstalled}");
            }
        }

        private void ThreadLaunch(Timeline timeline, TimelineHandler handler)
        {

            try
            {
                _log.Trace($"Attempting new thread for: {handler.HandlerType}");

                //IServiceProvider serviceProvider;
                //IWord wordHandler = null;

                //serviceProvider = new ServiceCollection()
                //   .AddSingleton<IWord, Word>()
                //   .BuildServiceProvider();

                //IUnityContainer container = new UnityContainer();
                //container.RegisterType<IWord, Word>();

                IWord word;// = container.Resolve<IWord>();
                IPowerPoint powerPoint;
                IReboot rebootsystem;
                IOutlook outlook;
                IWatcher watcher;
                IPrint print;
                IExcel excel;
                IClicks clicks;
                INotepad notepad;
                ICmd cmd;
                IBrowserIE browserie;
                IBrowserChrome browserchrome;
                IBrowserFirefox browserfirefox;
                INpcSystem npcsystem;

                Thread t = null;
                ThreadJob threadJob = new ThreadJob
                {
                    Id = Guid.NewGuid().ToString(),
                    Handler = handler
                };

                switch (handler.HandlerType)
                {
                    case HandlerType.NpcSystem:
                        //NpcSystem npc = new NpcSystem(handler);
                        npcsystem = _container.Resolve<INpcSystem>();
                        npcsystem.CallHandlerAction(timeline, handler);
                        break;
                    case HandlerType.Command:
                        t = new Thread(() =>
                        {
                            //Cmd o = new Cmd(handler);
                            cmd = _container.Resolve<ICmd>();
                            cmd.CallHandlerAction(timeline, handler);

                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();

                        threadJob.ProcessName = ProcessManager.ProcessNames.Command;

                        break;
                    case HandlerType.Word:
                        _log.Trace("Launching thread for word");
                        if (_isWordInstalled)
                        {
                            var pids = ProcessManager.GetPids(ProcessManager.ProcessNames.Word).ToList();
                            if (pids.Count > timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.Word))
                                return;
                            Task.Run(() => {
                                //ExcelHandler o = new ExcelHandler(timeline, handler);
                                word = _container.Resolve<IWord>();
                                word.CallHandlerAction(timeline, handler);
                            });
                            Thread.CurrentThread.Name = threadJob.Id;
                            Thread.CurrentThread.IsBackground = true;


                            //t = new Thread(() =>
                            //{
                            //    //WordHandler o = new WordHandler(timeline, handler);
                            //    //setup our DI                                

                            //    //serviceProvider = new ServiceCollection()
                            //    //   .AddSingleton<IWord, Word>()
                            //    //   .BuildServiceProvider();

                            //    //wordHandler = serviceProvider.GetService<IWord>();
                            //    //wordHandler.CallHandlerAction(timeline, handler);
                            //    word = _container.Resolve<IWord>();
                            //    word.CallHandlerAction(timeline, handler);
                            //})
                            //{
                            //    IsBackground = true,
                            //    Name = threadJob.Id
                            //};
                            //t.Start();

                            threadJob.ProcessName = ProcessManager.ProcessNames.Word;
                        }
                        break;
                    case HandlerType.Excel:
                        _log.Trace("Launching thread for excel");
                        if (_isExcelInstalled)
                        {
                            var pids = ProcessManager.GetPids(ProcessManager.ProcessNames.Excel).ToList();
                            if (pids.Count > timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.Excel))
                                return;
                            Task.Factory.StartNew(() => {
                                //ExcelHandler o = new ExcelHandler(timeline, handler);
                                excel = _container.Resolve<IExcel>();
                                excel.CallHandlerAction(timeline, handler);
                            });
                            Thread.CurrentThread.Name = threadJob.Id;
                            Thread.CurrentThread.IsBackground = true;
                            //t = new Thread(() =>
                            //{
                            //    //ExcelHandler o = new ExcelHandler(timeline, handler);
                            //    excel = _container.Resolve<IExcel>();
                            //    excel.CallHandlerAction(timeline, handler);
                            //})
                            //{
                            //    IsBackground = true,
                            //    Name = threadJob.Id
                            //};
                            //t.Start();

                            threadJob.ProcessName = ProcessManager.ProcessNames.Excel;
                        }
                        break;
                    case HandlerType.Clicks:
                        _log.Trace("Launching thread to handle clicks");
                        t = new Thread(() =>
                        {
                            //Clicks o = new Clicks(handler);
                            clicks = _container.Resolve<IClicks>();
                            clicks.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();
                        break;
                    case HandlerType.Reboot:
                        _log.Trace("Launching thread to handle reboot");
                        t = new Thread(() =>
                        {
                            //Reboot o = new Reboot(handler);
                            rebootsystem = _container.Resolve<IReboot>();
                            rebootsystem.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();
                        break;
                    case HandlerType.PowerPoint:
                        _log.Trace("Launching thread for powerpoint");
                        if (_isPowerPointInstalled)
                        {
                            var pids = ProcessManager.GetPids(ProcessManager.ProcessNames.PowerPoint).ToList();
                            if (pids.Count > timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.PowerPoint))
                                return;
                            Task.Factory.StartNew(() => {
                                //ExcelHandler o = new ExcelHandler(timeline, handler);
                                powerPoint = _container.Resolve<IPowerPoint>();
                                powerPoint.CallHandlerAction(timeline, handler);
                            });
                            Thread.CurrentThread.Name = threadJob.Id;
                            Thread.CurrentThread.IsBackground = true;
                            //t = new Thread(() =>
                            //{
                            //    //PowerPointHandler o = new PowerPointHandler(timeline, handler);
                            //    powerPoint= _container.Resolve<IPowerPoint>();
                            //    powerPoint.CallHandlerAction(timeline, handler);
                            //})
                            //{
                            //    IsBackground = true,
                            //    Name = threadJob.Id
                            //};
                            //t.Start();

                            threadJob.ProcessName = ProcessManager.ProcessNames.PowerPoint;
                        }
                        break;
                    case HandlerType.Outlook:
                        _log.Trace("Launching thread for outlook - note we're not checking if outlook installed, just going for it");
                        //if (this.IsOutlookInstalled)
                        //{
                        Task.Factory.StartNew(() => {
                            //ExcelHandler o = new ExcelHandler(timeline, handler);
                            outlook = _container.Resolve<IOutlook>();
                            outlook.CallHandlerAction(timeline, handler);
                        });
                        Thread.CurrentThread.Name = threadJob.Id;
                        Thread.CurrentThread.IsBackground = true;

                        //t = new Thread(() =>
                        //{
                        //    //Outlook o = new Outlook(handler);
                        //    outlook = _container.Resolve<IOutlook>();
                        //    outlook.CallHandlerAction(timeline, handler);
                        //})
                        //{
                        //    IsBackground = true,
                        //    Name = threadJob.Id
                        //};
                        //t.Start();

                        threadJob.ProcessName = ProcessManager.ProcessNames.Outlook;
                        //}

                        break;
                    case HandlerType.BrowserIE:
                        //IE demands COM apartmentstate be STA so diff thread creation required
                        t = new Thread(() =>
                        {
                            //BrowserIE o = new BrowserIE(handler);
                            browserie = _container.Resolve<IBrowserIE>();
                            browserie.CallHandlerAction(timeline, handler);
                        });
                        t.SetApartmentState(ApartmentState.STA);
                        t.IsBackground = true;
                        t.Name = threadJob.Id;
                        t.Start();

                        break;
                    case HandlerType.Notepad:
                        //TODO
                        t = new Thread(() =>
                        {
                            //Notepad o = new Notepad(handler);
                            notepad = _container.Resolve<INotepad>();
                            notepad.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();

                        break;

                    case HandlerType.BrowserChrome:
                        t = new Thread(() =>
                        {
                            //BrowserChrome o = new BrowserChrome(handler);
                            browserchrome = _container.Resolve<IBrowserChrome>();
                            browserchrome.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();

                        threadJob.ProcessName = ProcessManager.ProcessNames.Chrome;

                        break;
                    case HandlerType.BrowserFirefox:
                        t = new Thread(() =>
                        {
                            //BrowserFirefox o = new BrowserFirefox(handler);
                            browserfirefox = _container.Resolve<IBrowserFirefox>();
                            browserfirefox.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();

                        threadJob.ProcessName = ProcessManager.ProcessNames.Firefox;

                        break;
                    case HandlerType.Watcher:
                        t = new Thread(() =>
                        {
                            //Watcher o = new Watcher(handler);
                            watcher = _container.Resolve<IWatcher>();
                            watcher.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();

                        //threadJob.ProcessName = ProcessManager.ProcessNames.Watcher;

                        break;
                    case HandlerType.Print:
                        t = new Thread(() =>
                        {
                            //var p = new Print(handler);
                            print = _container.Resolve<IPrint>();
                            print.CallHandlerAction(timeline, handler);
                        })
                        {
                            IsBackground = true,
                            Name = threadJob.Id
                        };
                        t.Start();

                        break;
                }

                if (threadJob.ProcessName != null)
                {
                    _threadJobs.Add(threadJob);
                }

                if (t != null)
                {
                    _threads.Add(t);
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                _log.Trace($"FileWatcher event raised: {e.FullPath} {e.Name} {e.ChangeType}");

                // filewatcher throws two events, we only need 1
                DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);
                if (lastWriteTime != _lastRead)
                {
                    _lastRead = lastWriteTime;
                    _log.Trace("FileWatcher Processing: " + e.FullPath + " " + e.ChangeType);
                    _log.Trace($"Reloading {MethodBase.GetCurrentMethod().DeclaringType}");

                    _log.Trace("terminate existing tasks and rerun orchestrator");

                    try
                    {
                        Shutdown();
                    }
                    catch (Exception exception)
                    {
                        _log.Info(exception);
                    }

                    try
                    {
                        StartupTasks.CleanupProcesses();
                    }
                    catch (Exception exception)
                    {
                        _log.Info(exception);
                    }

                    Thread.Sleep(7500);

                    try
                    {
                        Run();
                    }
                    catch (Exception exception)
                    {
                        _log.Info(exception);
                    }
                }
            }
            catch (Exception exc)
            {
                _log.Info(exc);
            }

        }
    }

    public class ThreadJob
    {
        public string Id { get; set; }
        public TimelineHandler Handler { get; set; }
        public string ProcessName { get; set; }
    }
}
