using System;

namespace CScriptEz.Data.Models
{
    public class FileModel
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string SourceHash { get; set; }
    }
}