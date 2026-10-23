namespace AbGen.Api.Domain.Providers;

public sealed record ScriptAnalysisRequest(
    ImportedEbook Ebook,
    string? LanguageHint = null);

public sealed record ScriptSegment(
    Guid Id,
    int SequenceNumber,
    string Text,
    string SpeakerId,
    string Emotion,
    string? ChapterId = null,
    string? ChapterTitle = null);

public sealed record ScriptAnalysisResult(
    IReadOnlyList<ScriptSegment> Segments,
    IReadOnlyList<ProviderMessage> Messages);

public interface IScriptAnalysisProvider
{
    string ProviderId { get; }
    Task<ScriptAnalysisResult> AnalyzeAsync(
        ScriptAnalysisRequest request,
        IProgress<ProviderProgress> progress,
        CancellationToken cancellationToken);
}
