using System;
using System.Collections.Generic;
using System.Text;

namespace UnakinShared.DTO
{
    public class HitsDTO
    {
        public string node { get; set; }
        public string body { get; set; }
        public string type { get; set; }
        public IList<string> children { get; set; }
        public double score { get; set; }
        public string full_path { get; set; }
    }
}
