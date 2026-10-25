namespace AbGen.Api.Services.Providers;

public interface IAudioStorage
{
    Task<string> SaveAsync(
        Guid projectId,
        string artifactName,
        Stream content,
        CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}
