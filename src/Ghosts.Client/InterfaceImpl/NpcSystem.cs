// Copyright 2017 Carnegie Mellon University. All Rights Reserved. See LICENSE.md file for terms.

using Ghosts.Client.Handlers;
using Ghosts.Client.Infrastructure;

using Ghosts.Client.TimelineManager;
using Ghosts.Contracts.Interfaces;
using Ghosts.Domain;
using NLog;
using System;

namespace Ghosts.Client.InterfaceImpl
{
    public class NpcSystem : BaseHandler, INpcSystem
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public bool CallHandlerAction(Timeline timeline, TimelineHandler handler)
        {
            try
            {
                EmulateNpcSystem(handler);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private void EmulateNpcSystem(TimelineHandler handler)
        {
            _log.Trace($"Handling NpcSystem call: {handler}");

            foreach (var timelineEvent in handler.TimeLineEvents)
            {
                if (string.IsNullOrEmpty(timelineEvent.Command))
                    continue;

                Timeline timeline;

                switch (timelineEvent.Command.ToLower())
                {
                    case "start":
                        timeline = TimelineBuilder.GetLocalTimeline();
                        timeline.Status = Timeline.TimelineStatus.Run;
                        TimelineBuilder.SetLocalTimeline(timeline);
                        break;
                    case "stop":
                        timeline = TimelineBuilder.GetLocalTimeline();
                        timeline.Status = Timeline.TimelineStatus.Stop;

                        StartupTasks.CleanupProcesses();

                        TimelineBuilder.SetLocalTimeline(timeline);
                        break;
                }
            }
        }

    }
}