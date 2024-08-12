using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnakinShared.DTO
{
    public class AgentDTO
    {
        public int Id { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }
        public string Functionality { get; set; }
        public int Sequence { get; set; }
        public bool Active { get; set; }
        public Boolean IsDirty { get; set; }

    }
}

namespace UnakinShared.Models
{
    public class Agent
    {
        [SQLite.PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }
        public string Functionality { get; set; }
        public int Sequence { get; set; }
        public bool Active { get; set; }

    }
}
