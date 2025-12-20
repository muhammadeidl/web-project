using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class AiModel
    {
        [Range(1, 120)]
        public int Age { get; set; }

        [Range(1, 500)]
        public int Weight { get; set; }

        [Range(50, 250)]
        public int Height { get; set; }

        [Required]
        public string Gender { get; set; } = "Erkek";

        [Required]
        [MaxLength(200)]
        public string Goal { get; set; } = "";

        [MaxLength(500)]
        public string? ExtraNote { get; set; }

        // Optional photo
        public IFormFile? Photo { get; set; }

        // Plan
        public string? AiResponse { get; set; }

        // Duration (months)
        [Range(1, 24)]
        public int DurationMonths { get; set; } = 6;

        // BEFORE + AFTER for UI
        public string? UploadedImageUrl { get; set; }
        public string? GeneratedImageUrl { get; set; }

        // For showing image errors nicely
        public string? ImageError { get; set; }
    }
}
