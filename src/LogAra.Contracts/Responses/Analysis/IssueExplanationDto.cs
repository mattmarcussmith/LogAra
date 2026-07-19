namespace LogAra.Contracts.Responses.Analysis
{
    public sealed class IssueExplanationDto
    {
        public required string Summary { get; init; }
        public IReadOnlyList<string> PossibleCauses { get; init; } = [];
        public IReadOnlyList<string> RecommendedNextSteps { get; init; } = [];
        public required string RawText { get; init; }
    }
}
