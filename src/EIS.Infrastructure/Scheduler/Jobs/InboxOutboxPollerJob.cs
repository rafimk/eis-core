using EIS.Application.Constants;
using EIS.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Threading.Tasks;
namespace EIS.Infrastructure.Scheduler.Jobs;

public class InboxOutboxPollerJob : IJob
{
    private readonly IMessageQueueManager _messageQueueManager;
    private readonly ILogger<InboxOutboxPollerJob> _log;

    public InboxOutboxPollerJob(IMessageQueueManager messageQueueManager, ILogger<InboxOutboxPollerJob> log)
    {
        _messageQueueManager = messageQueueManager;
        _log = log;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _log.LogInformation("QuartzInboxOutboxPollerJob >> Executing Task...");

        if (GlobalVariables.IsDatabaseTablesInitialized)
        {
            await _messageQueueManager.InboxOutboxPollerTask();
        }

        await Task.CompletedTask;
    }
}