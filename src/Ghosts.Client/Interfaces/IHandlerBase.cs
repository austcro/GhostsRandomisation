using Ghosts.Domain;
using System;

namespace Ghosts.Client.Interfaces
{
    public interface IHandlerBase
    {
        Boolean CallHandlerAction(Timeline timeline, TimelineHandler handler);
    }
}
