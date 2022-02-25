using System;
using System.IO;
using CScriptEz.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace CScriptEz.Data
{
    public class CScriptEzDbContext : DbContext
    {
        private readonly string _dbPath;

        public CScriptEzDbContext()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            // var folder = Environment.SpecialFolder.LocalApplicationData;
            // var path = Environment.GetFolderPath(folder);
            _dbPath = Path.Join(path, "cscriptez.db");
        }

        public CScriptEzDbContext(string dbPath)
        {
            _dbPath = dbPath;
        }

        public DbSet<FileDataModel> FilesData { get; set; }
        public DbSet<FileModel> Files { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }

        // protected override void Dispose(bool disposing)
        // {
        //     SaveChanges();
        //     base.Dispose(disposing);
        // }
    }
}