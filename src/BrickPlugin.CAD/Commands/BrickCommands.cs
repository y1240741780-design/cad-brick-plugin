using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.Runtime;
using Fanben.BrickPlugin.CAD.Abstractions;
using Fanben.BrickPlugin.CAD.AutoCAD;
using Fanben.BrickPlugin.Core.Algorithms;
using Fanben.BrickPlugin.Core.Models;

// 使 AutoCAD 自动发现此程序集中的命令
[assembly: CommandClass(typeof(Fanben.BrickPlugin.CAD.Commands.BrickCommands))]

namespace Fanben.BrickPlugin.CAD.Commands
{
    /// <summary>
    /// CAD 排砖插件 — 所有 AutoCAD 命令定义
    /// </summary>
    public class BrickCommands
    {
        // ============ 主命令 ============

        /// <summary>
        /// BRICK — 启动排砖主面板
        /// </summary>
        [CommandMethod("BRICK")]
        public void BrickMain()
        {
            var layerSvc = new AcadLayerService();
            EnsureAllLayers(layerSvc);

            // 打开 PaletteSet 面板
            var palette = new UI.Palettes.MainPalette();
            // 面板由 AutoCAD PaletteSet 系统托管
            _ed.WriteMessage("\n🧱 CAD 排砖插件 v1.0 — 请使用面板设置参数并开始排砖");
        }

        /// <summary>
        /// BRICKWALL — 快速墙砖排布
        /// </summary>
        [CommandMethod("BRICKWALL")]
        public void QuickWallBrick()
        {
            var layerSvc = new AcadLayerService();
            EnsureAllLayers(layerSvc);

            var selectionSvc = new AcadSelectionService();

            // 选择墙面区域
            var boundaries = selectionSvc.SelectClosedPolylines("选择墙面边界多段线（闭合）");
            if (boundaries.Count == 0)
            {
                _ed.WriteMessage("\n❌ 未选择有效的闭合多段线");
                return;
            }

            // 选择门窗洞口（可选）
            _ed.WriteMessage("\n选择门窗洞口多段线（无则按回车跳过）...");
            var openingPolys = selectionSvc.SelectOpeningPolylines("选择门窗洞口多段线");

            // 使用默认参数快速排砖
            var request = new LayoutRequest
            {
                BrickSpec = BrickSpec.Presets[0],      // 600×300
                JointSetting = new JointSetting(),
                Pattern = LayoutPattern.Straight,
                StartPointMode = StartPointMode.Center,
                AvoidanceMode = AvoidanceMode.Both,
                CutMode = CutConstraintMode.Comprehensive,
            };

            var areaAnalyzer = new AreaAnalyzer.AreaAnalyzer();
            var region = new TileRegion
            {
                Name = "墙面",
                TileType = TileType.Wall,
                BoundaryPoints = boundaries[0]
            };
            region.BoundingBox = areaAnalyzer.ComputeBoundingBox(region.BoundaryPoints);

            // 自动检测洞口
            if (openingPolys != null && openingPolys.Count > 0)
            {
                region.Openings = areaAnalyzer.DetectOpenings(region, openingPolys);
            }

            request.Regions.Add(region);

            // 执行排砖
            ExecuteLayout(request, layerSvc);
        }

        /// <summary>
        /// BRICKFLOOR — 快速地砖排布
        /// </summary>
        [CommandMethod("BRICKFLOOR")]
        public void QuickFloorBrick()
        {
            var layerSvc = new AcadLayerService();
            EnsureAllLayers(layerSvc);

            var selectionSvc = new AcadSelectionService();

            // 选择地面区域
            var boundaries = selectionSvc.SelectClosedPolylines("选择地面边界多段线（闭合）");
            if (boundaries.Count == 0)
            {
                _ed.WriteMessage("\n❌ 未选择有效的闭合多段线");
                return;
            }

            // 使用默认参数快速排砖
            var request = new LayoutRequest
            {
                BrickSpec = BrickSpec.Presets[2],      // 800×800
                JointSetting = new JointSetting(),
                Pattern = LayoutPattern.Straight,
                StartPointMode = StartPointMode.Center,
                AvoidanceMode = AvoidanceMode.None,     // 地砖通常不需要避让
                CutMode = CutConstraintMode.Comprehensive,
            };

            var areaAnalyzer = new AreaAnalyzer.AreaAnalyzer();
            var region = new TileRegion
            {
                Name = "地面",
                TileType = TileType.Floor,
                BoundaryPoints = boundaries[0]
            };
            region.BoundingBox = areaAnalyzer.ComputeBoundingBox(region.BoundingBox);
            request.Regions.Add(region);

            // 执行排砖
            ExecuteLayout(request, layerSvc);
        }

