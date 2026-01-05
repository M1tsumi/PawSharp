using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace PawSharp.API.Tests
{
    public class IntegrationTests
    {
        private readonly HttpClient _httpClient;

        public IntegrationTests()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://discord.com/api/v10/")
            };
        }

        [Fact]
        public async Task Test_GetGateway_Endpoint()
        {
            var response = await _httpClient.GetAsync("gateway");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }

        [Fact]
        public async Task Test_GetBotUser_Endpoint()
        {
            var token = "YOUR_BOT_TOKEN"; // Replace with your bot token
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", token);

            var response = await _httpClient.GetAsync("users/@me");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            Assert.NotNull(content);
        }
    }
}