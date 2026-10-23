using AbGen.Api.Application.Jobs;

namespace AbGen.Api.Infrastructure.Jobs;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
