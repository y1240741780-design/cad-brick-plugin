# 🧱 CAD 排砖插件 v1.1

> AutoCAD .NET 排砖插件 — 墙砖/地砖自动排版 + 3D可视化 + 材料统计 + Excel 导出
> 兼容 AutoCAD 2010–2026 | C# 开发 | Hermes Agent 驱动

---

## 功能

| 功能 | 说明 |
|------|------|
| 🔲 墙砖排布 | 沿墙面/立面排布，支持门窗洞口自动避让 |
| ⬜ 地砖排布 | 封闭区域内填满，支持任意多边形边界 |
| 📐 4种铺贴方式 | 直铺 · 工字铺 · 人字铺 · 斜铺 |
| 🎯 3种起铺点 | 居中 · 角落 · 手动指定 |
| ✂️ 切割优化 | 最小尺寸约束 + 优先整砖 + 切割前确认 |
| 🚪 门窗避让 | 自动检测 + 手动指定避让区域 |
| 🎨 3种呈现 | 轮廓线 · 实体块 · Hatch 填充图案 |
| 📊 材料统计 | 整砖/切割砖/损耗率 + AutoCAD 表格 |
| 📁 Excel 导出 | 3 Sheet（汇总/切割明细/排版数据）|
| 🖥️ 交互方式 | 命令行 (BRICK) + PaletteSet 可停靠面板 |
| 🧊 **3D 可视化** | **墙砖 Solid3d 实体 + 地砖拉伸体 + 布尔减法挖洞** |

## 新增：3D 可视化（v1.1）

| 特性 | 说明 |
|------|------|
| 墙砖 3D | Solid3d.CreateBox() 创建立方体实体 |
| 地砖 3D | Polyline.Thickness 拉伸体（轻量快速） |
| 精确修剪 | 布尔减法从砖块中挖掉门窗洞口 |
| Z轴起铺 | 从地面往上 / 从顶部往下 / 居中对称 |
| 3D 视图 | VSCURRENT → "真实" 或 "概念" 视觉样式 |

## 使用命令
| 命令 | 功能 |
|------|------|
| `BRICK` | 打开排砖设置面板（含3D设置区） |
| `BRICKWALL` | 快速墙砖排布 |
| `BRICKFLOOR` | 快速地砖排布 |
| `BRICK3D` | **3D 排砖（墙砖 Solid3d + 地砖拉伸体）** |
| `BRICKTABLE` | 导出材料统计表 |
| `BRICKHELP` | 显示帮助信息 |
### 使用命令
| 命令 | 功能 |
|------|------|
| `BRICK` | 打开排砖设置面板 |
| `BRICKWALL` | 快速墙砖排布（默认参数） |
| `BRICKFLOOR` | 快速地砖排布（默认参数） |
| `BRICKTABLE` | 导出材料统计表 |
| `BRICKHELP` | 显示帮助信息 |

## 项目架构

```
src/
├── BrickPlugin.Core/     # 核心算法（.NET Standard 2.0）
├── BrickPlugin.Data/     # 材料统计 + Excel 导出
├── BrickPlugin.CAD/      # AutoCAD 适配层
└── BrickPlugin.UI/       # PaletteSet 面板
```

详见 [架构设计](docs/架构设计.md)

## 构建要求

- Visual Studio 2022
- AutoCAD 2015+ SDK（ObjectARX）
- .NET Framework 4.8 + .NET 8.0

```bash
dotnet restore BrickPlugin.sln
dotnet build BrickPlugin.sln -c Release
```

## 配置文件

在 [配置/](配置/) 目录下：
- `config.json` — 插件设置
- `砖规格预设.json` — 10种常用砖规格
- `灰缝预设.json` — 4种灰缝方案

## 许可证

MIT License
