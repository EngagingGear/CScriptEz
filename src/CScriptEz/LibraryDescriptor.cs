using System;

namespace CScriptEz
{
    public class LibraryDescriptor
    {
        public LibraryDescriptor(string fileName, bool isLocal, string folderName = null)
        {
            FileName = fileName;
            IsLocal = isLocal;
            FolderName = folderName;

            if (string.IsNullOrWhiteSpace(FolderName) && !IsLocal)
            {
                throw new OperationCanceledException($"FolderName must be specified when IsLocal set to false");
            }
        }

        public string FileName { get; }
        public bool IsLocal { get; }
        public string FolderName { get; }
    }
}