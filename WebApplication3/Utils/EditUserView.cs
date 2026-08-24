using System.ComponentModel.DataAnnotations;

namespace ReservationSystem.Utils
{
    public class EditUserView
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Jméno")]
        public string Name { get; set; }

        // Display-only: e-mail is also the login user name and is not editable here.
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }
    }
}