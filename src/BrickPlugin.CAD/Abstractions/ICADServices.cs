using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.CAD.Abstractions
{
    /// <summary>
    /// CAD 绘图服务抽象接口 — 与具体 CAD 平台解耦
    /// 方便未来扩展到中望 CAD / 浩辰 CAD
    /// </summary>
    public interface ICADDrawingService
    {
        /// <summary>绘制单砖轮廓多段线</summary>
        void DrawBrickOutline(BrickPlacement placement, string layerName);

        /// <summary>批量绘制砖块轮廓</summary>
        void DrawBrickOutlines(List<BrickPlacement> placements, string layerName);

        /// <summary>创建砖块实体 (BlockReference)</summary>
        void CreateBrickBlock(BrickPlacement placement, string blockName, string layerName);

        /// <summary>创建填充图案 (Hatch)</summary>
        void CreateBrickHatch(BrickPlacement placement, string patternName, string layerName);

        /// <summary>绘制灰缝线</summary>
        void DrawJointLines(double x, double y, double length, double jointWidth, string layerName);

        /// <summary>绘制区域边界</summary>
        void DrawRegionBoundary(List<Point2D> boundaryPoints, string layerName);

        /// <summary>绘制门窗洞口标记</summary>
        void DrawOpeningMarkers(List<OpeningInfo> openings, string layerName);

        /// <summary>创建标注文本</summary>
        void CreateAnnotation(Point2D position, string text, double height, string layerName);

        /// <summary>创建材料统计表 (AutoCAD Table)</summary>
        void CreateMaterialTable(Point2D insertPoint, MaterialSummary summary);

        /// <summary>导出材料表到 Excel</summary>
        bool ExportToExcel(LayoutResult result, string filePath);
    }

    /// <summary>
    /// CAD 选择服务抽象接口
    /// </summary>
    public interface ICADSelectionService
    {
        /// <summary>让用户选择闭合多段线作为排砖区域</summary>
        List<List<Point2D>> SelectClosedPolylines(string prompt);

        /// <summary>让用户选择一个起铺点</summary>
        Point2D? PickPoint(string prompt);

        /// <summary>让用户选择门窗洞口的边界多段线</summary>
        List<List<Point2D>> SelectOpeningPolylines(string prompt);

        /// <summary>获取当前文档路径</summary>
        string GetCurrentDocumentPath();
    }

    /// <summary>
    /// CAD 图层服务抽象接口
    /// </summary>
    public interface ICADLayerService
    {
        /// <summary>创建或获取图层</summary>
        void EnsureLayer(string layerName, short colorIndex = 7);

        /// <summary>锁定/解锁图层</summary>
        void SetLayerLocked(string layerName, bool locked);

        /// <summary>冻结/解冻图层</summary>
        void SetLayerFrozen(string layerName, bool frozen);

        /// <summary>设置当前图层</summary>
        void SetCurrentLayer(string layerName);
    }
}
