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
    }
}
