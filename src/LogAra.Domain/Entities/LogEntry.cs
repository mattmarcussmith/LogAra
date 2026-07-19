namespace LogAra.Domain
{
    public sealed record LogEntry(
        DateTimeOffset? TimestampUtc,
        LogSeverity Severity,
        string Source,
        string Message,
        string RawLine);
}
