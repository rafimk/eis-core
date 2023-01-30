using System.Reflection.Metadata.Ecma335;
using System.Transactions;
using System.Diagnostics;
using System;
using System.Runtime.CompilerServices;
using EIS.Application.Interfaces;
using EIS.Domain.Entities;
using MediatR;
using EIS.Api.Application.MDM.Common;
using EIS.Api.Application.Constants;
using EIS.Api.Application.Common.Behaviour;
using EIS.Api.Application.Contrats;
using EIS.Application.Util;

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

        var sourceSystemName = payload?.SourceSystemName;

        if (sourceSystemName.Equals("MDM"))
        {
            _logger.LogInformation($"MDM Event:: {eventType} Received");

            var tableName = payload.ContentType;

            if (tableName == null)
            {
                throw new NullReferenceException("Content Type is null!");
            }

            payloadContractCommand = EisMDMTableMapper.MapTableToSerializedObject(tableName, payloadContent.ToString());
        }

        if (sourceSystemName.Equals("ITEM-MANAGEMENT"))
        {
            if (eventType.Equals(EISEventTypes.ItemManagement.ITEM_CREATED))
            {
                payloadContractCommand = JsonSerializerUtil.DeserializeObject<ItemCreatedContract>(payloadContent.ToString());
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