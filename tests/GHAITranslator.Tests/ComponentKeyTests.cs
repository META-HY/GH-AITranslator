using GHAITranslator.Core;
using Xunit;

namespace GHAITranslator.Tests;

public class ComponentKeyTests
{
    [Fact]
    public void Grasshopper_assembly_uses_native_prefix()
    {
        Assert.Equal("Native_Point", ComponentKey.Build("Grasshopper", "Point"));
        Assert.Equal("Native_Curve", ComponentKey.Build("Grasshopper", "Curve"));
    }

    [Fact]
    public void Third_party_assembly_uses_its_name()
    {
        Assert.Equal("Heron_HeronGh", ComponentKey.Build("Heron", "HeronGh"));
        Assert.Equal("Kangaroo2_Pressure", ComponentKey.Build("Kangaroo2", "Pressure"));
    }

    [Fact]
    public void Empty_assembly_falls_back_to_native()
    {
        Assert.Equal("Native_X", ComponentKey.Build("", "X"));
    }
}