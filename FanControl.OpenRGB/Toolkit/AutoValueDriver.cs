namespace FanControl.OpenRGB.Toolkit;

internal static class AutoValueDriver
{
    public static float Compute(int frame) =>
        50f + 50f * (float)Math.Sin(frame * 0.01);
}
