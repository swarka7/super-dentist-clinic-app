using System.Threading;
using System.Threading.Tasks;

namespace SuperDentist.Core.Services
{
    public interface IDatabaseInitializer
    {
        Task<InitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
    }

    public sealed record InitializationResult(bool IsNewDatabase, string DatabasePath);
}
