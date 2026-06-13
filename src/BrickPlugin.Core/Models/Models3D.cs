using System;

namespace Fanben.BrickPlugin.Core.Models
{
    /// <summary>
    /// 垂直（Z轴）起铺模式
    /// </summary>
    public enum VerticalStartMode
    {
        /// <summary>从地面往上排 — Z=0 整砖起步，顶部切割</summary>
        Bottom = 0,

        /// <summary>从顶往下排 — 墙顶整砖收口，底部切割</summary>
        Top = 1,

        /// <summary>居中排 — 上下对称切割</summary>
        Center = 2
    }

    /// <summary>
    /// 3D 点
    /// </summary>
    public struct Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z = 0)
        { X = x; Y = y; Z = z; }

        public static Point3D operator +(Point3D a, Point3D b) =>
            new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }

    /// <summary>
    /// 3D 砖块放置结果（墙砖用）
    /// </summary>
    public class Brick3DPlacement
    {
        /// <summary>砖编号</summary>
        public int Id { get; set; }

        /// <summary>排砖区域名称</summary>
        public string RegionName { get; set; }

        /// <summary>砖左下角 3D 位置</summary>
        public Point3D Position { get; set; }

        /// <summary>绕 Z 轴旋转角度（度）</summary>
        public double Rotation { get; set; }

        /// <summary>实际长度（切割后，沿 X）</summary>
        public double ActualLength { get; set; }

        /// <summary>实际宽度（切割后，沿 Z = 高度方向）</summary>
        public double ActualHeight { get; set; }

        /// <summary>实际厚度（沿 Y = 墙厚方向）</summary>
        public double ActualThickness { get; set; }

        /// <summary>原始砖规格</summary>
        public BrickSpec OriginalSpec { get; set; }

        /// <summary>是否为切割砖</summary>
        public bool IsCut => Math.Abs(ActualLength - OriginalSpec.Length) > 0.01 ||
                              Math.Abs(ActualHeight - OriginalSpec.Width) > 0.01;

        /// <summary>水平行索引</summary>
        public int RowX { get; set; }

        /// <summary>垂直行索引（Z轴方向）</summary>
        public int RowZ { get; set; }

        /// <summary>是否为整砖</summary>
        public bool IsWhole => !IsCut;

        /// <summary>砖的体积（mm³）</summary>
        public double Volume => ActualLength * ActualHeight * ActualThickness;

        public override string ToString() =>
            $"3D砖#{Id} [{RegionName}] @{Position} {ActualLength:F0}×{ActualHeight:F0}×{ActualThickness:F0} " +
            $"{(IsCut ? "(切割)" : "(整砖)")}";
    }

    /// <summary>
    /// 3D 墙面区域（含高度参数）
    /// </summary>
    public class Wall3DRegion
    {
        /// <summary>区域名称</summary>
        public string Name { get; set; }

        /// <summary>2D 边界点（墙面在 XY 平面的投影）</summary>
        public System.Collections.Generic.List<Point2D> BoundaryPoints { get; set; }
            = new System.Collections.Generic.List<Point2D>();

        /// <summary>包围盒</summary>
        public Rect2D BoundingBox { get; set; }

        /// <summary>墙面总高度（mm）</summary>
        public double WallHeight { get; set; } = 2800;

        /// <summary>底部偏移（从 Z=0 起，mm）</summary>
        public double BottomOffset { get; set; } = 0;

        /// <summary>墙面厚度（mm，砖的厚度方向）</summary>
        public double WallThickness { get; set; } = 240;

        /// <summary>垂直起铺模式</summary>
        public VerticalStartMode VerticalStart { get; set; } = VerticalStartMode.Bottom;

        /// <summary>3D 门窗洞口列表</summary>
        public System.Collections.Generic.List<Opening3DInfo> Openings3D { get; set; }
            = new System.Collections.Generic.List<Opening3DInfo>();

        /// <summary>墙体起点 Z（BottomOffset 换算后）</summary>
        public double BaseZ => BottomOffset;
        /// <summary>墙体终点 Z</summary>
        public double TopZ => BottomOffset + WallHeight;
    }

    /// <summary>
    /// 3D 门窗洞口信息
    /// </summary>
    public class Opening3DInfo
    {
        /// <summary>洞口名称</summary>
        public string Name { get; set; }

        /// <summary>洞口在 XY 平面的边界</summary>
        public Rect2D Boundary { get; set; }

        /// <summary>洞口底标高（Z）</summary>
        public double BottomZ { get; set; }

        /// <summary>洞口顶标高（Z）</summary>
        public double TopZ { get; set; }

        /// <summary>洞口高度</summary>
        public double Height => TopZ - BottomZ;

        /// <summary>洞口类型</summary>
        public string Type { get; set; } = "洞口";

        /// <summary>3D 包围盒（用于碰撞检测）</summary>
        public bool Contains3D(double x, double y, double z)
        {
            if (z < BottomZ || z > TopZ) return false;
            return Boundary.Contains(new Point2D(x, y));
        }

        public override string ToString() =>
            $"{Type}[{Name}] XY:{Boundary} Z:[{BottomZ:F0}→{TopZ:F0}]";
    }

    /// <summary>
    /// 3D 排砖请求
    /// </summary>
    public class Layout3DRequest
    {
        /// <summary>2D 排砖请求（水平方向参数）</summary>
        public LayoutRequest Layout2D { get; set; } = new LayoutRequest();

        /// <summary>3D 墙面区域列表</summary>
        public System.Collections.Generic.List<Wall3DRegion> WallRegions { get; set; }
            = new System.Collections.Generic.List<Wall3DRegion>();

        /// <summary>垂直起铺模式</summary>
        public VerticalStartMode VerticalStart { get; set; } = VerticalStartMode.Bottom;

        /// <summary>是否启用精确修剪（布尔减法挖洞）</summary>
        public bool PreciseTrim { get; set; } = false;

        /// <summary>3D 呈现方式</summary>
        public PresentationStyle Presentation { get; set; } = PresentationStyle.All;
    }

    /// <summary>
    /// 3D 排砖结果
    /// </summary>
    public class Layout3DResult
    {
        /// <summary>原始请求</summary>
        public Layout3DRequest Request { get; set; }

        /// <summary>3D 墙砖放置结果</summary>
        public System.Collections.Generic.List<Brick3DPlacement> WallPlacements { get; set; }
            = new System.Collections.Generic.List<Brick3DPlacement>();

        /// <summary>2D 排砖结果（地砖）</summary>
        public LayoutResult FloorResult { get; set; }

        /// <summary>3D 材料统计</summary>
        public Material3DSummary MaterialSummary { get; set; }

        /// <summary>是否成功</summary>
        public bool Success { get; set; } = true;

        /// <summary>错误信息</summary>
        public string ErrorMessage { get; set; }

        /// <summary>执行时间（ms）</summary>
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// 3D 材料统计
    /// </summary>
    public class Material3DSummary
    {
        /// <summary>砖规格</summary>
        public string SpecName { get; set; }

        /// <summary>整砖数量</summary>
        public int WholeBrickCount { get; set; }

        /// <summary>切割砖数量</summary>
        public int CutBrickCount { get; set; }

        /// <summary>总用量</summary>
        public int TotalCount => WholeBrickCount + CutBrickCount;

        /// <summary>总面积（m²）</summary>
        public double TotalArea { get; set; }

        /// <summary>总体积（m³）</summary>
        public double TotalVolume { get; set; }

        /// <summary>废料体积（m³）</summary>
        public double WasteVolume { get; set; }

        /// <summary>损耗率</summary>
        public double WasteRate => TotalArea > 0 ? (WasteVolume / TotalVolume) * 100.0 : 0;

        /// <summary>切割记录</summary>
        public System.Collections.Generic.List<CutRecord> CutRecords { get; set; }
            = new System.Collections.Generic.List<CutRecord>();
    }
}
