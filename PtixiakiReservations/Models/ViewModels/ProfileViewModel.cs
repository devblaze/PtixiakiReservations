using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PtixiakiReservations.Models.ViewModels
{
    public class ProfileViewModel
    {
        public ApplicationUser User { get; set; }
        public IList<string> Roles { get; set; }
        public List<Reservation> RecentReservations { get; set; }
    }

    public class ProfileEditViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}