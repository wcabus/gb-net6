namespace GB.Core.Graphics
{
    public class NullDisplay : IDisplay
    {
        public bool Enabled { get; set; }
        public bool HasGameloop => false;

        public void PutDmgPixel(int color)
        {
        }

        public void PutColorPixel(int gbcRgb)
        {
        }

        public void RequestRefresh()
        {
        }

        public void WaitForRefresh()
        {
        }

        public Task Run(CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}
