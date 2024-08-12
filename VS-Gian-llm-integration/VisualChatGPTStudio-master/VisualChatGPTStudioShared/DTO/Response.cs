using System;
using System.Collections.Generic;
using System.Text;

namespace UnakinShared.DTO
{
    public class Response
    {
        public ICSharpCode.AvalonEdit.Document.TextDocument Doc { get; set; }
        public string FilePath { get; set; }
    }
}
