using System.Collections.Generic;
using System.Text;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// Builds the system + user prompt pair sent to the LLM. The system prompt
/// fixes the schema and rules; the user prompt carries the source
/// component's metadata.
/// </summary>
public sealed class PromptBuilder
{
    public const string SystemPrompt =
        "你是 Grasshopper (GH) 插件的本地化专家。请将下列 GH 内置或第三方组件的英文元数据翻译成中文，并严格按 JSON schema 输出。\n" +
        "\n" +
        "规则：\n" +
        "1. name: 中文显示名，使用 GH 行业约定译法（如 Loft→放样、Sweep→扫掠、Boolean→布尔运算、Domain→域、Interval→区间）。\n" +
        "2. nick: 中文端口标签，最多 2 个汉字。\n" +
        "3. desc: 中文悬浮提示，以句号「。」结尾，不超过 60 字。\n" +
        "4. cat: 中文面板分组，仅限下列之一：参数、几何、数学、向量、曲线、曲面、网格、相交、变换、显示、逻辑、脚本、输入、输出、集合、树形数据、特殊。\n" +
        "5. 不要混入英文。技术专名（NURBS、B-rep、XYZ）可保留。\n" +
        "6. 只返回 JSON，不要 ``` 围栏，不要解释。";

    public IReadOnlyList<(string role, string content)> BuildChatMessages(ComponentInfo info)
    {
        var user = new StringBuilder();
        user.AppendLine($"Class: {info.FullName}");
        user.AppendLine($"Assembly: {info.Assembly}");
        user.AppendLine($"Category (EN): {info.Category}");
        if (!string.IsNullOrEmpty(info.SubCategory))
            user.AppendLine($"SubCategory (EN): {info.SubCategory}");
        user.AppendLine($"Name (EN): {info.OriginalName}");
        user.AppendLine($"NickName (EN): {info.OriginalNickName}");
        user.AppendLine($"Description (EN): {info.OriginalDescription}");
        user.AppendLine();
        user.AppendLine("Output JSON schema:");
        user.AppendLine("{ \"name\": \"...\", \"nick\": \"...\", \"desc\": \"...\", \"cat\": \"...\" }");

        return new[]
        {
            ("system", SystemPrompt),
            ("user", user.ToString()),
        };
    }
}