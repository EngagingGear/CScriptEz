using System;
using System.IO;
using System.Linq;
using CScriptEz.Data;
using Microsoft.Extensions.Logging;

namespace CScriptEz.CommandProcessors
{
    public class ClearStaleCommandProcessor : ServiceBase, IClearStaleCommandProcessor
    {
        private readonly ICScriptEzDbContextFactory _dbContextFactory;

        public ClearStaleCommandProcessor(ICScriptEzDbContextFactory dbContextFactory, ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ClearCacheCommandProcessor>())
        {
            _dbContextFactory = dbContextFactory;
        }

        public void Run(CommandContext context)
        {
            var command = context.Commands.FirstOrDefault(x =>
                string.Equals(x.Name, KnownCommands.ClearStale, StringComparison.OrdinalIgnoreCase));

            if (command == null)
            {
                return;
            }
            Log($"Processing clear stale command. FileName: {command.FileName}");

            using var dbContext = _dbContextFactory.Create();
            var filesToProcess = string.IsNullOrWhiteSpace(command.FileName)
                ? dbContext.Files
                : dbContext.Files.Where(x => x.FileName == command.FileName);
            dbContext.Files.RemoveRange(filesToProcess);

            foreach (var fileModel in filesToProcess)
            {
                if (!File.Exists(fileModel.FileName))
                {
                    Log($"File data for file {fileModel.FileName} exist in database but file was not found. Removing stale data");
                    dbContext.Files.Remove(fileModel);
                }
            }

            dbContext.SaveChanges();
            Log($"Finished processing clear stale command. FileName: {command.FileName}");
        }

    }
}