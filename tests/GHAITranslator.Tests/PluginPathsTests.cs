using GHAITranslator.Core;
using Xunit;

namespace GHAITranslator.Tests
{
    public class PluginPathsTests
    {
        [Fact]
        public void ReturnsPathsUnderMcNeelRhinoceros()
        {
            var dir = PluginPaths.GetPluginDir("7.0", appData: "/tmp/fake-appdata");
            Assert.Contains("McNeel", dir);
            Assert.Contains("7.0", dir);
            Assert.Contains("GH-AITranslator", dir);
        }

        [Fact]
        public void DictionaryAndSettings_AreSiblingFiles()
        {
            var dict = PluginPaths.GetDictionaryPath("8.0", appData: "/tmp/fake");
            var settings = PluginPaths.GetSettingsPath("8.0", appData: "/tmp/fake");
            Assert.Equal(System.IO.Path.GetDirectoryName(dict), System.IO.Path.GetDirectoryName(settings));
        }
    }
}
