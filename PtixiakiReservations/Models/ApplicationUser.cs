using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models;

public class ApplicationUser : IdentityUser
{
    public ApplicationUser() : base()
    {
    }

    public String FirstName { get; set; }
    public String LastName { get; set; }
    public int? CityId { get; set; }
    [ForeignKey("CityId")] public City City { get; set; }
    public string Address { get; set; }
    public string PostalCode { get; set; }
    public bool HasRequestedVenueManagerRole { get; set; }
    public DateTime? VenueManagerRequestDate { get; set; }
    public string? VenueManagerRequestReason { get; set; }
    public string? VenueManagerRequestStatus { get; set; } // "Pending", "Approved", "Rejected"

    public static implicit operator ApplicationUser(Task<ApplicationUser> v)
    {
        throw new NotImplementedException();
    }
}