using System.ComponentModel.DataAnnotations;

namespace ReservationSystem.Utils
{
    public class EditUserView
    {
        public string Id { get; set; }

        [Display(Name = "Jméno")]
        public string FirstName { get; set; }

        [Display(Name = "Příjmení")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Telefon")]
        public string PhoneNumber { get; set; }
    }
}
