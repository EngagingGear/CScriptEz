using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CScriptEz.Steps.Results;
using Microsoft.Extensions.Logging;

namespace CScriptEz.Steps.Impl
{
    public class ScriptFileReader : ServiceBase, IScriptFileReader
    {
        public ScriptFileReader(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<ScriptFileReader>())
        {
        }

        public void Run(ExecutionContext context)
        {
            var filePath = context.FilePath;
            LogTitle($"Script file to process: {filePath}");
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException("Check if file exists", filePath);
            }

            var name = fileInfo.Name;
            var modifiedDate = fileInfo.LastWriteTimeUtc;
            using var reader = fileInfo.OpenText();
            var content = reader.ReadToEnd();

            var scriptInfo = new ScriptInfo
            {
                FileName = name,
                ModifiedDate = modifiedDate,
                Content = content,
                ContentHash = GetFileContentHash(content)
            };
            Log(scriptInfo.ToString());

            context.Script = scriptInfo;
        }

        private string GetFileContentHash(string text)
        {
            using var md5 = MD5.Create();
            return BitConverter
                .ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(text)))
                .Replace("-", string.Empty);
        }
    }
}