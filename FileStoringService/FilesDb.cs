using System;
using Microsoft.EntityFrameworkCore;

namespace FileStoringService
{
    public class FilesDb : DbContext
    {
        public FilesDb(DbContextOptions<FilesDb> options) : base(options) { }

        public DbSet<FileMeta> StoredFiles { get; set; }
    }

}

