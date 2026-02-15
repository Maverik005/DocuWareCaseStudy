using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Text;

namespace EventRegistration.Core.Utilities
{
    public sealed class EventLimitsConfiguration ()
    {
        public int MaxEventsPerCreator { get; init; } = 100;
        public int MaxRegistrationsPerEvent { get; init; } = 1000;
    }
}
