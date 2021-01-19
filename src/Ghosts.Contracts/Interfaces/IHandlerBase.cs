using Ghosts.Domain;
using System;

namespace Ghosts.Contracts.Interfaces
{
    public interface IHandlerBase
    {
        Boolean CallHandlerAction(Timeline timeline, TimelineHandler handler);
    }
}
