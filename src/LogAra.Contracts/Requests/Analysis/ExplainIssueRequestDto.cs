namespace LogAra.Contracts.Requests.Analysis
{
    public sealed class ExplainIssueRequestDto
    {
        public required string Fingerprint { get; init; }
        public required string Severity { get; init; }
        public required string Source { get; init; }
        public required string Message { get; init; }
        public int Occurrences { get; init; }
    }
}
