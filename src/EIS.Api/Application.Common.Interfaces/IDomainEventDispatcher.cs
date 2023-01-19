namespace EIS.Api.Application.Common.Interfaces;

public interface IDomainEventDispatcher<T> where T : IHasDomainEvent
{
    Task DispatchEvent(T entity, string eventType);
}