using LogAra.Application.Abstractions;
using LogAra.Application.Services;
using LogAra.Infrastructure.Explanations;
using LogAra.Infrastructure.Parsing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ILogAnalysisService, LogAnalysisService>();
builder.Services.AddScoped<IAnalysisApiService, AnalysisApiService>();
builder.Services.AddScoped<ILogParser, SimpleLogParser>();
builder.Services.AddOptions<AiOptions>()
    .Bind(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.AddHttpClient<IExplanationProvider, PollinationsExplanationProvider>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientDev", policy =>
    {
        policy
            .WithOrigins("https://localhost:7273", "http://localhost:5033")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["urls"];
var hasHttpsEndpoint = configuredUrls?
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) == true;

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.").ExecuteAsync(context);
    });
});

if (!app.Environment.IsDevelopment() || hasHttpsEndpoint)
{
    app.UseHttpsRedirection();
}

app.UseCors("ClientDev");
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
