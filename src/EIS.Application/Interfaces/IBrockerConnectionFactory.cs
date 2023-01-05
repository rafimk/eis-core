namespace EIS.Application.Interfaces
{
    public interface IBrockerConnectionFactory : IDisposable
    {
        void CreateConsumerListener();
        void DestroyConsumerConnection();
        void PublishTopic(EisEvent eisEvent);
    }
}