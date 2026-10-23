namespace AbGen.Api.Domain.Providers;

public enum EbookFormat
{
    Epub,
    Mobi,
    Pdf
}

public sealed record EbookImportRequest(
    string FileName,
    EbookFormat Format,
    Stream Content);

public sealed record ImportedChapter(
    string Id,
    int SequenceNumber,
    string Title,
    string Content);

public sealed record ImportedEbook(
    string Id,
    string Title,
    IReadOnlyList<ImportedChapter> Chapters);

public interface IEbookImporter
{
    EbookFormat Format { get; }
    Task<ImportedEbook> ImportAsync(
        EbookImportRequest request,
        IProgress<ProviderProgress> progress,
        CancellationToken cancellationToken);
}
