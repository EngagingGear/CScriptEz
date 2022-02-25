namespace CScriptEz.Data.Models
{
    public class FileDataModel
    {
        public int Id { get; set; }
        public byte[] Data { get; set; }
        public FileModel FileModel { get; set; }
    }
}