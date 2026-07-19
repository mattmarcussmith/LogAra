namespace LogAra.Contracts.Responses.Analysis
{
    public sealed class AnalysisResponseDto
    {
        public DateTimeOffset GeneratedAtUtc { get; init; }
        public int TotalEntries { get; init; }
        public int ErrorCount { get; init; }
        public int WarningCount { get; init; }
        public int InformationCount { get; init; }
        public IReadOnlyList<IssueDto> Issues { get; init; } = [];
    }
}