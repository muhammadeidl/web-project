using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class AiModel
    {
        [Range(1, 120)]
        public int Age { get; set; }

        [Range(1, 500)]
        public int Weight { get; set; } // kg

        [Range(50, 250)]
        public int Height { get; set; } // cm

        [Required]
        public string Gender { get; set; } = "Erkek";

        [Required]
        [MaxLength(200)]
        public string Goal { get; set; } = "";

        //  New: Additional Note
        [MaxLength(500)]
        public string? ExtraNote { get; set; }

        // New: Optional Photo
        public IFormFile? Photo { get; set; }

        // AI Generation Result (Text Plan)
        public string? AiResponse { get; set; }
        
        public string? PhotoPath { get; set; }

        //  Add this new property 
        // This variable will hold the generated image data in Base64 format to be displayed directly in the view.
        public string? GeneratedImageUrl { get; set; }
    }
}
