using GHAITranslator.Core;
using Xunit;

namespace GHAITranslator.Tests
{
    public class ComponentKeyTests
    {
        [Fact]
        public void Build_FallsBackToNative_WhenPluginMissing()
        {
            string? plugin = null;
            var key = ComponentKey.Build(plugin, "Point");
            Assert.Equal("Native_Point", key);
        }

        [Fact]
        public void Build_FallsBackToNative_WhenPluginWhitespace()
        {
            var key = ComponentKey.Build("   ", "Brep");
            Assert.Equal("Native_Brep", key);
        }

        [Fact]
        public void Build_ReplacesDotsAndSpaces()
        {
            var key = ComponentKey.Build("Kangaroo 2", "Bend Goal");
            Assert.Equal("Kangaroo_2_Bend_Goal", key);
        }

        [Fact]
        public void Build_StripsUnsafeChars()
        {
            var key = ComponentKey.Build("Weaverbird", "Mesh\\Pipe?");
            Assert.Equal("Weaverbird_Mesh_Pipe_", key);
        }

        [Fact]
        public void Build_IsDeterministic()
        {
            var a = ComponentKey.Build("Kangaroo2", "Line");
            var b = ComponentKey.Build("Kangaroo2", "Line");
            Assert.Equal(a, b);
        }

        [Fact]
        public void Build_DifferentiatesSameNameAcrossPlugins()
        {
            var a = ComponentKey.Build("PluginA", "Point");
            var b = ComponentKey.Build("PluginB", "Point");
            Assert.NotEqual(a, b);
        }

        [Fact]
        public void Build_Throws_OnEmptyComponentName()
        {
            Assert.Throws<System.ArgumentException>(() => ComponentKey.Build("Plugin", " "));
        }

        [Fact]
        public void Build_FromComponentInfo_RegeneratesCanonicalKey()
        {
            var info = new Core.Models.ComponentInfo
            {
                PluginName = "  ",
                Name = "Divide Curve",
                Key = "totally-bogus"
            };
            var key = ComponentKey.Build(info);
            Assert.Equal("Native_Divide_Curve", key);
        }
    }
}
