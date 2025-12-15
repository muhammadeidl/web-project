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

        [MaxLength(500)]
        public string? ExtraNote { get; set; }

        public IFormFile? Photo { get; set; }

        public string? AiResponse { get; set; }
        public string? PhotoPath { get; set; }

    }
}
