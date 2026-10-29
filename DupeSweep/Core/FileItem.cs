using System;
using System.Collections.Generic;
using System.Text;

namespace DupeSweep.Core
{
    public class FileItem
    {
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long Size { get; set; }
        public string Hash { get; set; } = "";
    }
}
