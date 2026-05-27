using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PtixiakiReservations.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime StartDateTime { get; set; }
    public DateTime EndTime { get; set; }
    public int EventTypeId { get; set; }
    [ForeignKey("EventTypeId")] public EventType EventType { get; set; }
    public int VenueId { get; set; }
    [ForeignKey("VenueId")] public Venue Venue { get; set; }
    public int? SubAreaId { get; set; }
    [ForeignKey("SubAreaId")] public SubArea SubArea { get; set; }
    public int? ParentEventId { get; set; }
    [ForeignKey("ParentEventId")] public Event ParentEvent { get; set; }
    public string? ImagePath { get; set; }
    [NotMapped]
    public string DisplayImagePath => ImagePath ?? ParentEvent?.ImagePath;
    public ICollection<Event> ChildEvents { get; set; } = new List<Event>();
}