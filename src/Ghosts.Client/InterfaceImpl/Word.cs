// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using NLog;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Ghosts.Client.Infrastructure;

using Ghosts.Domain;
using Ghosts.Domain.Code;
//using NetOffice.WordApi.Enums;
using Ghosts.Client.Handlers;
using Ghosts.Contracts.Interfaces;
using Ghosts.NetOfficeProvider;

namespace Ghosts.Client.InterfaceImpl
{
    public class Word : BaseHandler, IWord
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public bool CallHandlerAction(Timeline timeline, TimelineHandler handler)
        {
            try
            {
                EmulateWordAction(timeline,handler);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private void EmulateWordAction(Timeline timeline, TimelineHandler handler)
        {
            _log.Trace("Launching Word handler");
            try
            {
                if (handler.Loop)
                {
                    _log.Trace("Word loop");
                    while (true)
                    {
                        if (timeline != null)
                        {
                            System.Collections.Generic.List<int> pids = ProcessManager.GetPids(ProcessManager.ProcessNames.Word).ToList();
                            if (pids.Count > timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.Word))
                            {
                                continue;
                            }
                        }

                        
                        
                        
                        ExecuteEvents(timeline, handler);
                    }
                }
                else
                {
                    _log.Trace("Word single run");
                    KillApp();
                    ExecuteEvents(timeline, handler);
                    KillApp();
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
                KillApp();
            }
            
        }

        private static void KillApp()
        {
            ProcessManager.KillProcessAndChildrenByName(ProcessManager.ProcessNames.Word);
        }

       

        private void ExecuteEvents(Timeline timeline, TimelineHandler handler)
        {
            try
            {
                foreach (TimelineEvent timelineEvent in handler.TimeLineEvents)
                {
                    try
                    {
                        _log.Trace($"Word event - {timelineEvent}");
                        WorkingHours.Is(handler);

                        if (timelineEvent.DelayBefore > 0)
                        {
                            Thread.Sleep(timelineEvent.DelayBefore);
                        }

                        if (timeline != null)
                        {
                            System.Collections.Generic.List<int> pids = ProcessManager.GetPids(ProcessManager.ProcessNames.Word).ToList();
                            if (pids.Count > timeline.TimeLineHandlers.Count(o => o.HandlerType == HandlerType.Word))
                            {
                                return;
                            }
                        }

                        // start word and turn off msg boxes
                        WordApplication wordApplication = new WordApplication();
                        WordApplicationData wordApplicationData= wordApplication.CreateWordApplicationInstance();
                        //NetOffice.WordApi.Application wordApplication = new NetOffice.WordApi.Application
                        //{
                        //    DisplayAlerts = WdAlertLevel.wdAlertsNone,
                        //    Visible = true
                        //};

                        //// add a new document
                        //NetOffice.WordApi.Document newDocument = wordApplication.Documents.Add();

                        //try
                        //{
                        //    wordApplication.WindowState = WdWindowState.wdWindowStateMinimize;
                        //    foreach (NetOffice.WordApi.Document item in wordApplication.Documents)
                        //    {
                        //        item.Windows[1].WindowState = WdWindowState.wdWindowStateMinimize;
                        //    }
                        //}
                        //catch (Exception e)
                        //{
                        //    _log.Trace($"Could not minimize: {e}");
                        //}

                        // insert some text
                        System.Collections.Generic.List<string> list = RandomText.GetDictionary.GetDictionaryList();
                        RandomText rt = new RandomText(list.ToArray());
                        rt.AddContentParagraphs(1, 1, 1, 10, 50);
                        wordApplicationData.wordApplication.Selection.TypeText(rt.Content);

                        int writeSleep = ProcessManager.Jitter(100);
                        Thread.Sleep(writeSleep);

                        //wordApplicationData.wordApplication.Selection.HomeKey(WdUnits.wdLine, WdMovementType.wdExtend);
                        //wordApplicationData.wordApplication.Selection.Font.Color = WdColor.wdColorSeaGreen;
                        wordApplicationData.wordApplication.Selection.Font.Bold = 1;
                        wordApplicationData.wordApplication.Selection.Font.Size = 18;

                        string rand = RandomFilename.Generate();

                        string dir = timelineEvent.CommandArgs[0].ToString();
                        if (dir.Contains("%"))
                        {
                            dir = Environment.ExpandEnvironmentVariables(dir);
                        }

                        if (Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        string path = $"{dir}\\{rand}.docx";

                        //if directory does not exist, create!
                        _log.Trace($"Checking directory at {path}");
                        DirectoryInfo f = new FileInfo(path).Directory;
                        if (f == null)
                        {
                            _log.Trace($"Directory does not exist, creating directory at {f.FullName}");
                            Directory.CreateDirectory(f.FullName);
                        }

                        try
                        {
                            if (File.Exists(path))
                            {
                                File.Delete(path);
                            }
                        }
                        catch (Exception e)
                        {
                            _log.Debug(e);
                        }

                        wordApplicationData.newDocument.Saved = true;
                        wordApplicationData.newDocument.SaveAs(path);
                        Report(handler.HandlerType.ToString(), timelineEvent.Command, timelineEvent.CommandArgs[0].ToString());

                        FileListing.Add(path);

                        if (timelineEvent.DelayAfter > 0)
                        {
                            //sleep and leave the app open
                            _log.Trace($"Sleep after for {timelineEvent.DelayAfter}");
                            Thread.Sleep(timelineEvent.DelayAfter - writeSleep);
                        }

                        wordApplicationData.wordApplication.Quit();
                        wordApplicationData.wordApplication.Dispose();
                        wordApplicationData.wordApplication = null;

                        try
                        {
                            //Marshal.ReleaseComObject(wordApplicationData.wordApplication);
                            MarshalProvider.MarshalProvider.ReleaseComObject(wordApplication);
                        }
                        catch { }

                        try
                        {
                            //Marshal.FinalReleaseComObject(wordApplication);
                            MarshalProvider.MarshalProvider.FinalReleaseComObject(wordApplication);
                        }
                        catch { }

                        GC.Collect();
                    }
                    catch (Exception e)
                    {
                        _log.Debug(e);
                    }
                    finally
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
            finally
            {
                KillApp();
                _log.Trace($"Word closing...");
            }
        }
    }
}
