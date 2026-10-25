extern alias api;

using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvestmentLab.IntegrationTests;

internal static class SignedInClient
{
    public const string Password = "correct horse battery";

    extension(WebApplicationFactory<api::Program> factory)
    {
        /// <summary>A client holding the auth cookie of a freshly registered user.</summary>
        public async Task<HttpClient> CreateSignedInClientAsync(CancellationToken cancellationToken, string? email = null)
        {
            var client = factory.CreateClient();
            email ??= $"investor-{Guid.NewGuid():N}@example.com";

            await EnsureSuccessAsync(await client.PostAsJsonAsync("/api/identity/register", new { email, password = Password }, cancellationToken));
            await EnsureSuccessAsync(await client.PostAsJsonAsync("/api/identity/login", new { email, password = Password }, cancellationToken));
            return client;
        }
    }

    extension(HttpClient client)
    {
        /// <summary>
        /// The standard strategies every new user gets at registration; they arrive through the outbox, so this waits
        /// for them. <paramref name="name"/> reads a strategy's name: the result is ordered by it ("EU…", "US…").
        /// </summary>
        public async Task<List<T>> DefaultStrategiesAsync<T>(Func<T, string> name, CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 60; attempt++)
            {
                var strategies = await client.GetFromJsonAsync<List<T>>("/api/strategies", cancellationToken) ?? [];
                if (strategies.Count >= 2)
                    return [.. strategies.OrderBy(name, StringComparer.Ordinal)];
                await Task.Delay(250, cancellationToken);
            }
            throw new TimeoutException("Default strategies were not created after registration.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}
