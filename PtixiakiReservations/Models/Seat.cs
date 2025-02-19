using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models
{
    public class Seat
    {
        public int Id { get; set; }
        public decimal X { get; set; }
        public decimal Y { get; set; } 
        public string Name { get; set; }
        public bool Available { get; set; }
        public int SubAreaId { get; set; }
        [ForeignKey("SubAreaId")]
        public SubArea SubArea { get; set; }
    }
}
