using System.Collections.Generic;

namespace SuperDentist.Application.Queries
{
    public sealed record PagedResult<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Limit,
        int Offset);
}
