using LogAra.Contracts.Requests.Analysis;
using LogAra.Contracts.Responses.Analysis;

namespace LogAra.Application.Abstractions
{
    public interface IAnalysisApiService
    {
        Task<AnalysisResponseDto> AnalyzeAsync(Stream rawLogStream, CancellationToken cancellationToken);
        Task<IssueExplanationDto> ExplainAsync(ExplainIssueRequestDto request, CancellationToken cancellationToken);
    }
}
