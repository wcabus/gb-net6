using GB.Core.Gui;

namespace GB.Core.Graphics
{
    public interface IDisplay : IRunnable
    {
        bool Enabled { get; set; }

        void PutDmgPixel(int color);
        void PutColorPixel(int gbcRgb);
        void RequestRefresh();
        void WaitForRefresh();
    }
}
