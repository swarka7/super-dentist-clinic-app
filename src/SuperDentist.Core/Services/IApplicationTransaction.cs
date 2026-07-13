using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Services
{
    public interface IApplicationTransaction
    {
        Task<T> ExecuteAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default);
    }
}
