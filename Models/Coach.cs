namespace FitnessCenter.Models
{
    public class Coach
    {
        public int CoachId { get; set; }
        public string? Name { get; set; }
        public string? Expertise { get; set; } 

        public int GymId { get; set; }
        public Gym? Gym { get; set; }
        public ICollection<CoachTrainingProgram> CoachTrainingPrograms { get; set; } = new List<CoachTrainingProgram>();

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
        public ICollection<CoachAvailability> CoachAvailabilities { get; set; } = new List<CoachAvailability>();
        public ICollection<UnavailableSlot> UnavailableSlots { get; set; } = new List<UnavailableSlot>();
    }
}
