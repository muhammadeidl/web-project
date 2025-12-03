using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using FitnessCenter.Models;

namespace FitnessCenter.Models
{
    public class User : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        // Role is already provided بواسطة IdentityUserRole
        // لذلك الأفضل عدم استخدام “Role” كـ string
        // لكن إذا تريد تبقيه، اجعله Claim وليس property
        [MaxLength(20)]
        public string Role { get; set; }

        public ICollection<Appointment> Appointments { get; set; }
    }
}
