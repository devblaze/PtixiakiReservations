using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models.ViewModels
{
    public class JsonSubAreaModel
    {
        public decimal Top { get; set; }
        public decimal Left { get; set; }
        public string AreaName { get; set; }

        public decimal Rotate { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
    }
}
