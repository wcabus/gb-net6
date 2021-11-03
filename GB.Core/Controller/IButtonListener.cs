namespace GB.Core.Controller
{
    public interface IButtonListener
    {
        void OnButtonPress(Button button);
        void OnButtonRelease(Button button);
    }
}
