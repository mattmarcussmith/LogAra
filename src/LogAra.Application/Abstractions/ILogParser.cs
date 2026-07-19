using LogAra.Domain;

namespace LogAra.Application.Abstractions
{
    public interface ILogParser
    {
        Task<IReadOnlyList<LogEntry>> ParseAsync(Stream stream, CancellationToken cancellationToken);
    }
}
