namespace WebRequesterDll;

/// <summary>
/// usage:
/// console app: GlobalEvents.ProcessCompleted += (s, msg) => Console.WriteLine(msg);
/// </summary>
public static class WebRequesterEvents
{
    // A static event can be accessed without creating an instance
    public static event EventHandler<string>? ProcessCompleted;

    // A static method to trigger it from anywhere inside the DLL
    public static void RaiseProcessCompleted(string message)
    {
        ProcessCompleted?.Invoke(null, message);
    }
}