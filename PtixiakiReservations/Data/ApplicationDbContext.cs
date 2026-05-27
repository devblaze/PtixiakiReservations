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
        public DbSet<Seat> Seat { get; set; }
        public DbSet<City> City { get; set; }
        public DbSet<EventType> EventType { get; set; }
		public DbSet<VenueCategory> VenueCategory { get; set; }



	protected override void OnModelCreating(ModelBuilder modelbuilder)
	{
  	  foreach (var relationship in modelbuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
    	{
        relationship.DeleteBehavior = DeleteBehavior.Restrict;
   	 }

   	 // FIX: TimeSpan mapping
	    modelbuilder.Entity<Reservation>()
	        .Property(r => r.Duration)
	        .HasConversion(
 	           v => v.Ticks,
	            v => TimeSpan.FromTicks(v)
 	       );

	    // FIX: decimal precision for PostgreSQL
	    modelbuilder.Entity<SubArea>().Property(s => s.Width).HasPrecision(18, 2);
	    modelbuilder.Entity<SubArea>().Property(s => s.Height).HasPrecision(18, 2);
	    modelbuilder.Entity<SubArea>().Property(s => s.Top).HasPrecision(18, 2);
	    modelbuilder.Entity<SubArea>().Property(s => s.Left).HasPrecision(18, 2);
	    modelbuilder.Entity<SubArea>().Property(s => s.Rotate).HasPrecision(18, 2);

	    modelbuilder.Entity<Seat>().Property(s => s.X).HasPrecision(18, 2);
	    modelbuilder.Entity<Seat>().Property(s => s.Y).HasPrecision(18, 2);

	    base.OnModelCreating(modelbuilder);
	}

    }

}
