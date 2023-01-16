using System.Threading.Tasks;

namespace EIS.Application.Interfaces;

public interface IEventPublisherService
{
    Task Publish(IMessageEISProducer messageObject);
}