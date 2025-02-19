using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models.ViewModels
{
    public class JsonEventModel
    {
        public string Name { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndTime { get; set; }
        public string EventTypeId { get; set; }
        public JsonRepeatTimeModel Repeat { get; set; }
    }
}
