using System;
namespace FileStoringService
{
    public class FileMeta
    {
        public Guid Id { get; set; }
        public string OriginalName { get; set; } = "";
        public string HashSum { get; set; } = "";
        public string FilePath { get; set; } = "";
    }

}

