using System;
using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }

        [Required]
        public DateTime AppointmentDate { get; set; }

        // ✅ Tek alan: onay + iptal + tamamlandı hepsi burada
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        // FK
        public int TrainingProgramId { get; set; }
        public int CoachId { get; set; }
        public string? UserId { get; set; }

        // Navigation
        public TrainingProgram? TrainingProgram { get; set; }
        public Coach? Coach { get; set; }
        public User? User { get; set; }
    }
}