        /// <summary>
        /// BRICKTABLE — 导出材料统计表
        /// </summary>
        [CommandMethod("BRICKTABLE")]
        public void ExportTable()
        {
            _ed.WriteMessage("\n📊 材料统计表导出功能");
            _ed.WriteMessage("\n请通过主面板 (BRICK命令) 执行排砖后自动生成统计表");
            _ed.WriteMessage("\n或使用 BRICK 命令重新排砖");
        }

        /// <summary>
        /// BRICKHELP — 帮助信息
        /// </summary>
        [CommandMethod("BRICKHELP")]
        public void ShowHelp()
        {
            _ed.WriteMessage("\n╔══════════════════════════════════════════════╗");
            _ed.WriteMessage("\n║      🧱 CAD 排砖插件 v1.0 — 帮助            ║");
            _ed.WriteMessage("\n╠══════════════════════════════════════════════╣");
            _ed.WriteMessage("\n║  BRICK       — 启动排砖主面板                ║");
            _ed.WriteMessage("\n║  BRICKWALL   — 快速墙砖排布                  ║");
            _ed.WriteMessage("\n║  BRICKFLOOR  — 快速地砖排布                  ║");
            _ed.WriteMessage("\n║  BRICKTABLE  — 导出材料统计表                ║");
            _ed.WriteMessage("\n║  BRICKHELP   — 显示此帮助                    ║");
            _ed.WriteMessage("\n╠══════════════════════════════════════════════╣");
            _ed.WriteMessage("\n║  铺贴方式: 直铺 | 工字 | 人字 | 斜铺        ║");
            _ed.WriteMessage("\n║  砖规格: 10种预置 + 自定义                   ║");
            _ed.WriteMessage("\n║  支持: 墙砖 / 地砖 / 门窗避让 / 材料统计     ║");
            _ed.WriteMessage("\n║  兼容: AutoCAD 2010–2026                     ║");
            _ed.WriteMessage("\n╚══════════════════════════════════════════════╝");
        }

        // ============ 内部方法 ============

        private static Autodesk.AutoCAD.EditorInput.Editor _ed =>
            Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager
                .MdiActiveDocument.Editor;

        /// <summary>
        /// 确保所有插件图层存在
        /// </summary>
        private void EnsureAllLayers(AcadLayerService layerSvc)
        {
            layerSvc.EnsureLayer(AcadDrawingService.Layers.Outline, 7);   // 白色
            layerSvc.EnsureLayer(AcadDrawingService.Layers.Hatch, 8);    // 灰色
            layerSvc.EnsureLayer(AcadDrawingService.Layers.Joint, 9);    // 浅灰
            layerSvc.EnsureLayer(AcadDrawingService.Layers.Cut, 1);      // 红色
            layerSvc.EnsureLayer(AcadDrawingService.Layers.Annotation, 3); // 绿色
            layerSvc.EnsureLayer(AcadDrawingService.Layers.Avoidance, 2);  // 黄色
        }

