// Third-party plugin translation packs. Each pack is a static list of
// ComponentKey-canonical entries, kept separate from BuiltinSeed so a user
// can opt out of packs they don't have installed. The plugin layer can
// register them on demand; the Core layer exposes them as raw lists so the
// test suite can assert their content without any GH side-effects.

using System.Collections.Generic;
using GHAITranslator.Core.Models;

namespace GHAITranslator.Core.Packs
{
    public static class ThirdPartyPacks
    {
        /// <summary>All available packs, by plugin name.</summary>
        public static IReadOnlyList<ITranslationPack> All { get; } = new ITranslationPack[]
        {
            Kangaroo2Pack.Instance,
            WeaverbirdPack.Instance,
            LunchBoxPack.Instance,
            OpenNestPack.Instance
        };

        public static ITranslationPack? FindByPlugin(string pluginName)
        {
            if (string.IsNullOrEmpty(pluginName)) return null;
            foreach (var p in All)
                if (p.PluginName == pluginName) return p;
            return null;
        }
    }

    public interface ITranslationPack
    {
        string PluginName { get; }
        string DisplayName { get; }
        IReadOnlyDictionary<string, TranslationEntry> Entries { get; }
    }

    // ─────────────────────────────────────────────────────────────────
    // Kangaroo 2
    // ─────────────────────────────────────────────────────────────────
    internal sealed class Kangaroo2Pack : ITranslationPack
    {
        public static readonly Kangaroo2Pack Instance = new();
        public string PluginName => "Kangaroo2";
        public string DisplayName => "Kangaroo 2 (Daniel Piker's physics solver)";
        public IReadOnlyDictionary<string, TranslationEntry> Entries { get; }

        private Kangaroo2Pack()
        {
            var d = new Dictionary<string, TranslationEntry>(System.StringComparer.Ordinal);
            void Add(string name, string zh, string desc,
                (string, string)[]? ins = null, (string, string)[]? outs = null)
            {
                var e = new TranslationEntry { Name = zh, Description = desc, Source = TranslationSource.Builtin };
                if (ins != null) foreach (var (k, v) in ins) e.Inputs[k] = v;
                if (outs != null) foreach (var (k, v) in outs) e.Outputs[k] = v;
                d[$"Kangaroo2_{Sanitize(name)}"] = e;
            }

            Add("BendGoal",   "弯曲目标",   "将两条线在端点处弯折成指定角度",
                ins: new[] { ("L1", "线 1"), ("L2", "线 2"), ("A", "目标角度") },
                outs: new[] { ("L1", "线 1"), ("L2", "线 2") });
            Add("AngleGoal",  "角度目标",   "约束两条线之间的夹角",
                ins: new[] { ("L1", "线 1"), ("L2", "线 2"), ("A", "目标角度") },
                outs: new[] { ("L1", "线 1"), ("L2", "线 2") });
            Add("LengthGoal", "长度目标",   "约束线的长度为目标值",
                ins: new[] { ("L", "线"), ("L0", "目标长度") },
                outs: new[] { ("L", "线") });
            Add("Spring",     "弹簧",       "Hooke 弹簧(线段 + 刚度 + 阻尼)",
                ins: new[] { ("S", "起点"), ("E", "终点"), ("L0", "静止长度"), ("K", "刚度") },
                outs: new[] { ("S", "起点"), ("E", "终点") });
            Add("Grab",       "拖拽",       "将一个点拖拽到目标位置(可由滑块控制)",
                ins: new[] { ("P", "点"), ("T", "目标") },
                outs: new[] { ("P", "点") });
            Add("ZombieSolver", "Zombie 解算器", "迭代求解所有目标,直至收敛或达到步数",
                ins: new[] { ("G", "目标列表"), ("IT", "最大迭代") },
                outs: new[] { ("G", "目标列表") });
            Add("MeshSpring",  "网格弹簧",   "将网格的每条边视为弹簧",
                ins: new[] { ("M", "网格") },
                outs: new[] { ("M", "网格") });
            Add("Pressure",    "压力",       "由体积内表面施加均匀气压",
                ins: new[] { ("M", "网格"), ("P", "压力值") },
                outs: new[] { ("M", "网格") });

            Entries = d;
        }

        private static string Sanitize(string s) => s; // all our keys are clean
    }

    // ─────────────────────────────────────────────────────────────────
    // Weaverbird (mesh subdivision / weaving)
    // ─────────────────────────────────────────────────────────────────
    internal sealed class WeaverbirdPack : ITranslationPack
    {
        public static readonly WeaverbirdPack Instance = new();
        public string PluginName => "Weaverbird";
        public string DisplayName => "Weaverbird (网格细分 / 编织)";
        public IReadOnlyDictionary<string, TranslationEntry> Entries { get; }

