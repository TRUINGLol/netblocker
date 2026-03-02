namespace InternetBlocker.Core.Interfaces;

public interface IAutostartService
{
    bool IsEnabled { get; }
    void Enable();
    void Disable();
}
