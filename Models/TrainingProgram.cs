using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Models
{
    public class TrainingProgram
    {
        [Key]
        public int TrainingProgramId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [Range(1, 1440)] 
        public int Duration { get; set; } 

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 10000)] 
        public decimal Price { get; set; }

        [Required]
        public int GymId { get; set; }
        public Gym Gym { get; set; }
    }
}
