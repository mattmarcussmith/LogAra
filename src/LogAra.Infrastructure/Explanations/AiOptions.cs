namespace LogAra.Infrastructure.Explanations
{
    public sealed class AiOptions
    {
        public const string SectionName = "AI";

        public bool Enabled { get; init; }
        public string Provider { get; init; } = "Pollinations";
        public string Model { get; init; } = "openai-fast";
        public string Endpoint { get; init; } = "https://text.pollinations.ai/openai";
        public int TimeoutSeconds { get; init; } = 20;
        public int MaxOutputTokens { get; init; } = 500;
    }
}
