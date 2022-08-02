using System;
using System.Linq;
using CScriptEz.Data;
using CScriptEz.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CScriptEz.CommandProcessors
{
    public class ClearCacheCommandProcessor : ServiceBase, IClearCacheCommandProcessor
    {
        private readonly ICScriptEzDbContextFactory _dbContextFactory;

        public ClearCacheCommandProcessor(ICScriptEzDbContextFactory dbContextFactory, ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ClearCacheCommandProcessor>())
        {
            _dbContextFactory = dbContextFactory;
        }

        public void Run(CommandContext context)
        {
            var command = context.Commands.FirstOrDefault(x =>
                string.Equals(x.Name, KnownCommands.ClearCache, StringComparison.OrdinalIgnoreCase));

            if (command == null)
            {
                return;
            }
            Log($"Processing clear cache command. FileName: {command.FileName}");

            using var dbContext = _dbContextFactory.Create();
            var filesToProcess = string.IsNullOrWhiteSpace(command.FileName) 
                ? dbContext.Files 
                : dbContext.Files.Where(x => x.FileName == command.FileName);
            dbContext.Files.RemoveRange(filesToProcess);
            dbContext.SaveChanges();
            Log($"Finished processing clear cache command. FileName: {command.FileName}");
        }

    }
}