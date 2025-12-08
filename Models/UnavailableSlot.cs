namespace FitnessCenter.Models
{
    public class UnavailableSlot
    {
        public int Id { get; set; }
        public int CoachId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
    
}
