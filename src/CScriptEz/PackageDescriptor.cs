namespace CScriptEz
{
    public class PackageDescriptor
    {
        public PackageDescriptor(string name, string version, string address)
        {
            Name = name;
            Version = version;
            Address = address;
        }
        public string Name { get; }
        public string Version { get; }
        public string Address { get; }
    }
}