        private WeaverbirdPack()
        {
            var d = new Dictionary<string, TranslationEntry>(System.StringComparer.Ordinal);
            void Add(string name, string zh, string desc,
                (string, string)[]? ins = null, (string, string)[]? outs = null)
            {
                var e = new TranslationEntry { Name = zh, Description = desc, Source = TranslationSource.Builtin };
                if (ins != null) foreach (var (k, v) in ins) e.Inputs[k] = v;
                if (outs != null) foreach (var (k, v) in outs) e.Outputs[k] = v;
                d[$"Weaverbird_{name}"] = e;
            }

            Add("CatmullClark", "Catmull-Clark 细分", "四边形网格的 Catmull-Clark 细分",
                ins: new[] { ("M", "网格"), ("N", "迭代次数") }, outs: new[] { ("M", "网格") });
            Add("LoopSubdivision", "Loop 细分",     "三角网格的 Loop 细分",
                ins: new[] { ("M", "网格"), ("N", "迭代次数") }, outs: new[] { ("M", "网格") });
            Add("Weave",       "编织",           "在网格面上生成编织图案",
                ins: new[] { ("M", "网格"), ("D", "条带方向") }, outs: new[] { ("M", "网格") });
            Add("PictureFrame","画框",            "将网格外缘变形成画框状",
                ins: new[] { ("M", "网格"), ("W", "框宽") }, outs: new[] { ("M", "网格") });
            Add("Stitch",      "缝合",            "按边缝合两片网格",
                ins: new[] { ("M1", "网格 1"), ("M2", "网格 2") }, outs: new[] { ("M", "网格") });
            Add("QuadRemesh",  "四边形重网格",    "将任意网格重采样为高质量四边形网格",
                ins: new[] { ("M", "网格"), ("T", "目标边长") }, outs: new[] { ("M", "网格") });
            Add("MeshThicken", "网格加厚",       "为开放网格生成封闭的加厚体",
                ins: new[] { ("M", "网格"), ("D", "厚度") }, outs: new[] { ("M", "网格") });
            Add("SplitFaces",  "分割面",         "将网格的每个三角面拆分为三个新面",
                ins: new[] { ("M", "网格") }, outs: new[] { ("M", "网格") });

            Entries = d;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // LunchBox (paneling tools)
    // ─────────────────────────────────────────────────────────────────
    internal sealed class LunchBoxPack : ITranslationPack
    {
        public static readonly LunchBoxPack Instance = new();
        public string PluginName => "LunchBox";
        public string DisplayName => "LunchBox (面板化工具集)";
        public IReadOnlyDictionary<string, TranslationEntry> Entries { get; }

        private LunchBoxPack()
        {
            var d = new Dictionary<string, TranslationEntry>(System.StringComparer.Ordinal);
            void Add(string name, string zh, string desc,
                (string, string)[]? ins = null, (string, string)[]? outs = null)
            {
                var e = new TranslationEntry { Name = zh, Description = desc, Source = TranslationSource.Builtin };
                if (ins != null) foreach (var (k, v) in ins) e.Inputs[k] = v;
                if (outs != null) foreach (var (k, v) in outs) e.Outputs[k] = v;
                d[$"LunchBox_{name}"] = e;
            }

            Add("GridFromSurface", "曲面网格化", "在曲面上生成 U/V 网格点",
                ins: new[] { ("S", "曲面"), ("U", "U 向数量"), ("V", "V 向数量") },
                outs: new[] { ("P", "点"), ("UV", "参数") });
            Add("QuadPanel",       "四边面板",   "在曲面上生成四边形面板",
                ins: new[] { ("S", "曲面"), ("U", "U 向"), ("V", "V 向") },
                outs: new[] { ("P", "面板") });
            Add("TriangularPanel", "三角面板",   "在曲面上生成三角形面板",
                ins: new[] { ("S", "曲面"), ("U", "U 向"), ("V", "V 向") },
                outs: new[] { ("P", "面板") });
            Add("Diagrid",         "斜交网格",   "生成菱形/斜交图案的网格面",
                ins: new[] { ("S", "曲面"), ("U", "U 向"), ("V", "V 向"), ("A", "角度") },
                outs: new[] { ("P", "面板") });
            Add("Domino",          "多米诺",     "在曲面上生成多米诺骨牌状面板",
                ins: new[] { ("S", "曲面"), ("U", "U 向"), ("V", "V 向") },
                outs: new[] { ("P", "面板") });
            Add("RotateDomain",    "旋转域",     "按给定比率对曲面 U/V 域进行插值采样",
                ins: new[] { ("S", "曲面"), ("A", "起点比率"), ("B", "终点比率") },
                outs: new[] { ("P", "点") });

            Entries = d;
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // OpenNest (nesting on flat sheets)
    // ─────────────────────────────────────────────────────────────────
    internal sealed class OpenNestPack : ITranslationPack
    {
        public static readonly OpenNestPack Instance = new();
        public string PluginName => "OpenNest";
        public string DisplayName => "OpenNest (板材排样)";
        public IReadOnlyDictionary<string, TranslationEntry> Entries { get; }

        private OpenNestPack()
        {
            var d = new Dictionary<string, TranslationEntry>(System.StringComparer.Ordinal);
            void Add(string name, string zh, string desc,
                (string, string)[]? ins = null, (string, string)[]? outs = null)
            {
                var e = new TranslationEntry { Name = zh, Description = desc, Source = TranslationSource.Builtin };
                if (ins != null) foreach (var (k, v) in ins) e.Inputs[k] = v;
                if (outs != null) foreach (var (k, v) in outs) e.Outputs[k] = v;
                d[$"OpenNest_{name}"] = e;
            }

            Add("OpenNest",          "OpenNest 排样",     "在板材上排样曲线集合",
                ins: new[] { ("Curves", "待排样曲线"), ("Sheet", "板材"), ("Tolerance", "容差") },
                outs: new[] { ("NCurves", "未排样曲线"), ("Placed", "已排样曲线") });
            Add("SheetFromCorners",  "板材(角点)",   "通过两个对角点创建板材矩形",
                ins: new[] { ("A", "角点 A"), ("B", "角点 B") },
                outs: new[] { ("Sheet", "板材") });
            Add("NestingSettings",   "排样设置",     "配置排样参数(旋转、间距等)",
                ins: new[] { ("Spacing", "间距"), ("Rotations", "允许旋转数") },
                outs: new[] { ("Settings", "设置") });
            Add("TagNestingResults", "标注排样结果", "在已排样曲线上标注排样信息",
                ins: new[] { ("Placed", "已排样曲线") },
                outs: new[] { ("Tagged", "标注后曲线") });

            Entries = d;
        }
    }
}
