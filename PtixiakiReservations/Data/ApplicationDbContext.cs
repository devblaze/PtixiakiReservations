using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PtixiakiReservations.Models;


namespace PtixiakiReservations.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
          : base(options)
        {
        }
        public DbSet<Venue> Venue { get; set; }
        public DbSet<Reservation> Reservation { get; set; }
        public DbSet<SubArea> SubArea { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<FamilyEvent> FamilyEvent { get; set; }
        public DbSet<Seat> Seat { get; set; }

        public DbSet<City> City { get; set; }

        public DbSet<EventType> EventType { get; set; }

        protected override void OnModelCreating(ModelBuilder modelbuilder)
        {
            foreach (var relationship in modelbuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            {
                relationship.DeleteBehavior = DeleteBehavior.Restrict;
            }          

            base.OnModelCreating(modelbuilder);
        }
    }

}
