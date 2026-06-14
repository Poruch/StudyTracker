using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Notes.Client.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Захардкоженные настройки
        private const string AuthServerUrl = "https://localhost:7268";
        private const string ClientId = "my-client";
        private const string ClientSecret = "my-secret";
        private const string RedirectUri = "https://localhost:5001/Auth/Callback";
        private const string Scope = "openid profile email api";

        public AuthController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // Генерируем state для защиты от CSRF
            var state = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString("OAuthState", state);

            // Формируем URL авторизации
            var authUrl = $"{AuthServerUrl}/connect/authorize?" +
                          $"response_type=code&" +
                          $"client_id={Uri.EscapeDataString(ClientId)}&" +
                          $"redirect_uri={Uri.EscapeDataString(RedirectUri)}&" +
                          $"scope={Uri.EscapeDataString(Scope)}&" +
                          $"state={state}";

            // Перенаправляем на AuthServer
            return Redirect(authUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Callback(string code, string state)
        {
            // Проверяем state
            var savedState = HttpContext.Session.GetString("OAuthState");
            if (state != savedState)
            {
                TempData["Error"] = "State mismatch! Возможна CSRF-атака.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(code))
            {
                TempData["Error"] = "Authorization code не получен";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Обмениваем code на token
                var token = await ExchangeCodeForToken(code);

                // Сохраняем токен в сессию
                HttpContext.Session.SetString("AccessToken", token);
                HttpContext.Session.Remove("OAuthState");

                TempData["Success"] = "Успешная авторизация!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка получения токена: {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        private async Task<string> ExchangeCodeForToken(string code)
        {
            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{AuthServerUrl}/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "authorization_code",
                    ["code"] = code,
                    ["redirect_uri"] = RedirectUri,
                    ["client_id"] = ClientId,
                    ["client_secret"] = ClientSecret
                })
            };

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"HTTP {response.StatusCode}: {content}");
            }

            var json = JsonDocument.Parse(content);
            return json.RootElement.GetProperty("access_token").GetString()!;
        }
    }
}