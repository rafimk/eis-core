using EIS.Api.Domain.Common;


namespace EIS.Api.Application.Contrats;

public class ItemCreated : IHasDomainEvent
{
    public Guid Id { get; set; }
    public string ItemName { get; set;}
    public DateTime Created { get; set;}

    public List<DomainEvent> DomainEvents { get; set;}  = new List<DomainEvent>();

};

public class ItemCreatedEvent : DomainEvent
{
    public ItemCreated Item { get; }

    public ItemCreatedEvent(ItemCreated item)
    {
        Item = item;
    }
}

