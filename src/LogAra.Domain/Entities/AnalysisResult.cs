namespace LogAra.Domain
{
    public sealed record AnalysisResult(
        DateTimeOffset GeneratedAtUtc,
        int TotalEntries,
        int ErrorCount,
        int WarningCount,
        int InformationCount,
        IReadOnlyList<IssueInsight> Issues);
}