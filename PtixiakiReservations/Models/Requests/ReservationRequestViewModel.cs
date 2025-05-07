using System;

namespace PtixiakiReservations.Models.Requests;

public class ReservationRequestViewModel
{
    public int[] SeatIds { get; set; }
    public int EventId { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime ResDate { get; set; }
}