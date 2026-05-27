
using System.ComponentModel.DataAnnotations.Schema;

namespace PtixiakiReservations.Models;

public class VenueCategory
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    [ForeignKey("VenueId")] public Venue Venue { get; set; }
    public int? CategoryId { get; set; }
    [ForeignKey("CategoryId")] public EventType EventType { get; set; }
}