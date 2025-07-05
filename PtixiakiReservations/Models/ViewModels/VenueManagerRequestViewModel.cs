
using System.ComponentModel.DataAnnotations;

namespace PtixiakiReservations.Models.ViewModels
{
    public class VenueManagerRequestViewModel
    {
        [Required(ErrorMessage = "Please provide a reason for your request")]
        [MinLength(10, ErrorMessage = "Your reason should be at least 10 characters long")]
        [MaxLength(500, ErrorMessage = "Your reason cannot exceed 500 characters")]
        [Display(Name = "Why do you want to become a venue manager?")]
        public string Reason { get; set; } = string.Empty;
    }
}