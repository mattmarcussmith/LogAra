namespace LogAra.Domain
{
    public sealed record IssueInsight(
        string Fingerprint,
        LogSeverity Severity,
        int Occurrences,
        DateTimeOffset? FirstSeenUtc,
        DateTimeOffset? LastSeenUtc,
        LogEntry RepresentativeEntry,
        string Explanation);
}