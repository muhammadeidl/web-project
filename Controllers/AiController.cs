using FitnessCenter.Controllers;
using FitnessCenter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitnessCenter.Controllers
{
    [Authorize(Roles = "VISITOR")]
    public class AiController : Controller
    {
        private const string ApiUrl = "https://hairstyle-changer.p.rapidapi.com/huoshan/facebody/hairstyle";
        private const string ApiKey = "51b7015574mshd6b5841268bf01ep1d9965jsn1013881d884d";
        private const string ApiHost = "hairstyle-changer.p.rapidapi.com";

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(Ai request)
        {
            if (request.Image == null || request.Image.Length == 0)
            {
                TempData["Error"] = "Please upload a valid image.";
                return RedirectToAction("Index");
            }

            var hairTypes = new List<int> { 401, 403, 201 };
            var editedImages = new List<string>();

            using (var client = new HttpClient())
            {
                foreach (var hairType in hairTypes)
                {
                    var formData = new MultipartFormDataContent();

                    var fileContent = new StreamContent(request.Image.OpenReadStream());
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.Image.ContentType);
                    formData.Add(fileContent, "image_target", request.Image.FileName);

                    formData.Add(new StringContent(hairType.ToString()), "hair_type");

                    var requestMessage = new HttpRequestMessage
                    {
                        Method = HttpMethod.Post,
                        RequestUri = new Uri(ApiUrl),
                        Headers =
                        {
                            { "x-rapidapi-key", ApiKey },
                            { "x-rapidapi-host", ApiHost },
                        },
                        Content = formData
                    };

                    try
                    {
                        var response = await client.SendAsync(requestMessage);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var result = JsonSerializer.Deserialize<JsonElement>(responseBody);

                            if (result.TryGetProperty("data", out var data) &&
                                data.TryGetProperty("image", out var editedImage))
                            {
                                editedImages.Add(editedImage.GetString());
                            }
                            else
                            {
                                TempData["Error"] = "Failed to process the image. Invalid response format.";
                                return RedirectToAction("Index");
                            }
                        }
                        else
                        {
                            TempData["Error"] = $"API Error: {responseBody}";
                            return RedirectToAction("Index");
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        TempData["Error"] = $"Request Failed: {ex.Message}";
                        return RedirectToAction("Index");
                    }
                }
            }

            ViewData["EditedImages"] = editedImages;

            return View("Index");
        }

    }
}
