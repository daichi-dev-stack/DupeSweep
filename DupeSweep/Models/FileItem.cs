using System;
using System.Collections.Generic;
using System.Text;

namespace DupeSweep.Models
{
    public class FileItem
    {
        public bool IsSelected { get; set; }
        public string FileName { get; set; } = "";
        public string FullPath { get; set; } = "";
        public long Size { get; set; }
        public string Hash { get; set; } = "";
    }
}
