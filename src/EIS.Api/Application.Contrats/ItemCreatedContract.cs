namespace EIS.Api.Application.Contrats;

public class ItemCreatedContract
{
    public Guid Id { get; set; }
    public string ItemName { get; set; }
    public DateTime Created { get; set; }
}

