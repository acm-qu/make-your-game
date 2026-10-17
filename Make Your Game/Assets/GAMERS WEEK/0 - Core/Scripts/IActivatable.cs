namespace GamersWeek
{
    // Anything a Button can switch on and off (Doors, Moving Platforms...).
    // Want a Button to control your own script? Add ", IActivatable" after MonoBehaviour and write these two methods.
    public interface IActivatable
    {
        void Activate();
        void Deactivate();
    }
}
