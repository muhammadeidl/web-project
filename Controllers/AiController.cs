using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "visitor")]
    public class AiController : Controller
    {
        private readonly IConfiguration _configuration;

        public AiController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new AiModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(AiModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Lütfen tüm alanları doğru şekilde doldurduğunuzdan emin olun.");
                return View(model);
            }

            try
            {
                model.AiResponse = await GeneratePlan(model);
            }
            catch (Exception ex)
            {
                model.AiResponse = "Bağlantı hatası oluştu: " + ex.Message;
            }

            return View(model);
        }

        private async Task<string> GeneratePlan(AiModel model)
        {
            string apiKey = _configuration["GeminiSettings:ApiKey"];

            if (string.IsNullOrEmpty(apiKey))
                return "Hata: API Anahtarı yapılandırma dosyasında bulunamadı!!";

            using var client = new HttpClient();

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";

            var extra = string.IsNullOrWhiteSpace(model.ExtraNote) ? "Yok" : model.ExtraNote;

            string prompt =
                "Sen profesyonel bir fitness koçu ve beslenme uzmanısın.\n" +
                "Kullanıcı bilgilerine göre kişiye özel antrenman + beslenme planı oluştur.\n\n" +
                $"Yaş: {model.Age}\n" +
                $"Kilo: {model.Weight} kg\n" +
                $"Boy: {model.Height} cm\n" +
                $"Cinsiyet: {model.Gender}\n" +
                $"Hedef: {model.Goal}\n" +
                $"Ek Bilgi: {extra}\n\n" +
                "Kurallar:\n" +
                "1) Yanıt tamamen Türkçe olsun.\n" +
                "2) Başlıklar ve maddeler halinde yaz.\n" +
                "3) Haftalık antrenman planı + günlük beslenme örneği ver.\n" +
                "4) Eğer fotoğraf varsa: vücut tipi/denge/hatalar hakkında genel yorum yap (tıbbi teşhis yapma).\n";

            var parts = new List<object>();

            parts.Add(new { text = prompt });

            if (model.Photo != null && model.Photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await model.Photo.CopyToAsync(ms);
                var bytes = ms.ToArray();
                var base64 = Convert.ToBase64String(bytes);

                var mimeType = string.IsNullOrWhiteSpace(model.Photo.ContentType)
                    ? "image/jpeg"
                    : model.Photo.ContentType;

                parts.Add(new
                {
                    inlineData = new
                    {
                        mimeType = mimeType,
                        data = base64
                    }
                });
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = parts
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            string responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return "API Hatası: " + responseText;

            using var doc = JsonDocument.Parse(responseText);

            if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                return candidates[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString() ?? "Yapay zekadan geçerli bir yanıt alınamadı.";
            }

            return "Yapay zekadan geçerli bir yanıt alınamadı.";
        }
    }
}
 public async Task<byte[]> GenerateAfterImageAsync(byte[] inputImageBytes, string prompt)
    {
        using var form = new MultipartFormDataContent();

        // model
        form.Add(new StringContent("gpt-image-1"), "model");

        // prompt
        form.Add(new StringContent(prompt), "prompt");

        // input image (required)
        var imageContent = new ByteArrayContent(inputImageBytes);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        form.Add(imageContent, "image", "before.jpg");

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/edits");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
        req.Content = form;

        using var res = await _http.SendAsync(req);
        var json = await res.Content.ReadAsStringAsync();

        if (!res.IsSuccessStatusCode)
            throw new Exception($"OpenAI error: {res.StatusCode} - {json}");

        // response typically returns base64 image data in JSON
        // parse: data[0].b64_json
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var b64 = doc.RootElement.GetProperty("data")[0].GetProperty("b64_json").GetString();
        return Convert.FromBase64String(b64!);
    }
}
