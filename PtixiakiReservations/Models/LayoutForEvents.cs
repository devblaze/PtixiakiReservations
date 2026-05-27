using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace PtixiakiReservations.Models;


public class LayoutForEvents
{
    public int LayoutId { get; set; }
    [ForeignKey("LayoutId")] public SubArea Layout { get; set; }
    public int EventId { get; set; }
    [ForeignKey("EventId")] public Event Event { get; set; }
    public ICollection<SubArea> Layouts { get; set; } = new List<SubArea>();
}