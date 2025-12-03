using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Data
{
    public class SporSalonuDbContext : IdentityDbContext<User>
    {
        public SporSalonuDbContext(DbContextOptions<SporSalonuDbContext> options)
            : base(options) { }

        public DbSet<Gym> Gyms { get; set; }
        public DbSet<Coach> Coaches { get; set; }
        public DbSet<TrainingProgram> TrainingPrograms { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<CoachAvailability> CoachAvailability { get; set; }
        public DbSet<GymWorkingHours> GymWorkingHours { get; set; }
        public DbSet<UnavailableSlot> UnavailableSlots { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // prevent double cascade
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.TrainingProgram)
                .WithMany()
                .HasForeignKey(a => a.TrainingProgramId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany(u => u.Appointments)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TrainingProgram>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);
        }
    }
}
