using EIS.Api.Domain.Common;

namespace EIS.Api.Application.Common.Interfaces;

public interface IDomainEventService
{
    Task Publish(DomainEvent domainEvent);
}