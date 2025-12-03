using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class Gym
    {
        public int GymId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Location { get; set; }

        public ICollection<Coach> Coaches { get; set; } = new List<Coach>();
        public ICollection<TrainingProgram> TrainingPrograms { get; set; } = new List<TrainingProgram>();
        public List<GymWorkingHours> WorkingHours { get; set; } = new List<GymWorkingHours>(); /**************************************/
    }
}
