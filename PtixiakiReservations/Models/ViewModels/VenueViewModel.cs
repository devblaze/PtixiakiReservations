using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models.ViewModels
{
    public class VenueViewModel
    {
        [Required]
        public string Name { get; set; }
        public string Address { get; set; }
        public City City { get; set; }        
        public int CityId { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }       
        public string UserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public IFormFile Photo { get; set; }
    }
}
