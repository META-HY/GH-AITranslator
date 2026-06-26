using System;
using GHAITranslator.Core;
using GHAITranslator.Core.Models;
using Xunit;

namespace GHAITranslator.Tests
{
    public class PromptBuilderTests
    {
        [Fact]
        public void SystemPrompt_IsNonEmpty_AndMentionsJson()
        {
            Assert.False(string.IsNullOrWhiteSpace(PromptBuilder.SystemPrompt));
            Assert.Contains("JSON", PromptBuilder.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UserPrompt_IncludesNameNicknamePlugin()
        {
            var info = new ComponentInfo
            {
                Name = "Brep",
                NickName = "Brep",
                PluginName = "Native",
                Description = "Boundary rep.",
                InputParams = new[]
                {
                    new ParamInfo { Name = "F", Description = "Faces" }
                }
            };
            var prompt = PromptBuilder.BuildUserPrompt(info);
            Assert.Contains("Brep", prompt);
            Assert.Contains("Native", prompt);
            Assert.Contains("Faces", prompt);
        }

        [Fact]
        public void UserPrompt_HandlesNullInfo()
        {
            Core.Models.ComponentInfo? info = null;
            Assert.Throws<ArgumentNullException>(() => PromptBuilder.BuildUserPrompt(info));
        }

        [Fact]
        public void UserPrompt_OmitsEmptyParamBlocks()
        {
            var info = new ComponentInfo { Name = "X", PluginName = "Y" };
            var prompt = PromptBuilder.BuildUserPrompt(info);
            Assert.DoesNotContain("输入参数", prompt);
            Assert.DoesNotContain("输出参数", prompt);
        }
    }
}
