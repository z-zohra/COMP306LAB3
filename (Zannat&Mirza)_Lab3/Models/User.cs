using System.ComponentModel.DataAnnotations;

namespace _Zannat_Mirza__Lab3.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }  

        public DateTime CreatedDate { get; set; }
    }
}
