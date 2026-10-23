namespace AbGen.Api.Domain.Providers;

public enum ProviderMessageLevel
{
    Information,
    Warning,
    Error
}

public sealed record ProviderMessage(
    ProviderMessageLevel Level,
    string Code,
    string Message,
    bool IsRetryable = false);

public sealed record ProviderProgress(
    string Stage,
    int? ProgressPercent = null,
    string? Message = null);
