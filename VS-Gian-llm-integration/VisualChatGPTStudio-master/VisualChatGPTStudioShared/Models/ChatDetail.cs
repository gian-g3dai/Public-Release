using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnakinShared.Models
{
    internal class ChatDetail
    {
        [SQLite.PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int Index { get; set; }
        public int ChatMasterId { get; set; }
        public int Aurthor { get; set; }
        public bool IsFirstSegment { get; set; }
        public string Content { get; set; }
        public string Syntax { get; set; }
        public String Image { get; set; }
        public int Sequence { get; set; }

        public DateTime CreationTime { get; set; }
    }
}
