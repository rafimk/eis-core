using EIS.Api.Domain.Common;

namespace EIS.Api.Application.Publisher
{
    public record ItemManager(Guid Id, string ItemName, DateTime Created) : IHasDomainEvent
    {
        public List<DomainEvent> DomainEvents { get ; set ; }
    }
}
