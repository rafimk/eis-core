using System.Reflection.Metadata.Ecma335;
using System.Transactions;
using System.Diagnostics;
using System;
using System.Runtime.CompilerServices;
namespace EIS.Api.Infrastructure.Integration.Service;

public class EisEventProcessorService : IMessageProcessor
{
    private ILogger<EisEventProcessorService> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EisEventProcessorService(ILogger<EisEventProcessorService> logger, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task Process(Payload payload, string eventType)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var payloadContent = payload.Content;
        object payloadContractCommand = null;

        var sourceSystemName = payload?.sourceSystemName;

        if (sourceSystemName.Equals("MDM"))
        {
            _logger.LogInformation($"MDM Event:: {eventType} Received");

            var tableName = payload.ContentType;

            if (tableName == null)
            {
                throw new DllNotFoundException(nameof(EisEventProcessorService), "Payload content type is bull");
            }

            payloadContractCommand = EisMDMTableMapper.MapTableToSerializedObject(tableName, payloadContent.ToString());
        }

        if (sourceSystemName.Equals("ITEM-MANAGEMENT"))
        {
            if (eventType.Equals(EISEventTypes.ItemManagement.ITEM_CREATED))
            {
                payloadContractCommand = EIS.Application.Util.JsonSerializeObject<ItemCreatedCommand>(payloadContent.ToString());
            }
        }

        if (payloadContractCommand is not null)
        {
            _logger.LogInformation($"Executing --> Source : {sourceSystemName} : Event {eventType} ");
            await mediator.Send(payloadContractCommand);
        }
        else
        {
            _logger.LogDebug($"{nameof(EisEventProcessorService)} --> received payload - not used for {sourceSystemName}");
        }
    }
}