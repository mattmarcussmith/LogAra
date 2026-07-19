using LogAra.Contracts.Responses.Analysis;

namespace LogAra.Client.Models
{
    public sealed class AnalysisHistoryItem
    {
        public required string Id { get; init; }
        public required string FileName { get; init; }
        public DateTimeOffset AnalyzedAtUtc { get; init; }
        public required AnalysisResponseDto Result { get; init; }
    }
}
