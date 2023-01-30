using EIS.Application.Interfaces;
using EIS.Domain.Entities;
using System;

namespace EIS.Api.Application.Common.Behaviour;

public class EisEventPayloadBehaviour : IMessageEISProducer
{
    private string _eventType;

    private Payload _payload;

    public EisEventPayloadBehaviour(Payload payload, string eventType)
    {
        _payload = payload;
        _eventType = eventType;
    }

    public EisEventPayloadBehaviour(Payload payload)
    {
        _payload = payload;
    }

    public string GetEventType()
    {
        return _eventType;
    }

    public Payload GetPayLoad()
    {
        return _payload;
    }

    public string GetTraceId()
    {
        return Guid.NewGuid().ToString();
    }


}