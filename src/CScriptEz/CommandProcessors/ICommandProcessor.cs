namespace CScriptEz.CommandProcessors
{
    public interface ICommandProcessor
    {
        void Run(CommandContext context);
    }
}