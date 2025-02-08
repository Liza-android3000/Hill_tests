using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace Hill.Tests
{
    [TestFixture]
    public class HillCipherApiTests
    {
        private HttpClient? _client;
        private string? _token;

        [SetUp]
        public async Task Setup()
        {
            var factory = new HillApplication();
            _client = factory.CreateClient();
            _token = await GetTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        }

        [Test]
        public async Task AddText_ShouldReturnSuccess()
        {
            var content = new StringContent(JsonSerializer.Serialize(new { Content = "Test text" }), Encoding.UTF8, "application/json");
            var response = await _client!.PostAsync("/api/hillcipher/texts", content);
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            responseString.Should().Contain("Test text");
        }

        [Test]
        public async Task UpdateText_ShouldReturnSuccess()
        {
            var addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "Initial text" }), Encoding.UTF8, "application/json");
            var addTextResponse = await _client!.PostAsync("/api/hillcipher/texts", addTextContent);
            addTextResponse.EnsureSuccessStatusCode();
            var addTextResult = await addTextResponse.Content.ReadAsStringAsync();
            var textId = JsonSerializer.Deserialize<JsonElement>(addTextResult).GetProperty("id").GetInt32();

            var updateTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "Updated text" }), Encoding.UTF8, "application/json");
            var updateResponse = await _client.PatchAsync($"/api/hillcipher/texts/{textId}", updateTextContent);

            updateResponse.EnsureSuccessStatusCode();
            var updateResult = await updateResponse.Content.ReadAsStringAsync();
            updateResult.Should().Contain("Updated text");
        }

        [Test]
        public async Task DeleteText_ShouldReturnSuccess()
        {
            var addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "Text to delete" }), Encoding.UTF8, "application/json");
            var addTextResponse = await _client!.PostAsync("/api/hillcipher/texts", addTextContent);
            addTextResponse.EnsureSuccessStatusCode();
            var addTextResult = await addTextResponse.Content.ReadAsStringAsync();
            var textId = JsonSerializer.Deserialize<JsonElement>(addTextResult).GetProperty("id").GetInt32();

            var deleteResponse = await _client.DeleteAsync($"/api/hillcipher/texts/{textId}");
            deleteResponse.EnsureSuccessStatusCode();
        }

        [Test]
        public async Task EncryptText_ShouldReturnEncryptedText()
        {
            var addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "HELLO" }), Encoding.UTF8, "application/json");
            var addTextResponse = await _client!.PostAsync("/api/hillcipher/texts", addTextContent);
            addTextResponse.EnsureSuccessStatusCode();
            var addTextResult = await addTextResponse.Content.ReadAsStringAsync();
            var textId = JsonSerializer.Deserialize<JsonElement>(addTextResult).GetProperty("id").GetInt32();

            var encryptContent = new StringContent(JsonSerializer.Serialize(new { Key = "5,8,3,7" }), Encoding.UTF8, "application/json");
            var encryptResponse = await _client.PostAsync($"/api/hillcipher/texts/{textId}/encrypt", encryptContent);

            encryptResponse.EnsureSuccessStatusCode();
            var encryptResult = await encryptResponse.Content.ReadAsStringAsync();
            var jsonResult = JsonSerializer.Deserialize<JsonElement>(encryptResult);
            jsonResult.GetProperty("content").GetString().Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task DecryptText_ShouldReturnDecryptedText()
        {
            var addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "HELLO" }), Encoding.UTF8, "application/json");
            var addTextResponse = await _client!.PostAsync("/api/hillcipher/texts", addTextContent);
            addTextResponse.EnsureSuccessStatusCode();
            var addTextResult = await addTextResponse.Content.ReadAsStringAsync();
            var textId = JsonSerializer.Deserialize<JsonElement>(addTextResult).GetProperty("id").GetInt32();

            var encryptContent = new StringContent(JsonSerializer.Serialize(new { Key = "5,8,3,7" }), Encoding.UTF8, "application/json");
            await _client.PostAsync($"/api/hillcipher/texts/{textId}/encrypt", encryptContent);

            var decryptContent = new StringContent(JsonSerializer.Serialize(new { Key = "5,8,3,7" }), Encoding.UTF8, "application/json");
            var decryptResponse = await _client.PostAsync($"/api/hillcipher/texts/{textId}/decrypt", decryptContent);

            decryptResponse.EnsureSuccessStatusCode();
            var decryptResult = await decryptResponse.Content.ReadAsStringAsync();
            decryptResult.Should().Contain("HELLO");
        }

        [Test]
        public async Task GetText_ShouldReturnText()
        {
            var addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "Test text" }), Encoding.UTF8, "application/json");
            var addTextResponse = await _client!.PostAsync("/api/hillcipher/texts", addTextContent);
            addTextResponse.EnsureSuccessStatusCode();
            var addTextResult = await addTextResponse.Content.ReadAsStringAsync();
            var textId = JsonSerializer.Deserialize<JsonElement>(addTextResult).GetProperty("id").GetInt32();

            var getResponse = await _client.GetAsync($"/api/hillcipher/texts/{textId}");
            getResponse.EnsureSuccessStatusCode();
            var getResult = await getResponse.Content.ReadAsStringAsync();
            getResult.Should().Contain("Test text");
        }

        [Test]
        public async Task GetAllTexts_ShouldReturnListOfTexts()
        {
            var addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "Text 1" }), Encoding.UTF8, "application/json");
            await _client!.PostAsync("/api/hillcipher/texts", addTextContent);

            addTextContent = new StringContent(JsonSerializer.Serialize(new { Content = "Text 2" }), Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/hillcipher/texts", addTextContent);

            var getAllResponse = await _client.GetAsync("/api/hillcipher/texts");
            getAllResponse.EnsureSuccessStatusCode();
            var getAllResult = await getAllResponse.Content.ReadAsStringAsync();
            getAllResult.Should().Contain("Text 1").And.Contain("Text 2");
        }

        private async Task<string> GetTokenAsync()
        {
            var username = $"testuser_{Guid.NewGuid()}";
            var password = "testpassword";

            var registerContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var registerResponse = await _client!.PostAsync("/api/users/register", registerContent);
            registerResponse.EnsureSuccessStatusCode();

            var loginContent = new StringContent(JsonSerializer.Serialize(new { Username = username, Password = password }), Encoding.UTF8, "application/json");
            var loginResponse = await _client.PostAsync("/api/users/login", loginContent);
            loginResponse.EnsureSuccessStatusCode();

            var loginResult = await loginResponse.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<JsonElement>(loginResult).GetProperty("token").GetString();

            return token!;
        }
    }
}