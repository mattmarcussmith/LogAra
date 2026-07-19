namespace LogAra.Contracts.Responses.Analysis
{
    public sealed class IssueDto
    {
        public required string Fingerprint { get; init; }
        public required string Severity { get; init; }
        public int Occurrences { get; init; }
        public DateTimeOffset? FirstSeenUtc { get; init; }
        public DateTimeOffset? LastSeenUtc { get; init; }
        public required RepresentativeEntryDto RepresentativeEntry { get; init; }
        public required string Explanation { get; set; }
    }
}