using System.Collections.Generic;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core;

/// <summary>
/// The shipped-with-the-plugin seed dictionary covering Rhino/Grasshopper native
/// primitives. The intent is "zero-latency hit" for the components a user reaches
/// for in their first session, NOT a full localization of every GH component.
///
/// Entries use the canonical key rule (see <see cref="ComponentKey"/>):
/// keys are "{PluginName}_{SanitizedComponentName}".
/// </summary>
internal static class BuiltinSeed
{
    public static Dictionary<string, TranslationEntry> Build()
    {
        var dict = new Dictionary<string, TranslationEntry>(System.StringComparer.Ordinal)
        {
            // ────── Geometry primitives ──────
            ["Native_Point"] = new()
            {
                Name = "点",
                Description = "通过 X、Y、Z 坐标创建一个三维点对象",
                Inputs = { ["X"] = "X 坐标", ["Y"] = "Y 坐标", ["Z"] = "Z 坐标" },
                Outputs = { ["Pt"] = "点" },
                Source = TranslationSource.Builtin
            },
            ["Native_Line"] = new()
            {
                Name = "线",
                Description = "通过两个端点创建一条线段",
                Inputs = { ["A"] = "起点", ["B"] = "终点" },
                Outputs = { ["L"] = "线" },
                Source = TranslationSource.Builtin
            },
            ["Native_Circle"] = new()
            {
                Name = "圆",
                Description = "通过圆心和半径创建圆",
                Inputs = { ["Plane"] = "基准平面", ["Radius"] = "半径" },
                Outputs = { ["C"] = "圆" },
                Source = TranslationSource.Builtin
            },
            ["Native_Arc"] = new()
            {
                Name = "圆弧",
                Description = "通过起点、终点和方向上的点创建圆弧",
                Inputs = { ["Start"] = "起点", ["End"] = "终点", ["Dir"] = "方向点" },
                Outputs = { ["A"] = "圆弧" },
                Source = TranslationSource.Builtin
            },
            ["Native_Polyline"] = new()
            {
                Name = "多段线",
                Description = "通过有序点集合创建多段线",
                Inputs = { ["V"] = "顶点列表" },
                Outputs = { ["P"] = "多段线" },
                Source = TranslationSource.Builtin
            },
            ["Native_Polygon"] = new()
            {
                Name = "多边形",
                Description = "在给定平面上创建正多边形",
                Inputs = { ["Plane"] = "基准平面", ["Radius"] = "半径", ["Sides"] = "边数" },
                Outputs = { ["P"] = "多边形" },
                Source = TranslationSource.Builtin
            },
            ["Native_Rectangle"] = new()
            {
                Name = "矩形",
                Description = "在给定平面上创建矩形",
                Inputs = { ["Plane"] = "基准平面", ["X"] = "X 尺寸", ["Y"] = "Y 尺寸" },
                Outputs = { ["R"] = "矩形" },
                Source = TranslationSource.Builtin
            },
            ["Native_Ellipse"] = new()
            {
                Name = "椭圆",
                Description = "在给定平面上创建椭圆",
                Inputs = { ["Plane"] = "基准平面", ["A"] = "长轴", ["B"] = "短轴" },
                Outputs = { ["E"] = "椭圆" },
                Source = TranslationSource.Builtin
            },
            ["Native_Box"] = new()
            {
                Name = "立方体",
                Description = "通过两个对角点或基准面与尺寸创建长方体",
                Inputs = { ["Box"] = "立方体", ["Base"] = "基准面", ["X"] = "X 尺寸", ["Y"] = "Y 尺寸", ["Z"] = "Z 尺寸" },
                Outputs = { ["B"] = "立方体" },
                Source = TranslationSource.Builtin
            },
            ["Native_Plane"] = new()
            {
                Name = "平面",
                Description = "通过原点与 X、Y 轴方向创建工作平面",
                Inputs = { ["O"] = "原点", ["X"] = "X 方向", ["Y"] = "Y 方向" },
                Outputs = { ["P"] = "平面" },
                Source = TranslationSource.Builtin
            },
            ["Native_Vector"] = new()
            {
                Name = "向量",
                Description = "通过分量创建三维向量",
                Inputs = { ["X"] = "X 分量", ["Y"] = "Y 分量", ["Z"] = "Z 分量" },
                Outputs = { ["V"] = "向量" },
                Source = TranslationSource.Builtin
            },
            ["Native_Plane_Surface"] = new()
            {
                Name = "平面曲面",
                Description = "将基准面转换为无限大的平面曲面",
                Inputs = { ["P"] = "基准面" },
                Outputs = { ["S"] = "曲面" },
                Source = TranslationSource.Builtin
            },

            // ────── Domain / number ──────
            ["Native_Number"] = new()
            {
                Name = "数字",
                Description = "数值滑块,支持整数与浮点",
                Inputs = { ["Min"] = "最小值", ["Max"] = "最大值", ["Value"] = "当前值" },
                Outputs = { ["N"] = "数字" },
                Source = TranslationSource.Builtin
            },
            ["Native_Integer"] = new()
            {
                Name = "整数",
                Description = "整型滑块",
                Outputs = { ["N"] = "整数" },
                Source = TranslationSource.Builtin
            },
            ["Native_Range"] = new()
            {
                Name = "数值范围",
                Description = "生成等差数列(范围)",
                Inputs = { ["Domain"] = "值域", ["Steps"] = "步数" },
                Outputs = { ["I"] = "数列" },
                Source = TranslationSource.Builtin
            },
            ["Native_Series"] = new()
            {
                Name = "数列",
                Description = "按起点与步长生成等差数列",
                Inputs = { ["S"] = "起点", ["N"] = "数量", ["C"] = "步长" },
                Outputs = { ["I"] = "数列" },
                Source = TranslationSource.Builtin
            },
            ["Native_Random"] = new()
            {
                Name = "随机数",
                Description = "生成指定范围内的随机数",
                Inputs = { ["Domain"] = "值域", ["N"] = "数量", ["S"] = "种子" },
                Outputs = { ["R"] = "随机数" },
                Source = TranslationSource.Builtin
            },

            // ────── Sets / lists ──────
            ["Native_List_Length"] = new()
            {
                Name = "列表长度",
                Description = "返回列表中的元素数量",
                Inputs = { ["L"] = "列表" },
                Outputs = { ["N"] = "数量" },
                Source = TranslationSource.Builtin
            },
            ["Native_List_Item"] = new()
            {
                Name = "列表取项",
                Description = "按索引从列表中取出一个元素",
                Inputs = { ["L"] = "列表", ["i"] = "索引" },
                Outputs = { ["I"] = "元素" },
                Source = TranslationSource.Builtin
            },
            ["Native_Cull_Nth"] = new()
            {
                Name = "隔项筛选",
                Description = "按 N 步长从列表中筛除元素",
                Inputs = { ["L"] = "列表", ["N"] = "步长" },
                Outputs = { ["L"] = "筛选结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Split_List"] = new()
            {
                Name = "分割列表",
                Description = "将列表按指定索引位置拆分为两个子列表",
                Inputs = { ["L"] = "列表", ["i"] = "分割位置" },
                Outputs = { ["A"] = "前段", ["B"] = "后段" },
                Source = TranslationSource.Builtin
            },
            ["Native_Weave"] = new()
            {
                Name = "交错合并",
                Description = "将两个列表交错合并为一个列表",
                Inputs = { ["A"] = "列表 A", ["B"] = "列表 B" },
                Outputs = { ["W"] = "合并结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Flatten_Tree"] = new()
            {
                Name = "拍平树形",
                Description = "将多维数据树拍平为一维列表",
                Inputs = { ["T"] = "数据树" },
                Outputs = { ["T"] = "拍平结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Graft_Tree"] = new()
            {
                Name = "嫁接树形",
                Description = "为列表中每个元素建立独立分支",
                Inputs = { ["T"] = "数据树" },
                Outputs = { ["T"] = "嫁接结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Simplify_Tree"] = new()
            {
                Name = "简化树形",
                Description = "移除数据树中重复的路径结构",
                Inputs = { ["T"] = "数据树" },
                Outputs = { ["T"] = "简化结果" },
                Source = TranslationSource.Builtin
            },

            // ────── Math ──────
            ["Native_Addition"] = new()
            {
                Name = "加法",
                Description = "对输入值求和(可输入多项)",
                Inputs = { ["A"] = "A", ["B"] = "B" },
                Outputs = { ["R"] = "结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Subtraction"] = new()
            {
                Name = "减法",
                Description = "A 减去 B",
                Inputs = { ["A"] = "A", ["B"] = "B" },
                Outputs = { ["R"] = "结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Multiplication"] = new()
            {
                Name = "乘法",
                Description = "对输入值求积(可输入多项)",
                Inputs = { ["A"] = "A", ["B"] = "B" },
                Outputs = { ["R"] = "结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Division"] = new()
            {
                Name = "除法",
                Description = "A 除以 B",
                Inputs = { ["A"] = "A", ["B"] = "B" },
                Outputs = { ["R"] = "结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Minimum"] = new()
            {
                Name = "最小值",
                Description = "返回输入值中的最小值",
                Inputs = { ["A"] = "A", ["B"] = "B" },
                Outputs = { ["R"] = "结果" },
                Source = TranslationSource.Builtin
            },
            ["Native_Maximum"] = new()
            {
                Name = "最大值",
                Description = "返回输入值中的最大值",
                Inputs = { ["A"] = "A", ["B"] = "B" },
                Outputs = { ["R"] = "结果" },
                Source = TranslationSource.Builtin
            },

            // ────── Display / preview ──────
            ["Native_Panel"] = new()
            {
                Name = "面板",
                Description = "显示文本与多行字符串,支持表达式",
                Inputs = { ["x"] = "输入" },
                Outputs = { ["out"] = "输出" },
                Source = TranslationSource.Builtin
            },

            // ────── Curve ops (subset) ──────
            ["Native_Curve"] = new()
            {
                Name = "曲线",
                NickName = "曲线",
                NameEn = "Curve",
                NickNameEn = "Curve",
                Description = "从曲线数据列表中取出一条曲线",
                Inputs = { ["C"] = "曲线" },
                Outputs = { ["C"] = "曲线" },
                Source = TranslationSource.Builtin
            },
            ["Native_Divide_Curve"] = new()
            {
                Name = "等分曲线",
                Description = "沿曲线按等弧长或等参数生成若干点",
                Inputs = { ["C"] = "曲线", ["N"] = "段数" },
                Outputs = { ["P"] = "点", ["t"] = "参数" },
                Source = TranslationSource.Builtin
            },
            ["Native_Evaluate_Curve"] = new()
            {
                Name = "曲线求值",
                Description = "在给定参数处计算曲线上的点与切线",
                Inputs = { ["C"] = "曲线", ["t"] = "参数" },
                Outputs = { ["P"] = "点", ["T"] = "切向" },
                Source = TranslationSource.Builtin
            },
            ["Native_Offset_Curve"] = new()
            {
                Name = "偏移曲线",
                Description = "沿平面法向偏移曲线生成新曲线",
                Inputs = { ["C"] = "曲线", ["D"] = "距离", ["P"] = "平面" },
                Outputs = { ["C"] = "偏移曲线" },
                Source = TranslationSource.Builtin
            },

            // ────── Surface ops (subset) ──────
            ["Native_Surface_From_Points"] = new()
            {
                Name = "点阵成面",
                Description = "通过规则点阵构建 Nurbs 曲面",
                Inputs = { ["P"] = "点", ["U"] = "U 阶数", ["V"] = "V 阶数" },
                Outputs = { ["S"] = "曲面" },
                Source = TranslationSource.Builtin
            },
            ["Native_Iso_Curve"] = new()
            {
                Name = "等参线",
                Description = "从曲面提取 U 向或 V 向等参线",
                Inputs = { ["S"] = "曲面", ["D"] = "方向", ["t"] = "参数" },
                Outputs = { ["C"] = "曲线" },
                Source = TranslationSource.Builtin
            },

            // ────── Common params ──────
            ["Native_Number_Slider"] = new()
            {
                Name = "数值滑块",
                Description = "通过滑块交互输入数值",
                Outputs = { ["N"] = "数值" },
                Source = TranslationSource.Builtin
            },
            ["Native_Boolean_Toggle"] = new()
            {
                Name = "布尔开关",
                Description = "切换 True / False",
                Outputs = { ["B"] = "布尔值" },
                Source = TranslationSource.Builtin
            },
            ["Native_Colour_Swatch"] = new()
            {
                Name = "拾色器",
                Description = "通过拾色器选择颜色",
                Outputs = { ["C"] = "颜色" },
                Source = TranslationSource.Builtin
            },
        };

        return dict;
    }
}
