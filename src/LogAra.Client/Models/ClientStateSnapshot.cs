namespace LogAra.Client.Models
{
    public sealed class ClientStateSnapshot
    {
        public string Theme { get; init; } = "light";
        public List<AnalysisHistoryItem> History { get; init; } = [];
        public Dictionary<string, string> ExplanationCache { get; init; } = new(StringComparer.Ordinal);
    }
}
