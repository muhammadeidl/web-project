namespace FitnessCenter.Models
{
    public enum GymDay
    {
        Sunday,
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday
    }

    public class GymWorkingHours
    {
        public int Id { get; set; }
        public GymDay DayOfWeek { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public bool IsClosed { get; set; }
        public int GymId { get; set; }
        public Gym ?Gym { get; set; }



     
       
    }
}
