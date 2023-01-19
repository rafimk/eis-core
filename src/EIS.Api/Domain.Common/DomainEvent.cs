using System;
namespace EIS.Api.Domain.Common;

public abstract class DomainEvent
{
    public Guid id { get; set; }

    public bool IsPublished { get; set; }
    public DateTimeOffset DateOccurred { get; protected set; } = DateTimeOffset.UtcNow;

    protected DomainEvent()
    {
        DateOccurred = DateTimeOffset.UtcNow;
    }
}