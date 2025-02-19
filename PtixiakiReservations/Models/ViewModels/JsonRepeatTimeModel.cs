using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Models.ViewModels
{
    public class JsonRepeatTimeModel
    {
        public string NumOfRepeat { get; set; }
        public string SelectRepeat { get; set; }
        public bool Su { get; set; }
        public bool M { get; set; }
        public bool Tu { get; set; }
        public bool W { get; set; }
        public bool Th { get; set; }
        public bool F { get; set; }
        public bool Sa { get; set; }
        public DateTime UntilDate { get; set; }
        public string AfterNumTimes { get; set; }



    }
}
