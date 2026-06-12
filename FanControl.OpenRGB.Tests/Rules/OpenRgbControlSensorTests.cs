using FanControl.OpenRGB;
using Xunit;

namespace FanControl.OpenRGB.Tests.Rules;

public class OpenRgbControlSensorTests
{
    [Fact]
    public void Properties_StoredFromCtor()
    {
        var s = new OpenRgbControlSensor("my-id", "My Name");
        Assert.Equal("my-id", s.Id);
        Assert.Equal("My Name", s.Name);
    }

    [Fact]
    public void InitialValue_IsZero()
    {
        var s = new OpenRgbControlSensor("id", "name");
        Assert.Equal(0f, s.Value);
    }

    [Fact]
    public void Set_StoresValue()
    {
        var s = new OpenRgbControlSensor("id", "name");
        s.Set(42.5f);
        Assert.Equal(42.5f, s.Value);
    }

    [Fact]
    public void Reset_SetsValueToZero()
    {
        var s = new OpenRgbControlSensor("id", "name");
        s.Set(75f);
        s.Reset();
        Assert.Equal(0f, s.Value);
    }

    [Fact]
    public void Update_IsNoOp()
    {
        var s = new OpenRgbControlSensor("id", "name");
        s.Set(50f);
        s.Update();
        Assert.Equal(50f, s.Value);
    }
}
