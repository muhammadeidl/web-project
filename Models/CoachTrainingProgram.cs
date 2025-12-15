namespace FitnessCenter.Models
{
    public class CoachTrainingProgram
    {
        public int CoachId { get; set; }
        public Coach Coach { get; set; } = null!;

        public int TrainingProgramId { get; set; }
        public TrainingProgram TrainingProgram { get; set; } = null!;
    }
}