        /// <summary>
        /// 执行排砖、绘图、统计、导出全流程
        /// </summary>
        private void ExecuteLayout(LayoutRequest request, AcadLayerService layerSvc)
        {
            var engine = new BrickLayoutEngine();
            var drawingSvc = new AcadDrawingService();

            _ed.WriteMessage($"\n⏳ 正在计算排砖方案... 砖规格: {request.BrickSpec.Name}, 方式: {request.Pattern}");

            // 执行排砖
            var result = engine.Execute(request);

            if (!result.Success)
            {
                _ed.WriteMessage($"\n❌ 排砖失败: {result.ErrorMessage}");
                return;
            }

            _ed.WriteMessage($"\n✅ 排砖完成! 共 {result.Placements.Count} 块砖, 耗时 {result.ExecutionTimeMs}ms");

            // 绘制结果
            var presentation = request.Presentation;
            if (presentation.HasFlag(PresentationStyle.Outline))
            {
                _ed.WriteMessage("\n   绘制轮廓线...");
                drawingSvc.DrawBrickOutlines(result.Placements, AcadDrawingService.Layers.Outline);
            }

            if (presentation.HasFlag(PresentationStyle.Block))
            {
                _ed.WriteMessage("\n   创建实体块...");
                int blockIdx = 0;
                foreach (var placement in result.Placements)
                {
                    drawingSvc.CreateBrickBlock(placement,
                        $"BRICK_{blockIdx++}", AcadDrawingService.Layers.Outline);
                }
            }

            if (presentation.HasFlag(PresentationStyle.Hatch))
            {
                _ed.WriteMessage("\n   填充图案...");
                foreach (var placement in result.Placements)
                {
                    drawingSvc.CreateBrickHatch(placement,
                        "ANSI31", AcadDrawingService.Layers.Hatch);
                }
            }

            // 绘制避让区域标记
            foreach (var region in request.Regions)
            {
                if (region.Openings.Count > 0)
                {
                    drawingSvc.DrawOpeningMarkers(region.Openings,
                        AcadDrawingService.Layers.Avoidance);
                }
            }

            // 材料统计表
            var summary = result.MaterialSummary;
            _ed.WriteMessage($"\n📊 材料统计: {summary.SpecName} | " +
                $"整砖:{summary.WholeBrickCount} | 切割:{summary.CutBrickCount} | " +
                $"总量:{summary.TotalCount} | 损耗:{summary.WasteRate:F1}%");

            // 在图中插入统计表
            var insertPt = new Point2D(
                request.Regions[0].BoundingBox.MaxX + 500,
                request.Regions[0].BoundingBox.MaxY);
            drawingSvc.CreateMaterialTable(insertPt, summary);

            // 导出 Excel
            string docPath = new AcadSelectionService().GetCurrentDocumentPath();
            string dir = Path.GetDirectoryName(docPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string excelPath = Path.Combine(dir, $"排砖统计_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

            if (drawingSvc.ExportToExcel(result, excelPath))
            {
                _ed.WriteMessage($"\n📁 Excel 导出: {excelPath}");
            }
            else
            {
                _ed.WriteMessage($"\n⚠️ Excel 导出失败, 改为 CSV 格式");
            }
        }

        /// <summary>
        /// BRICK3D — 3D 排砖（墙砖 Solid3d + 地砖拉伸体）
        /// </summary>
        [CommandMethod("BRICK3D")]
        public void Brick3D()
        {
            var layerSvc = new AcadLayerService();
            EnsureAllLayers(layerSvc);
            layerSvc.EnsureLayer(Acad3DDrawingService.Layers3D.Wall3D, 150);   // 蓝色
            layerSvc.EnsureLayer(Acad3DDrawingService.Layers3D.Floor3D, 40);   // 橙色
            layerSvc.EnsureLayer(Acad3DDrawingService.Layers3D.Opening3D, 2);  // 黄色

            var selectionSvc = new AcadSelectionService();
            var drawing3D = new Acad3DDrawingService();

            _ed.WriteMessage("\n🧱 3D 排砖模式");
            _ed.WriteMessage("\n   墙砖 → Solid3d 实体 | 地砖 → 拉伸体");

            // 选择墙面区域
            var boundaries = selectionSvc.SelectClosedPolylines("\n选择墙面的 2D 边界多段线（闭合）");
            if (boundaries.Count == 0)
            {
                _ed.WriteMessage("\n❌ 未选择墙面边界");
                return;
            }

            // 输入墙高
            var heightOpts = new Autodesk.AutoCAD.EditorInput.PromptDoubleOptions("\n输入墙面高度(mm) [默认 2800]: ");
            heightOpts.DefaultValue = 2800;
            heightOpts.AllowNone = true;
            var heightRes = _ed.GetDouble(heightOpts);
            double wallHeight = heightRes.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK
                ? heightRes.Value : 2800;

            // 输入墙厚
            var thickOpts = new Autodesk.AutoCAD.EditorInput.PromptDoubleOptions("\n输入墙面厚度(mm) [默认 240]: ");
            thickOpts.DefaultValue = 240;
            thickOpts.AllowNone = true;
            var thickRes = _ed.GetDouble(thickOpts);
            double wallThick = thickRes.Status == Autodesk.AutoCAD.EditorInput.PromptStatus.OK
                ? thickRes.Value : 240;

            // 选洞口
            _ed.WriteMessage("\n选择门窗洞口多段线（无则按 ESC 跳过）...");
            var openingPolys = selectionSvc.SelectOpeningPolylines("选择洞口多段线");

            // 构建区域
            var areaAnalyzer = new AreaAnalyzer.AreaAnalyzer();
            var wallRegion = new Wall3DRegion
            {
                Name = "3D墙面",
                BoundaryPoints = boundaries[0],
                WallHeight = wallHeight,
                WallThickness = wallThick,
                VerticalStart = VerticalStartMode.Bottom
            };
            wallRegion.BoundingBox = areaAnalyzer.ComputeBoundingBox(wallRegion.BoundaryPoints);

            // 转换洞口为 3D
            if (openingPolys != null && openingPolys.Count > 0)
            {
                foreach (var poly in openingPolys)
                {
                    var box = areaAnalyzer.ComputeBoundingBox(poly);
                    wallRegion.Openings3D.Add(new Opening3DInfo
                    {
                        Name = $"洞口{wallRegion.Openings3D.Count + 1}",
                        Boundary = box,
                        BottomZ = 0,    // 默认从地面起
                        TopZ = box.Height > 1500 ? 2100 : box.Height,
                        Type = box.Height > 1500 ? "门" : "窗"
                    });
                }
            }

            // 构建请求
            var request3D = new Layout3DRequest
            {
                Layout2D = new LayoutRequest
                {
                    BrickSpec = BrickSpec.Presets[0], // 600×300
                    JointSetting = new JointSetting(),
                    Pattern = LayoutPattern.Straight,
                    StartPointMode = StartPointMode.Center
                },
                WallRegions = new List<Wall3DRegion> { wallRegion },
                VerticalStart = VerticalStartMode.Bottom,
                PreciseTrim = false
            };

            _ed.WriteMessage($"\n⏳ 3D排砖计算中... 墙高:{wallHeight}mm 墙厚:{wallThick}mm");

            // 执行
            var engine = new Wall3DLayoutEngine();
            var result = engine.Execute(request3D);

            if (!result.Success)
            {
                _ed.WriteMessage($"\n❌ 3D排砖失败: {result.ErrorMessage}");
                return;
            }

            _ed.WriteMessage($"\n✅ 3D排砖完成! 墙砖:{result.WallPlacements.Count}块, 耗时{result.ExecutionTimeMs}ms");

            // 绘制墙砖 Solid3d
            _ed.WriteMessage("\n   创建墙砖 3D 实体...");
            drawing3D.CreateWallBrickSolids(result.WallPlacements, Acad3DDrawingService.Layers3D.Wall3D);

            // 如果启用精确修剪且有洞口
            if (request3D.PreciseTrim && wallRegion.Openings3D.Count > 0)
            {
                _ed.WriteMessage("\n   精确修剪洞口（布尔减法）...");
                drawing3D.PreciseTrimWallBricks(result.WallPlacements,
                    wallRegion.Openings3D, Acad3DDrawingService.Layers3D.Wall3D);
            }

            // 地砖（如果有）
            if (result.FloorResult != null && result.FloorResult.Placements.Count > 0)
            {
                _ed.WriteMessage("\n   创建地砖 3D 拉伸体...");
                double floorThickness = request3D.Layout2D.BrickSpec.Thickness;
                drawing3D.CreateFloorBrickExtrusions(result.FloorResult.Placements,
                    floorThickness, Acad3DDrawingService.Layers3D.Floor3D);
            }

            // 统计
            var summary = result.MaterialSummary;
            _ed.WriteMessage($"\n📊 3D材料统计: {summary.SpecName} | " +
                $"整砖:{summary.WholeBrickCount} | 切割:{summary.CutBrickCount} | " +
                $"总体积:{summary.TotalVolume:F3}m³ | 损耗:{summary.WasteRate:F1}%");

            _ed.WriteMessage("\n💡 提示: 使用 VSCURRENT 切换到 3D 视图查看效果");
            _ed.WriteMessage("\n   _vscurrent → 选择 '真实' 或 '概念' 视觉样式");
        }
    }
}
