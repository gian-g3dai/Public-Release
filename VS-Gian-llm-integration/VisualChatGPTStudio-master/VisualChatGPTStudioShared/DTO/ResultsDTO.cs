using System;
using System.Collections.Generic;
using System.Text;

namespace UnakinShared.DTO
{
    public class ResultsDTO
    {
        public string uuid { get; set; }
        public string query { get; set; }
        public IList<HitsDTO> hits { get; set; }
        public int timestamp { get; set; }
    }
}
