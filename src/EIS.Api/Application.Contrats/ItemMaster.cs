using EIS.Api.Domain.Common;

namespace EIS.Api.Application.Contrats
{
    public record ItemMaster(Guid Id, string ItemName, DateTime Created) : IHasDomainEvent
    {
        public List<DomainEvent>? DomainEvents { get; set; } 
    }
}
