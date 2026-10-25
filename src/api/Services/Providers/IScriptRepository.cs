namespace AbGen.Api.Services.Providers;

public interface IScriptRepository
{
    Task SaveAnalysisAsync(
        Guid projectId,
        ScriptAnalysisResult analysis,
        CancellationToken cancellationToken);
    Task<ScriptAnalysisResult?> GetAnalysisAsync(
        Guid projectId,
        CancellationToken cancellationToken);
}
