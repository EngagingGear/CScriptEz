namespace CScriptEz
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            using var serviceResolver = new ServiceResolver();
            var processor = serviceResolver.Resolve<ICScriptEzApplication>();
            processor.Run(args);
        }
    }
}