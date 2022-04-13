namespace CScriptEz
{
    public interface IServiceResolver
    {
        T Resolve<T>();
    }
}