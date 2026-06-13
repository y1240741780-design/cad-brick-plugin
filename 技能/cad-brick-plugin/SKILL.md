---
name: cad-brick-plugin
description: "AutoCAD .NET 排砖插件开发 — 墙砖/地砖排版、材料统计、C# 架构"
version: 1.0.0
author: Hermes Agent
license: MIT
platforms: [windows]
---

# CAD 排砖插件

AutoCAD 2010–2026 全系列 .NET 排砖插件。由 Hermes Agent 驱动开发。

## 触发条件

- 用户说"排砖"、"CAD 铺砖"、"砖排版"、"brick layout"
- 用户想在 AutoCAD 中自动排列墙砖/地砖
- 用户需要修改/扩展/调试此插件

## 项目结构

```
D:\fanben\CAD排砖插件\
├── src/
│   ├── BrickPlugin.Core/       # 核心算法（.NET Standard 2.0）
│   ├── BrickPlugin.Data/       # 材料统计 + Excel 导出
│   ├── BrickPlugin.CAD/        # AutoCAD 适配层（net48;net8.0）
│   └── BrickPlugin.UI/         # PaletteSet 面板
├── 配置/                       # 配置文件
├── 模板/                       # 铺贴图案模板
├── 知识库/                     # 参考文档
└── BrickPlugin.sln
```

## 架构分层

1. **Core 层** — 纯算法，零 CAD 依赖
   - Models：BrickSpec, JointSetting, LayoutRequest, LayoutResult
   - Algorithms：StraightPattern, BrickBondPattern, HerringbonePattern, DiagonalPattern
   - CutOptimizer：最小切割尺寸约束、优先整砖
   - AreaAnalyzer：边界处理、洞口检测（射线法）
   - StartPointResolver：居中/角落/手动起铺

2. **Data 层** — 材料统计 + ClosedXML Excel 导出
   - MaterialCalculator：按规格分组统计
   - ExcelExporter：3 Sheet（汇总/切割明细/排版数据）

3. **CAD 层** — AutoCAD .NET API 实现
   - AcadDrawingService：绘制轮廓线/实体块/填充图案/表格/标注
   - AcadSelectionService：选择闭合多段线/点选
   - AcadLayerService：图层管理
   - BrickCommands：BRICK/BRICKWALL/BRICKFLOOR/BRICKTABLE/BRICKHELP

4. **UI 层** — PaletteSet 可停靠面板
   - MainPalette：参数设置面板（WinForm）
   - BrickPaletteSet：AutoCAD PaletteSet 集成

## 关键技术决策

| 项目 | 选择 | 原因 |
|------|------|------|
| Core 框架 | .NET Standard 2.0 | 兼容 net48 + net8.0 |
| CAD 多目标 | net48;net8.0-windows | 覆盖 2015-2026 |
| Excel 库 | ClosedXML 0.102.2 | MIT 许可证 |
| 策略模式 | IPatternStrategy | 每种铺贴方式独立实现 |
| 图层命名 | BRICK-* 前缀 | 清理方便，不冲突 |

## 构建命令

```bash
# 使用 MSBuild 构建（需安装 AutoCAD SDK）
cd "D:\fanben\CAD排砖插件"
dotnet restore BrickPlugin.sln
dotnet build BrickPlugin.sln -c Release
```

## AutoCAD 加载方式

```
NETLOAD → 选择 BrickPlugin.CAD.dll
命令: BRICK / BRICKWALL / BRICKFLOOR / BRICKTABLE / BRICKHELP
```

## Pitfalls

- AutoCAD .NET DLL 引用路径需匹配实际安装版本
- 2010–2014（.NET 3.5/4.0）需要单独编译，Core 需降级到 net40
- PaletteSet 在 AutoCAD 2010–2012 行为略有差异（需条件编译）
- ClosedXML 要求 .NET Framework 4.6.1+，2010–2014 用 CSV 回退
