namespace FanControl.OpenRGB.Toolkit;

internal static class LockFile
{
    public static string Path =>
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fancontrol_rgb.lock");

    public static void Create()
    {
        try { File.Create(Path).Dispose(); } catch { }
    }

    public static void Delete()
    {
        try { if (File.Exists(Path)) File.Delete(Path); } catch { }
    }
}
