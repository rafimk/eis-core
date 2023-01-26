namespace EIS.Api.Application.Contrats
{
    public record ItemCreatedEvent(Guid Id, string ItemName, DateTime Created);
}
