using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser() : base() { }
        public String FirstName { get; set; }

        public String LastName { get; set; }


        public static implicit operator ApplicationUser(Task<ApplicationUser> v)
        {
            throw new NotImplementedException();
        }
    }
}
