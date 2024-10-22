using System.ComponentModel.DataAnnotations;

namespace _Zannat_Mirza__Lab3.Models
{
    public class LoginViewModel
    {
        [Required]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

}
