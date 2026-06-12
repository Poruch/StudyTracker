using Microsoft.AspNetCore.Mvc;
using StudyTracker.Models;
using System.Diagnostics;

namespace StudyTracker.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var task = new TaskViewModel()
            {
                Id = Guid.NewGuid(),
                Name = "Lab №4",
                Description = "Сделать лабораторную работу номер 4",
                IsCompleted = false
            };

            return View("Piska", task);
        }

        public IActionResult Privacy()
        {
            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
