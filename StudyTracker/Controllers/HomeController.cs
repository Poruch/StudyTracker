using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Notes.Client.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            // Проверяем, есть ли токен в сессии
            var token = HttpContext.Session.GetString("AccessToken");
            var notesJson = HttpContext.Session.GetString("NotesJson");

            var model = new HomeViewModel
            {
                IsAuthenticated = !string.IsNullOrEmpty(token),
                AccessToken = token,
                NotesJson = notesJson
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CallApi()
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Сначала авторизуйтесь";
                return RedirectToAction("Index");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("NotesApi");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                // Получаем все заметки
                var response = await client.GetAsync("/api/Note/GetAllNotes");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    HttpContext.Session.SetString("NotesJson", content);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Ошибка API: {response.StatusCode} - {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Исключение: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CreateNote()
        {
            var token = HttpContext.Session.GetString("AccessToken");
            if (string.IsNullOrEmpty(token))
            {
                TempData["Error"] = "Сначала авторизуйтесь";
                return RedirectToAction("Index");
            }

            try
            {
                var client = _httpClientFactory.CreateClient("NotesApi");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var newNote = new
                {
                    Title = "Заметка из MVC клиента",
                    Content = $"Создана {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Details = "Тестовая заметка"
                };

                var response = await client.PostAsJsonAsync("/api/Note/CreateNote", newNote);

                if (response.IsSuccessStatusCode)
                {
                    var noteId = await response.Content.ReadFromJsonAsync<Guid>();
                    TempData["Success"] = $"Заметка создана с ID: {noteId}";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Ошибка создания: {response.StatusCode} - {error}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Исключение: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AccessToken");
            HttpContext.Session.Remove("NotesJson");
            TempData["Success"] = "Вы вышли из системы";
            return RedirectToAction("Index");
        }
    }

    public class HomeViewModel
    {
        public bool IsAuthenticated { get; set; }
        public string? AccessToken { get; set; }
        public string? NotesJson { get; set; }
    }
}