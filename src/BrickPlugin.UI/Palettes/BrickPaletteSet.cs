using System;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;

namespace Fanben.BrickPlugin.UI.Palettes
{
    /// <summary>
    /// AutoCAD PaletteSet 包装器 — 将 WinForm 面板集成为可停靠面板
    /// </summary>
    public class BrickPaletteSet
    {
        private static PaletteSet _paletteSet;
        private static MainPalette _mainPalette;
        private static bool _isShown = false;

        /// <summary>
        /// 显示/切换面板可见性
        /// </summary>
        public static void Show()
        {
            if (_isShown && _paletteSet != null)
            {
                _paletteSet.Visible = !_paletteSet.Visible;
                return;
            }

            _paletteSet = new PaletteSet("🧱 CAD 排砖插件", new Guid("B1C2D3E4-5000-4000-9000-000000000001"))
            {
                Style = PaletteSetStyles.NameEditable |
                        PaletteSetStyles.ShowPropertiesMenu |
                        PaletteSetStyles.ShowAutoHideButton |
                        PaletteSetStyles.ShowCloseButton,
                MinimumSize = new System.Drawing.Size(300, 400),
                TitleBarLocation = PaletteSetTitleBarLocation.Left
            };

            _mainPalette = new MainPalette();
            _mainPalette.StartClicked += OnStartBrickLayout;

            _paletteSet.Add("排砖设置", _mainPalette);
            _paletteSet.Visible = true;
            _paletteSet.Size = new System.Drawing.Size(300, 750);
            _isShown = true;

            // 面板关闭时清理
            _paletteSet.VisibleChanged += (s, e) =>
            {
                if (!_paletteSet.Visible && _paletteSet != null)
                {
                    _isShown = false;
                }
            };
        }

        /// <summary>
        /// 面板中点击"开始排砖"时触发
        /// </summary>
        private static void OnStartBrickLayout(object sender, EventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            try
            {
                var request = _mainPalette.BuildRequest();

                // 选择闭合多段线
                ed.WriteMessage("\n选择排砖区域边界多段线（闭合）...");
                var selSvc = new CAD.AutoCAD.AcadSelectionService();
                var boundaries = selSvc.SelectClosedPolylines("选择区域边界多段线");

                if (boundaries.Count == 0)
                {
                    ed.WriteMessage("\n❌ 未选择有效的闭合多段线");
                    return;
                }

                // 如果是墙砖且开启了避让，选择门窗洞口
                if (request.AvoidanceMode != Core.Models.AvoidanceMode.None)
                {
                    ed.WriteMessage("\n选择门窗洞口多段线（无则按 ESC 跳过）...");
                    var openings = selSvc.SelectOpeningPolylines("选择门窗洞口多段线");
                    if (openings != null && openings.Count > 0)
                    {
                        var areaAnalyzer = new Core.AreaAnalyzer.AreaAnalyzer();
                        foreach (var region in request.Regions)
                        {
                            region.Openings = areaAnalyzer.DetectOpenings(region, openings);
                        }
                    }
                }

                // 构建区域
                var region = new Core.Models.TileRegion
                {
                    Name = "排砖区域",
                    TileType = Core.Models.TileType.Floor,
                    BoundaryPoints = boundaries[0]
                };
                var areaAnalyzer2 = new Core.AreaAnalyzer.AreaAnalyzer();
                region.BoundingBox = areaAnalyzer2.ComputeBoundingBox(region.BoundaryPoints);
                request.Regions.Add(region);

                // 执行排砖（委托给 BrickCommands 中的核心逻辑）
                var engine = new Core.Algorithms.BrickLayoutEngine();
                var result = engine.Execute(request);

                if (!result.Success)
                {
                    ed.WriteMessage($"\n❌ 排砖失败: {result.ErrorMessage}");
                    return;
                }

                ed.WriteMessage($"\n✅ 排砖完成! 共 {result.Placements.Count} 块砖, 耗时 {result.ExecutionTimeMs}ms");

                // 绘制
                var drawingSvc = new CAD.AutoCAD.AcadDrawingService();
                var layerSvc = new CAD.AutoCAD.AcadLayerService();
                EnsureLayers(layerSvc);

                if (request.Presentation.HasFlag(Core.Models.PresentationStyle.Outline))
                    drawingSvc.DrawBrickOutlines(result.Placements, CAD.AutoCAD.AcadDrawingService.Layers.Outline);

                if (request.Presentation.HasFlag(Core.Models.PresentationStyle.Block))
                {
                    int idx = 0;
                    foreach (var p in result.Placements)
                        drawingSvc.CreateBrickBlock(p, $"B_{idx++}", CAD.AutoCAD.AcadDrawingService.Layers.Outline);
                }

                if (request.Presentation.HasFlag(Core.Models.PresentationStyle.Hatch))
                    foreach (var p in result.Placements)
                        drawingSvc.CreateBrickHatch(p, "ANSI31", CAD.AutoCAD.AcadDrawingService.Layers.Hatch);

                // 统计表
                var summary = result.MaterialSummary;
                ed.WriteMessage($"\n📊 {summary.SpecName} | 整:{summary.WholeBrickCount} 切:{summary.CutBrickCount} | 损耗:{summary.WasteRate:F1}%");

                var insertPt = new Core.Models.Point2D(
                    region.BoundingBox.MaxX + 500, region.BoundingBox.MaxY);
                drawingSvc.CreateMaterialTable(insertPt, summary);

                // Excel
                string excelPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"排砖统计_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                drawingSvc.ExportToExcel(result, excelPath);
                ed.WriteMessage($"\n📁 导出: {excelPath}");
            }
            catch (Exception ex)
            {
                ed.WriteMessage($"\n❌ 排砖异常: {ex.Message}");
            }
        }

        private static void EnsureLayers(CAD.AutoCAD.AcadLayerService layerSvc)
        {
            layerSvc.EnsureLayer(CAD.AutoCAD.AcadDrawingService.Layers.Outline, 7);
            layerSvc.EnsureLayer(CAD.AutoCAD.AcadDrawingService.Layers.Hatch, 8);
            layerSvc.EnsureLayer(CAD.AutoCAD.AcadDrawingService.Layers.Joint, 9);
            layerSvc.EnsureLayer(CAD.AutoCAD.AcadDrawingService.Layers.Cut, 1);
            layerSvc.EnsureLayer(CAD.AutoCAD.AcadDrawingService.Layers.Annotation, 3);
            layerSvc.EnsureLayer(CAD.AutoCAD.AcadDrawingService.Layers.Avoidance, 2);
        }
    }
}
