using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models.ViewModels
{
    public class JsonReservationModel
    {
        public int SeatId { get; set; }
        public int EventId { get; set; }
        public string Duration { get; set; }
    }
}
