using LogAra.Domain;

namespace LogAra.Application.Abstractions
{
    public interface ILogAnalysisService
    {
        Task<AnalysisResult> AnalyzeAsync(Stream rawLogStream, CancellationToken cancellationToken);
    }
}
