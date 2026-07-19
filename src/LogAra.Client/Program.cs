using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LogAra.Client;
using LogAra.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var configuredApiBaseUrl = builder.Configuration["ApiBaseUrl"];
Uri apiBaseAddress;
var isDevelopment = string.Equals(builder.HostEnvironment.Environment, "Development", StringComparison.OrdinalIgnoreCase);

if (isDevelopment && (string.IsNullOrWhiteSpace(configuredApiBaseUrl) || configuredApiBaseUrl == "/" || string.Equals(configuredApiBaseUrl, "auto", StringComparison.OrdinalIgnoreCase)))
{
    var clientIsHttps = builder.HostEnvironment.BaseAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    apiBaseAddress = new Uri(clientIsHttps ? "https://localhost:7191/" : "http://localhost:5205/");
}
else if (string.IsNullOrWhiteSpace(configuredApiBaseUrl) || configuredApiBaseUrl == "/")
{
    apiBaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
}
else if (Uri.TryCreate(configuredApiBaseUrl, UriKind.Absolute, out var absoluteUri))
{
    apiBaseAddress = absoluteUri;
}
else
{
    apiBaseAddress = new Uri(new Uri(builder.HostEnvironment.BaseAddress), configuredApiBaseUrl);
}

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseAddress });
builder.Services.AddScoped<AnalysisApiClient>();
builder.Services.AddScoped<BrowserStorageService>();

await builder.Build().RunAsync();
