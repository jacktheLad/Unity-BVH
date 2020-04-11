using System.Diagnostics;

public class SwTimer : System.IDisposable
{
    static Stopwatch sw = new Stopwatch();
    string eventName = string.Empty;
    public SwTimer(string context)
    {
        eventName = context;
        sw.Restart();
    }

    public void Dispose()
    {
        sw.Stop();
        UnityEngine.Debug.Log(string.Format("{0} execution time: {1} ms ", eventName, sw.ElapsedMilliseconds));
    }
}
