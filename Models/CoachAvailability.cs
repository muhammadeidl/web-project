using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class CoachAvailability
    {
        public int CoachAvailabilityId { get; set; }

        [Required]
        public GymDay DayOfWeek { get; set; }

        public bool IsClosed { get; set; } = false;

        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }

        public int CoachId { get; set; }
        public Coach? Coach { get; set; }
    }
}
