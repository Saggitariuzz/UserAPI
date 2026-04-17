using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UserAPI.DTOs;
using System.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using UserAPI.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text;

namespace UserAPI.Tests
{
    public class UsersAuthTests : IClassFixture<WebApplicationFactory<Program>>
    {

        private readonly HttpClient client;

        private readonly WebApplicationFactory<Program> _factory;

        public UsersAuthTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAll_Without_Token()
        {
            var response = await client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_And_Access_Methods_With_Token()
        {
            var login = $"testuser{Guid.NewGuid().ToString()}";
            var password = "12345678";
            string? userId = null;
            string? token = null;
            try
            {
                var createDto = new UserCreateDto { Login = login, Password = password };
                var createResponse = await client.PostAsJsonAsync("/api/users", createDto);
                createResponse.EnsureSuccessStatusCode();
                var createdUser = await createResponse.Content.ReadFromJsonAsync<UserReadDto>();
                userId = createdUser.Id;
                var loginDto = new UserLoginDto { Login = login, Password = password };
                var loginResponse = await client.PostAsJsonAsync("/api/users/login", loginDto);
                loginResponse.EnsureSuccessStatusCode();
                var jsonResponse = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
                token = jsonResponse.GetProperty("token").GetString();
                var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var getResponse = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            }
            finally
            {
                if(userId != null && token != null)
                {
                    var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"api/users/{userId}");
                    deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    await client.SendAsync(deleteReq);
                }
            }
        }

        [Fact]
        public async Task Try_Get_Access_With_Invalid_Token()
        {
            var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjMifQ.InvalidSignatureHdfhsdfh";
            var request = new HttpRequestMessage(HttpMethod.Get, "api/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Try_Get_Access_With_Expired_Token()
        {
            var authOptions = _factory.Services.GetRequiredService<IOptions<AuthOptions>>().Value;
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "test") }),
                Expires = DateTime.UtcNow.AddHours(-1),
                NotBefore = DateTime.UtcNow.AddHours(-2),
                Issuer = authOptions.Issuer,
                Audience = authOptions.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authOptions.Key)), SecurityAlgorithms.HmacSha256)
            };
            var expiredToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
            var request = new HttpRequestMessage(HttpMethod.Get, "api/users");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Try_Get_Access_After_Logout()
        {
            var login = $"testuser{Guid.NewGuid().ToString()}";
            var password = "12345678";
            string? userId = null;
            try
            {
                var createDto = new UserCreateDto { Login = login, Password = password };
                var createResponse = await client.PostAsJsonAsync("/api/users", createDto);
                createResponse.EnsureSuccessStatusCode();
                var createdUser = await createResponse.Content.ReadFromJsonAsync<UserReadDto>();
                userId = createdUser.Id;
                var loginDto = new UserLoginDto { Login = login, Password = password };
                var loginResponse = await client.PostAsJsonAsync("/api/users/login", loginDto);
                loginResponse.EnsureSuccessStatusCode();
                var jsonResponse = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
                var token = jsonResponse.GetProperty("token").GetString();
                var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "api/users/logout");
                logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var logoutResponse = await client.SendAsync(logoutRequest);
                logoutResponse.EnsureSuccessStatusCode();
                var requestAfterLogout = new HttpRequestMessage(HttpMethod.Get, "api/users");
                requestAfterLogout.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var responceAfterLogout = await client.SendAsync(requestAfterLogout);
                Assert.Equal(HttpStatusCode.Unauthorized, responceAfterLogout.StatusCode);
            }
            finally
            {
                var loginDto = new UserLoginDto { Login = login, Password = password };
                var loginResponse = await client.PostAsJsonAsync("/api/users/login", loginDto);
                if (loginResponse.IsSuccessStatusCode)
                {
                    var jsonReLogin = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
                    var newToken = jsonReLogin.GetProperty("token").GetString();
                    var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"api/users/{userId}");
                    deleteReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                    await client.SendAsync(deleteReq);
                }
            }
        }
    }
}
