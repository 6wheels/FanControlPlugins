using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Toolkit;

public class LockFileTests
{
    [Fact]
    public void Path_IsInTempDirectory()
    {
        Assert.StartsWith(Path.GetTempPath(), LockFile.Path);
        Assert.EndsWith("fancontrol_rgb.lock", LockFile.Path);
    }

    [Fact]
    public void Create_CreatesFileAtExpectedPath()
    {
        try
        {
            LockFile.Create();

            Assert.True(File.Exists(LockFile.Path));
        }
        finally
        {
            LockFile.Delete();
        }
    }

    [Fact]
    public void Delete_RemovesExistingFile()
    {
        LockFile.Create();

        LockFile.Delete();

        Assert.False(File.Exists(LockFile.Path));
    }

    [Fact]
    public void Delete_DoesNotThrow_WhenFileAbsent()
    {
        if (File.Exists(LockFile.Path)) File.Delete(LockFile.Path);

        var ex = Record.Exception(LockFile.Delete);

        Assert.Null(ex);
    }
}
