using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models
{
    public class SubArea
    {
        public int Id { get; set; }
        public string AreaName { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public decimal Top { get; set; }
        public decimal Left { get; set; }
        public decimal Rotate { get; set; }
        public string Desc { get; set; }
        public int VenueId { get; set; }
        [ForeignKey("VenueId")]
        public Venue Venue { get; set; }
    }
}
