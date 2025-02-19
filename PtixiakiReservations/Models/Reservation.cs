using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models
{
    public class Reservation
    {    
        public int ID { get; set; }
        public string UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser ApplicationUser { get; set; }
        public int SeatId { get; set; }
        [ForeignKey("SeatId")]
        public Seat Seat { get; set; }
       public int EventId { get; set; }
        [ForeignKey("EventId")]
        public Event Event { get; set; }
        public DateTime Date { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
