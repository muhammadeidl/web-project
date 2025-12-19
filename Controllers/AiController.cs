using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace FitnessCenter.Controllers
{
    // ✅ Ensure that the role here matches the one in the database (usually "member" rather than "visitor")
    [Authorize(Roles = "member")]
    public class AiController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        // ✅ Inject IConfiguration to access keys within appsettings.json
        public AiController(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(120); // Increase wait time for image generation
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

            try
            {
                // 1. Fetch Gemini key from the correct path in appsettings.json
                string geminiKey = _configuration["GeminiSettings:ApiKey"]; // ✅ Corrected path

                string textPrompt = GenerateTextPrompt(model);
                model.AiResponse = await GetGeminiResponse(textPrompt, geminiKey);

                // 2. Fetch Stability key from the correct path and generate the image
                string stabilityKey = _configuration["StabilitySettings:ApiKey"]; // ✅ Corrected path

                string imagePrompt = GenerateImagePrompt(model);
                string base64Image = await GetStabilityAiImage(imagePrompt, stabilityKey);

                if (!string.IsNullOrEmpty(base64Image))
                {
                    // Convert Base64 to a data URI format to display it directly in an <img> tag
                    model.GeneratedImageUrl = $"data:image/png;base64,{base64Image}";
                }
            }
            catch (Exception ex)
            {
                model.AiResponse = "An error occurred: " + ex.Message;
            }

            return View(model);
        }

        // ==================================================================================
        // Helper Methods
        // ==================================================================================

        // Method to generate the text prompt for the AI coach
        private string GenerateTextPrompt(AiModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("You are an expert fitness and nutrition coach.");
            sb.AppendLine("Prepare a detailed training and nutrition program for a user with the following details:");
            sb.AppendLine($"Age: {model.Age}, Weight: {model.Weight}kg, Height: {model.Height}cm, Gender: {model.Gender}");
            sb.AppendLine($"Goal: {model.Goal}");
            if (!string.IsNullOrEmpty(model.ExtraNote))
            {
                sb.AppendLine($"Additional Notes: {model.ExtraNote}");
            }
            sb.AppendLine("\nPlease provide the response in Turkish, using Markdown headers and a motivating tone.");
            
            // Note: This version of the Gemini API relies on text prompts for the plan generation.
            return sb.ToString();
        }

        // ✅ New Method: Generate a descriptive prompt for the Stability AI image generation
        private string GenerateImagePrompt(AiModel model)
        {
            // Crafting an English description for the image model to understand.
            // Requesting a realistic photo of a person of the same gender and age who has achieved the specified goal.
            string genderTerm = model.Gender == "Kadın" ? "female" : "male";

            var sb = new StringBuilder();
            // Start with a high-quality general description to ensure visual fidelity
            sb.Append("A high-quality, realistic full-body photograph of a ");
            sb.Append($"{model.Age}-year-old {genderTerm}, ");
            
            // Add physique details based on the user's specific fitness goal
            sb.Append($"with an athletic, fit physique achieved after pursuing the goal of '{model.Goal}'. ");
            
            // Add environment and clothing details to increase realism
            sb.Append("Wearing modern workout sportswear, standing confidently in a well-lit, modern gym environment. ");
            sb.Append("Cinematic lighting, highly detailed, 8k resolution.");

            return sb.ToString();
        }

        // Method to communicate with Gemini API (updated to accept the key as a parameter)
        private async Task<string> GetGeminiResponse(string prompt, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return "Gemini API Key not found in configuration.";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");
            
            // Using the gemini-2.5-flash model endpoint
            var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}", content);
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(jsonResponse);
                return data?.candidates[0]?.content?.parts[0]?.text ?? "No response from AI.";
            }

            return $"Error communicating with Gemini API. Status: {response.StatusCode}";
        }

        // ✅ New Method: Connect to Stability AI to generate the body simulation image
        private async Task<string?> GetStabilityAiImage(string prompt, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return null;

            // Request configuration for the SDXL model
            var requestBody = new
            {
                text_prompts = new[]
                {
                    new { text = prompt, weight = 1 }
                },
                cfg_scale = 7,     // Prompt fidelity (7 is a standard balanced value)
                height = 1024,     // Image dimensions
                width = 1024,
                steps = 30,        // Number of inference steps (30 provides good quality)
                samples = 1        // Number of images to generate
            };

            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            // Setup required API headers
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.stability.ai/v1/generation/stable-diffusion-xl-1024-v1-0/text-to-image");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(jsonResponse);
                
                // The response contains an "artifacts" array; we retrieve the first generated image
                // The image data is returned as a Base64 string
                string base64String = data?.artifacts[0]?.base64;
                return base64String;
            }
            else
            {
                // Log the error to the console for debugging purposes
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Stability AI Error: {error}");
                return null; // Return null if the generation fails
            }
        }
    }
}
