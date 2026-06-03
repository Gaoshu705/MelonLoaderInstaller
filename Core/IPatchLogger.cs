namespace MelonLoader.Installer.Core;

public interface IPatchLogger
{
    public void Log(string message);
    public void LogProgress(string description, long downloaded, long total);
    public void LogProgressComplete();
}
