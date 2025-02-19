using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndTime { get; set; }
        public int EventTypeId { get; set; }
        [ForeignKey("EventTypeId")]
        public EventType EventType { get; set; }
        public int VenueId { get; set; }
        [ForeignKey("VenueId")]
        public Venue Venue { get; set; }
        public int FamilyEventId { get; set; }
        [ForeignKey("FamilyEventId")]
        public FamilyEvent FamilyEvent { get; set; }
    }
}
