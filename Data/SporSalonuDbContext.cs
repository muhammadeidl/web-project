using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;


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

            // Seed default roles: admin and visitor
            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole
                {
                    Id = "b1a6f6a2-4e2d-4e2a-9f1a-111111111111",
                    Name = "admin",
                    NormalizedName = "ADMIN",
                    ConcurrencyStamp = "b1a6f6a2-4e2d-4e2a-9f1a-111111111111"
                },
                new IdentityRole
                {
                    Id = "c2b7f7b3-5f3e-5f3b-0a2b-222222222222",
                    Name = "visitor",
                    NormalizedName = "VISITOR",
                    ConcurrencyStamp = "c2b7f7b3-5f3e-5f3b-0a2b-222222222222"
                }
            );
        }
    }
}
