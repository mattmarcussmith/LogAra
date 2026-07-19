namespace LogAra.Contracts.Responses.Analysis
{
    public sealed class RepresentativeEntryDto
    {
        public DateTimeOffset? TimestampUtc { get; init; }
        public required string Severity { get; init; }
        public required string Source { get; init; }
        public required string Message { get; init; }
        public required string RawLine { get; init; }
    }
}