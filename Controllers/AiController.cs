using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "visitor")]
    public class AiController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AiController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
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
            if (!ModelState.IsValid) return View(model);

            // ======================
            // A) ALWAYS: generate text plan
            // ======================
            try
            {
                string geminiKey = _configuration["GeminiSettings:ApiKey"];
                string planPrompt = GenerateTextPrompt(model);
                model.AiResponse = await GetGeminiResponse(planPrompt, geminiKey);
            }
            catch (Exception ex)
            {
                model.AiResponse = "Plan oluşturulurken hata oluştu: " + ex.Message;
                // Even if plan fails, still return view
                return View(model);
            }

            // ======================
            // B) OPTIONAL: if photo exists, generate before+after
            // ======================
            if (model.Photo == null || model.Photo.Length == 0)
            {
                // Case (1): No photo -> only plan (text)
                return View(model);
            }

            // Size limit (optional but recommended)
            if (model.Photo.Length > 4_000_000)
            {
                model.ImageError = "Fotoğraf çok büyük. Lütfen 4MB altında bir fotoğraf yükleyin.";
                return View(model);
            }

            try
            {
                // Read bytes
                byte[] photoBytes;
                using (var ms = new MemoryStream())
                {
                    await model.Photo.CopyToAsync(ms);
                    photoBytes = ms.ToArray();
                }

                // Show BEFORE
                model.UploadedImageUrl = $"data:{model.Photo.ContentType};base64,{Convert.ToBase64String(photoBytes)}";

                // Generate AFTER via OpenAI edits
                string openAiKey = _configuration["OpenAISettings:ApiKey"];
                string afterPrompt = GenerateAfterEditPrompt(model);

                string? afterB64 = await GetOpenAiAfterImageEdit(
                    imageBytes: photoBytes,
                    imageFileName: string.IsNullOrWhiteSpace(model.Photo.FileName) ? "before.jpg" : model.Photo.FileName,
                    imageContentType: model.Photo.ContentType,
                    prompt: afterPrompt,
                    apiKey: openAiKey
                );

                if (!string.IsNullOrWhiteSpace(afterB64))
                {
                    model.GeneratedImageUrl = $"data:image/png;base64,{afterB64}";
                }
                else
                {
                    model.ImageError = "After görseli üretilemedi. Lütfen tekrar deneyin.";
                }
            }
            catch (Exception ex)
            {
                // Do NOT overwrite plan. Just show image error.
                model.ImageError = ex.Message;
            }

            return View(model);
        }

        // ---------- Prompts ----------

        private string GenerateTextPrompt(AiModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Sen uzman bir spor ve beslenme koçusun.");
            sb.AppendLine("Aşağıdaki bilgilere sahip bir kullanıcı için detaylı bir antrenman ve beslenme programı hazırla.");
            sb.AppendLine($"Yaş: {model.Age}, Kilo: {model.Weight}kg, Boy: {model.Height}cm, Cinsiyet: {model.Gender}");
            sb.AppendLine($"Hedef: {model.Goal}");
            sb.AppendLine($"Süre: {model.DurationMonths} ay");

            if (!string.IsNullOrWhiteSpace(model.ExtraNote))
                sb.AppendLine($"Ek Notlar: {model.ExtraNote}");

            sb.AppendLine("\nLütfen yanıtı markdown formatında, başlıklar kullanarak, motive edici bir tonla ver.");
            return sb.ToString();
        }

        private string GenerateAfterEditPrompt(AiModel model)
        {
            string goal = string.IsNullOrWhiteSpace(model.Goal) ? "fit and healthy body" : model.Goal.Trim();
            int months = model.DurationMonths > 0 ? model.DurationMonths : 6;

            return $@"
Transform the person's body to reflect realistic progress after {months} months toward the goal: {goal}.

STRICT RULES:
- Keep the SAME person (identity), SAME face, SAME hairstyle, SAME clothes, SAME pose, SAME background, SAME lighting.
- Only change body shape and muscle/fat composition according to the goal and timeframe.
- Do NOT change facial features, gender, age, skin tone.
- Do NOT replace the person with someone else.
This is a health and wellness transformation showing realistic fitness progress.

Keep the same person's face and identity.
Keep appropriate athletic sportswear.
Keep the same background setting.
Keep the same pose and camera angle.
Keep professional fitness photography style.

This is a safe-for-work professional fitness transformation image.
Photorealistic, natural results.
".Trim();
        }

        // ---------- Gemini ----------
        private async Task<string> GetGeminiResponse(string prompt, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                return "Gemini API Key not found in configuration.";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}",
                content
            );

            if (!response.IsSuccessStatusCode)
                return $"Error communicating with Gemini API. Status: {response.StatusCode}";

            var jsonResponse = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonResponse);
            return data?.candidates[0]?.content?.parts[0]?.text ?? "No response from AI.";
        }

        // ---------- OpenAI Image Edit ----------
        private async Task<string?> GetOpenAiAfterImageEdit(
            byte[] imageBytes,
            string imageFileName,
            string? imageContentType,
            string prompt,
            string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("OpenAI API Key not found in configuration.");

            // Normalize content-type
            string ct = (imageContentType ?? "image/jpeg").ToLowerInvariant();
            if (ct != "image/jpeg" && ct != "image/png" && ct != "image/webp")
                ct = "image/jpeg";

            using var form = new MultipartFormDataContent();

            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(ct);
            form.Add(imageContent, "image", imageFileName);

            form.Add(new StringContent(prompt), "prompt");
            form.Add(new StringContent("gpt-image-1"), "model");
            form.Add(new StringContent("high"), "input_fidelity");
            form.Add(new StringContent("1024x1024"), "size");
            form.Add(new StringContent("1"), "n");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/edits");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = form;

            using var response = await _httpClient.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception(json);

            dynamic data = JsonConvert.DeserializeObject(json);
            return data?.data[0]?.b64_json;
        }
    }
}
