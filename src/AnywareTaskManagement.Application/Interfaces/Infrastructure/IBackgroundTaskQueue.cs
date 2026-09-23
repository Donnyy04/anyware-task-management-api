using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnywareTaskManagement.Application.Interfaces.Infrastructure;

public interface IBackgroundTaskQueue
{
    ValueTask QueueAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    ValueTask<Guid> DequeueAsync(
        CancellationToken cancellationToken);
}
