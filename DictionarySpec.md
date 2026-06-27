# GH-AITranslator-Pro — Dictionary Specification

This document defines the **publish-quality** rules for the `BuiltinSeed` dictionary
that ships inside the plugin. Every entry is reviewed against this checklist before
merge.

## Schema

Every entry in `BuiltinSeed.cs` must contain exactly five non-empty fields:

| Field         | Type   | Meaning                                           |
|---------------|--------|---------------------------------------------------|
| `Key`         | string | `"Native_<ClassName>"`, e.g. `"Native_Point"`     |
| `Name`        | string | Chinese display name, e.g. `点`                   |
| `NickName`    | string | Chinese port label, max **2 chars**               |
| `Description` | string | Chinese hover-tip text, **must end with `。`**    |
| `Category`    | string | Chinese tab name, e.g. `参数` / `几何` / `数学`    |

The original English values live in the source comment (`// EN: Point`) so future
maintainers can audit the translation.

## Quality rules

1. **Industry-standard terminology.** GH has well-known Chinese translations in the
   AEC industry. Use them — never invent. Example fixed mappings:
   - `Loft` → `放样` (not `阁楼`)
   - `Sweep` → `扫掠` (not `扫除`)
   - `Boolean` → `布尔运算`
   - `Nurbs` → `NURBS` (no translation; preserves recognition)
   - `Mesh` → `网格`
   - `Domain` → `域`
   - `Interval` → `区间`
   - `Dispatch` → `分流`
   - `Cull` → `筛除`
   - `Tween` → `插值`
   - `Morph` → `变形`
   - `Splop` → `贴附`
   - `Rebuild` → `重建`

2. **NickName length ≤ 2 Chinese characters.** Battery port labels are tiny.
   `合并` not `合并所有`, `域` not `域值`.

3. **Description ends with `。`.** Period-style sentence close, not `!` or `.` or no
   punctuation. Multi-sentence OK.

4. **No placeholder.** No `TBD`, `TODO`, `null`, empty string.

5. **No English in Name/NickName/Description/Category.** Exception: technical proper
   nouns (NURBS, B-rep, XYZ) are preserved.

## Three-language display rules (LanguageFormatter)

| Mode        | Name            | NickName       | Description          |
|-------------|-----------------|----------------|----------------------|
| Chinese     | `点`            | `点`           | `三维空间中的一个点。` |
| Bilingual   | `点\|Point`     | `点\|P`        | `三维空间中的一个点。` |
| English     | `Point`         | `P`            | `A point in 3D space.` |

The `|` separator is **half-width pipe** (U+007C), with no whitespace around it.
The English Name/Description are stored in `NameEn` / `DescriptionEn` fields so
the formatter can render either side independently.

## Categories (panel group names)

The `Category` field on a component is the **tab** name under which it appears
in the Grasshopper ribbon. GH has roughly 12 top-level tabs:

| English       | Chinese |
|---------------|---------|
| Params        | 参数     |
| Geometry      | 几何     |
| Maths         | 数学     |
| Vector        | 向量     |
| Curve         | 曲线     |
| Surface       | 曲面     |
| Mesh          | 网格     |
| Intersect     | 相交     |
| Transform     | 变换     |
| Display       | 显示     |
| Logic         | 逻辑     |
| Script        | 脚本     |
| Input         | 输入     |
| Output        | 输出     |
| Sets          | 集合     |
| Tree          | 树形数据 |
| Special       | 特殊     |

All entries in `BuiltinSeed` use these exact Chinese strings.

## Coverage bar

Before declaring a release complete, `BuiltinSeed` must contain entries for
**at minimum** the 250 most-used GH 8.25 built-in components, organized by tab.
Test `BuiltinSeedCoverageTests` enforces this minimum count.