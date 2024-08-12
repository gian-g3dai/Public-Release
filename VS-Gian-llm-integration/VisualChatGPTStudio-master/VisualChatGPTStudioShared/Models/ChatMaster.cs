using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnakinShared.Models
{
    internal class ChatMaster
    {
        [SQLite.PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int ChatType { get; set; } // 1. Chat, 2. Agent
        public string Name { get; set; }
        public string Desc { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
    }
}
