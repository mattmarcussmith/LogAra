using System.Text.Json;
using LogAra.Client.Models;
using Microsoft.JSInterop;

namespace LogAra.Client.Services
{
    public sealed class BrowserStorageService(IJSRuntime jsRuntime)
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<ClientStateSnapshot?> LoadStateAsync()
        {
            var json = await jsRuntime.InvokeAsync<string?>("logaraStorage.loadState");
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ClientStateSnapshot>(json, SerializerOptions);
        }

        public async Task SaveStateAsync(ClientStateSnapshot snapshot)
        {
            var json = JsonSerializer.Serialize(snapshot, SerializerOptions);
            await jsRuntime.InvokeVoidAsync("logaraStorage.saveState", json);
        }
    }
}
