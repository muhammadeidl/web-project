using FitnessCenter.Models;
using Microsoft.AspNetCore.Identity;
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

        // Ara tablo
        public DbSet<CoachTrainingProgram> CoachTrainingPrograms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ============================================================
            // Appointment ilişkileri
            // ============================================================
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

            // Price precision
            modelBuilder.Entity<TrainingProgram>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            // ============================================================
            // Coach <-> TrainingProgram (Many-to-Many) mapping
            // ============================================================
            modelBuilder.Entity<CoachTrainingProgram>()
                .HasKey(x => new { x.CoachId, x.TrainingProgramId });

            // Coach tarafı CASCADE kalsın
            modelBuilder.Entity<CoachTrainingProgram>()
                .HasOne(x => x.Coach)
                .WithMany(c => c.CoachTrainingPrograms)
                .HasForeignKey(x => x.CoachId)
                .OnDelete(DeleteBehavior.Cascade);

            // TrainingProgram tarafı CASCADE OLMASIN (multiple cascade paths hatasını çözer)
            modelBuilder.Entity<CoachTrainingProgram>()
                .HasOne(x => x.TrainingProgram)
                .WithMany(tp => tp.CoachTrainingPrograms)
                .HasForeignKey(x => x.TrainingProgramId)
                .OnDelete(DeleteBehavior.Restrict); // veya NoAction

            // ============================================================
            // UnavailableSlot -> Coach ilişkisi (net navigation ile)
            // ============================================================
            modelBuilder.Entity<UnavailableSlot>()
                .HasOne(u => u.Coach)
                .WithMany(c => c.UnavailableSlots)
                .HasForeignKey(u => u.CoachId)
                .OnDelete(DeleteBehavior.Cascade);

            // ============================================================
            // Seed default roles: admin and member
            // ============================================================
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
                    Name = "member",
                    NormalizedName = "MEMBER",
                    ConcurrencyStamp = "c2b7f7b3-5f3e-5f3b-0a2b-222222222222"
                }
            );
        }
    }
}
