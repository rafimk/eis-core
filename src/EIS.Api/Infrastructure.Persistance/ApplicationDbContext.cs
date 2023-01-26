using EIS.Api.Application.Common.Interfaces;
using EIS.Api.Domain.Common;
using Microsoft.EntityFrameworkCore;
using System.Threading;
namespace Infrastructure.Persistance;

public class ApplicationDbContext : DbContext
{
    private readonly IDomainEventService _domainEventService;


    public ApplicationDbContext(DbContextOptions options, IDomainEventService domainEventService) : base(options)
    {
        _domainEventService = domainEventService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken CancellationToken = new CancellationToken())
    {
        var result = await base.SaveChangesAsync(CancellationToken);
        await DispatchEvents();
    }

    private async Task DispatchEvents()
    {
        while (true)
        {
            var domainEventEntity = ChangeTracker.Entries<IHasDomainEvent>()
                .Select(x => x.Entity.DomainEvents)
                .SelectMany(x => x)
                .FirstOrDefault(domainEvent => !domainEvent.IsPublished);

            if (domainEventEntity is null)
            {
                break;
            }

            domainEventEntity.IsPublished = true;
            await _domainEventService.Publish(domainEventEntity);
        }
    }
}