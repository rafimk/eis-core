using EIS.Application.Constants;
using EIS.Application.Interfaces;
using Quartz;
using System.Threading.Tasks;
namespace EIS.Infrastructure.Scheduler.Jobs;

[DisallowConcurrentExecution]
public class ConsumerKeepAliveEntryPollerJob : IJob
{
    private readonly IMessageQueueManager _messageQueueManager;

    public ConsumerKeepAliveEntryPollerJob(IMessageQueueManager messageQueueManager)
    {
        _messageQueueManager = messageQueueManager;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        if (GlobalVariables.IsDatabaseTablesInitialized && GlobalVariables.IsMessageQueueSubscribed)
        {
            await _messageQueueManager.ConsumerKeepAliveTask();
        }

        await Task.CompletedTask;
    }
}