using System.Text;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Builds the prompts that drive the LLM translator. Lives in Core so it can be
/// unit-tested without a real LLM call. The wording mirrors the design doc.
/// </summary>
public static class PromptBuilder
{
    public const string SystemPrompt = @"你是专业的参数化设计与建筑行业术语翻译专家,精通 Grasshopper、Rhino 及各类参数化插件的专业术语。
请将给定的 Grasshopper 组件信息翻译为简体中文,严格遵循以下规则:
1. 术语必须符合国内建筑、参数化设计行业通用译法,禁止字面直译;
2. 组件名称简洁精准,通常 2-6 个字,符合 GH 用户使用习惯;
3. 功能描述清晰易懂,专业术语准确;
4. 输入输出参数名称保持行业通用简称;
5. 严格输出 JSON 格式,不要包含任何额外解释或 Markdown 代码块。
输出格式:
{
  ""name"": ""组件中文名称"",
  ""description"": ""组件功能中文描述"",
  ""inputs"": { ""参数英文名"": ""参数中文名"", ... },
  ""outputs"": { ""参数英文名"": ""参数中文名"", ... }
}";

    /// <summary>Build the user-side prompt from a <see cref="ComponentInfo"/>.</summary>
    public static string BuildUserPrompt(ComponentInfo info)
    {
        if (info == null) throw new System.ArgumentNullException(nameof(info));

        var sb = new StringBuilder();
        sb.Append("组件名称:").AppendLine(info.Name ?? string.Empty);
        sb.Append("组件昵称:").AppendLine(info.NickName ?? string.Empty);
        sb.Append("所属插件:").AppendLine(string.IsNullOrEmpty(info.PluginName) ? "Native" : info.PluginName);
        sb.Append("功能描述:").AppendLine(info.Description ?? string.Empty);

        AppendParamBlock(sb, "输入参数", info.InputParams);
        AppendParamBlock(sb, "输出参数", info.OutputParams);

        return sb.ToString();
    }

    private static void AppendParamBlock(StringBuilder sb, string header, ParamInfo[]? parameters)
    {
        if (parameters == null || parameters.Length == 0) return;
        sb.Append(header).AppendLine(":");
        foreach (var p in parameters)
        {
            var name = string.IsNullOrEmpty(p.Name) ? "?" : p.Name;
            var desc = string.IsNullOrEmpty(p.Description) ? "(无描述)" : p.Description;
            sb.Append("- ").Append(name).Append(": ").AppendLine(desc);
        }
    }
}
