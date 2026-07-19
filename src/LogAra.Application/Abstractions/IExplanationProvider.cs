using LogAra.Domain;

namespace LogAra.Application.Abstractions
{
    public interface IExplanationProvider
    {
        Task<string> GetExplanationAsync(string fingerprint, LogEntry representativeEntry, int occurrences, CancellationToken cancellationToken);
    }
}
