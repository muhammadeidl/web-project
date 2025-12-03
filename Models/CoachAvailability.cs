using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class CoachAvailability
    {
        public int CoachAvailabilityId { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int CoachId { get; set; }
        public Coach Coach { get; set; }
    }
}
