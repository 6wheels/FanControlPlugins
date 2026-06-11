using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Tests;

internal static class DeviceBuilder
{
    public static Device MakeDevice(string name, int ledCount)
    {
        var leds = Enumerable.Range(1, ledCount)
            .Select(i => MakeLed($"Led {i}"))
            .ToArray();
        var zone = MakeZone("Main", (uint)ledCount);
        var device = (Device)RuntimeHelpers.GetUninitializedObject(typeof(Device));
        SetField(device, "<Name>k__BackingField", name);
        SetField(device, "<Zones>k__BackingField", new[] { zone });
        SetField(device, "<Leds>k__BackingField", leds);
        return device;
    }

    public static Device MakeMatrixDevice(string name, uint width, uint height)
    {
        uint ledCount = width * height;
        var leds = Enumerable.Range(0, (int)ledCount)
            .Select(i => MakeLed($"Key {i}"))
            .ToArray();
        var matrix = new uint[height, width];
        for (uint y = 0; y < height; y++)
            for (uint x = 0; x < width; x++)
                matrix[y, x] = y * width + x;
        var zone = MakeZone("Main", ledCount, MakeMatrixMap(width, height, matrix));
        var device = (Device)RuntimeHelpers.GetUninitializedObject(typeof(Device));
        SetField(device, "<Name>k__BackingField", name);
        SetField(device, "<Zones>k__BackingField", new[] { zone });
        SetField(device, "<Leds>k__BackingField", leds);
        return device;
    }

    public static MatrixMap MakeMatrixMap(uint width, uint height, uint[,] matrix)
    {
        var map = (MatrixMap)RuntimeHelpers.GetUninitializedObject(typeof(MatrixMap));
        SetField(map, "<Width>k__BackingField", width);
        SetField(map, "<Height>k__BackingField", height);
        SetField(map, "<Matrix>k__BackingField", matrix);
        return map;
    }

    public static Zone MakeZone(string name, uint ledCount, MatrixMap? matrixMap = null)
    {
        var zone = (Zone)RuntimeHelpers.GetUninitializedObject(typeof(Zone));
        SetField(zone, "<Name>k__BackingField", name);
        SetField(zone, "<LedCount>k__BackingField", ledCount);
        if (matrixMap != null)
            SetField(zone, "<MatrixMap>k__BackingField", matrixMap);
        return zone;
    }

    public static Led MakeLed(string name)
    {
        var led = (Led)RuntimeHelpers.GetUninitializedObject(typeof(Led));
        SetField(led, "<Name>k__BackingField", name);
        return led;
    }

    private static void SetField(object obj, string fieldName, object? value)
    {
        var field = obj.GetType()
            .GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Field '{fieldName}' not found on {obj.GetType().Name}");
        field.SetValue(obj, value);
    }
}
