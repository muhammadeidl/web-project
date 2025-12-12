using FitnessCenter.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Controllers
{
    public class AiController : Controller
    {
        private readonly string apiKey = "yazacagim-sonra"; 
        [HttpGet]
        public IActionResult Index()
        {
            return View(new AiModel());
        }

        [HttpPost]
        public async Task<IActionResult> Index(AiModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "doldurcagim");
                return View(model);
            }

            try
            {
                model.AiResponse = await GeneratePlan(model);
            }
            catch (Exception ex)
            {
                model.AiResponse = "yanlis durumunda: " + ex.Message;
            }

            return View(model);
        }

        private async Task<string> GeneratePlan(AiModel model)
        {
            using var client = new HttpClient();

            // ملف AiController.cs
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            string prompt =
                $"yas: {model.Age}\n" +
                $"kilo: {model.Weight} كجم\n" +
                $"boy: {model.Height} سم\n" +
                $"gender: {model.Gender}\n" +
                $"hedef: {model.Goal}\n\n" +
                "give me plane";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);

            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return " yanlis API buradan: " + responseText;

            var doc = JsonDocument.Parse(responseText);

            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString()!;
        }
    }
}
