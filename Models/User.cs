using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using FitnessCenter.Models;

namespace FitnessCenter.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = string.Empty;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
