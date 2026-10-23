namespace AbGen.Api.Domain.Providers;

public sealed record VoiceDescriptor(
    string ProviderVoiceId,
    string DisplayName,
    string Language,
    IReadOnlySet<string> SupportedEmotions);

public sealed record TextToSpeechCapabilities(
    bool SupportsEmotionPrompts,
    bool SupportsVoiceMixing,
    bool SupportsStreaming,
    IReadOnlySet<string> SupportedLanguages);

public sealed record VoiceMapping(
    string CharacterId,
    string ProviderId,
    string ProviderVoiceId);

public sealed record SynthesisSegment(
    Guid SegmentId,
    string Text,
    string CharacterId,
    string ProviderVoiceId,
    string Emotion,
    string? EmotionPrompt = null);

public sealed record SynthesisResult(
    Guid SegmentId,
    Stream Audio,
    string MediaType,
    TimeSpan? Duration = null);

public interface IVoiceCatalog
{
    string ProviderId { get; }
    Task<IReadOnlyList<VoiceDescriptor>> GetVoicesAsync(CancellationToken cancellationToken);
}

public interface IVoiceAssignmentStore
{
    Task<VoiceMapping?> GetMappingAsync(
        string projectId,
        string characterId,
        string providerId,
        CancellationToken cancellationToken);
    Task SaveMappingAsync(
        string projectId,
        VoiceMapping mapping,
        CancellationToken cancellationToken);
}

public interface ITextToSpeechProvider
{
    string ProviderId { get; }
    TextToSpeechCapabilities Capabilities { get; }
    Task<IReadOnlyList<VoiceDescriptor>> GetVoicesAsync(CancellationToken cancellationToken);
    Task<SynthesisResult> SynthesizeAsync(
        SynthesisSegment segment,
        IProgress<ProviderProgress> progress,
        CancellationToken cancellationToken);
}
