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
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? CityId { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }
}