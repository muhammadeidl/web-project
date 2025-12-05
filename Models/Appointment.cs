namespace FitnessCenter.Models
{
    public enum AppointmentStatus
    {
        Confirmed,
        Cancelled,
        Done
    }
    public class Appointment
    {
        private DateTime appointmentDate;
        public int AppointmentId { get; set; }
        public DateTime AppointmentDate
        {
            get => appointmentDate;
            set => appointmentDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        public AppointmentStatus Status { get; set; } 

        public int TrainingProgramId { get; set; }
        public TrainingProgram ?TrainingProgram { get; set; }

        public int CoachId { get; set; }
        public Coach ?Coach { get; set; }

        public string ?UserId { get; set; }
        public User ?User { get; set; }
    }
}
