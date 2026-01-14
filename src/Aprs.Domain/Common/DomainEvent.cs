using System;

namespace Aprs.Domain.Common;

public abstract record DomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
