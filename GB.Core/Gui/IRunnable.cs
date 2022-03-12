namespace GB.Core.Gui
{
    public interface IRunnable
    {
        Task Run(CancellationToken token);
    }
}
