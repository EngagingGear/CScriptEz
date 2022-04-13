using System;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace CScriptEz.Data
{
    public class CScriptEzDbContextFactory : ICScriptEzDbContextFactory
    {
        public CScriptEzDbContextFactory()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "cscriptez.db");
        }

        private string DbPath { get; }

        public CScriptEzDbContext Create()
        {
            var dbContext = new CScriptEzDbContext();
            dbContext.Database.Migrate();
            return dbContext;
        }
    }
}