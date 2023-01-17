using EIS.Domain.Entities;

namespace EIS.Application.Interfaces;

public interface IConfigurationManager
{
    string GetBrokerUrl();
    ApplicationSettings GetAppSettings();
    BrokerConfiguration GetBrokerConfiguration();
    void Dispose();

    string GetSourceSystemName();
    bool GetMessageSubscriptionStatus();
    
}