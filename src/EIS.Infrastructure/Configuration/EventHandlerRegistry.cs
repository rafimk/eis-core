using EIS.Application.Interfaces;

namespace EIS.Infrastructure.Configuration;

public class EventHandlerRegistry : IEventHandlerRegistry
{
    private IMessageProcessor _messageProcessor;

    public void AddMessageProcessor(IMessageProcessor messageProcessor)
    {
        _messageProcessor = messageProcessor;
    }

    public IMessageProcessor GetMessageProcessor()
    {
        return _messageProcessor;
    }
}