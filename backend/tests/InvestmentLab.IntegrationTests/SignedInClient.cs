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

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
    }
}
