using Microsoft.EntityFrameworkCore.Storage;

namespace AbGen.Api.Infrastructure.Persistence;

public sealed class JobTransaction(IDbContextTransaction? transaction) : IAsyncDisposable
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction is null ? Task.CompletedTask : transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() =>
        transaction?.DisposeAsync() ?? ValueTask.CompletedTask;
}
