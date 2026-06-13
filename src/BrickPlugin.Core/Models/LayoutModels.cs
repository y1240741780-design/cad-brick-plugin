using System;
using System.Collections.Generic;

namespace Fanben.BrickPlugin.Core.Models
{
    /// <summary>
    /// 灰缝设置
    /// </summary>
    public class JointSetting
    {
        /// <summary>水平灰缝宽度（mm）</summary>
        public double HorizontalJoint { get; set; } = 2.0;

        /// <summary>垂直灰缝宽度（mm）</summary>
        public double VerticalJoint { get; set; } = 2.0;

        public override string ToString() => $"水平{HorizontalJoint}mm / 垂直{VerticalJoint}mm";
    }

    /// <summary>
    /// 门窗洞口信息
    /// </summary>
    public class OpeningInfo
    {
        /// <summary>洞口名称</summary>
        public string Name { get; set; }

        /// <summary>洞口边界矩形</summary>
        public Rect2D Boundary { get; set; }

        /// <summary>洞口类型（门/窗/其他）</summary>
        public string Type { get; set; } = "洞口";

        public override string ToString() => $"{Type}[{Name}] {Boundary}";
    }

    /// <summary>
    /// 排砖区域
    /// </summary>
    public class TileRegion
    {
        /// <summary>区域名称</summary>
        public string Name { get; set; }

        /// <summary>排砖类型</summary>
        public TileType TileType { get; set; }

        /// <summary>区域边界点（闭合多段线的顶点）</summary>
        public List<Point2D> BoundaryPoints { get; set; } = new List<Point2D>();

        /// <summary>包围盒</summary>
        public Rect2D BoundingBox { get; set; }

        /// <summary>门窗洞口列表（仅墙砖时使用）</summary>
        public List<OpeningInfo> Openings { get; set; } = new List<OpeningInfo>();

        /// <summary>用户手动指定的避让区域</summary>
        public List<Rect2D> ManualAvoidanceZones { get; set; } = new List<Rect2D>();
    }

    /// <summary>
    /// 排砖请求（用户输入汇总）
    /// </summary>
    public class LayoutRequest
    {
        /// <summary>排砖区域列表</summary>
        public List<TileRegion> Regions { get; set; } = new List<TileRegion>();

        /// <summary>砖规格</summary>
        public BrickSpec BrickSpec { get; set; }

        /// <summary>灰缝设置</summary>
        public JointSetting JointSetting { get; set; } = new JointSetting();

        /// <summary>铺贴方式</summary>
        public LayoutPattern Pattern { get; set; } = LayoutPattern.Straight;

        /// <summary>起铺点模式</summary>
        public StartPointMode StartPointMode { get; set; } = StartPointMode.Center;

        /// <summary>角落方向（角落起铺时生效）</summary>
        public CornerDirection CornerDirection { get; set; } = CornerDirection.BottomLeft;

        /// <summary>手动起铺点（手动起铺时生效）</summary>
        public Point2D? ManualStartPoint { get; set; }

        /// <summary>避让模式</summary>
        public AvoidanceMode AvoidanceMode { get; set; } = AvoidanceMode.AutoDetect;

        /// <summary>切割约束模式</summary>
        public CutConstraintMode CutMode { get; set; } = CutConstraintMode.Comprehensive;

        /// <summary>最小切割比例（砖块的指定比例，如 1/3）</summary>
        public double MinCutRatio { get; set; } = 1.0 / 3.0;

        /// <summary>工字铺错缝比例（0.5 = 1/2错缝，0.333 = 1/3错缝）</summary>
        public double BrickBondOffset { get; set; } = 0.5;

        /// <summary>呈现方式</summary>
        public PresentationStyle Presentation { get; set; } = PresentationStyle.All;
    }

    /// <summary>
    /// 单块砖的放置结果
    /// </summary>
    public class BrickPlacement
    {
        /// <summary>砖编号</summary>
        public int Id { get; set; }

        /// <summary>排砖区域名称</summary>
        public string RegionName { get; set; }

        /// <summary>砖左下角位置</summary>
        public Point2D Position { get; set; }

        /// <summary>旋转角度（度）</summary>
        public double Rotation { get; set; }

        /// <summary>实际长度（切割后）</summary>
        public double ActualLength { get; set; }

        /// <summary>实际宽度（切割后）</summary>
        public double ActualWidth { get; set; }

        /// <summary>原始砖规格</summary>
        public BrickSpec OriginalSpec { get; set; }

        /// <summary>是否为切割砖</summary>
        public bool IsCut => Math.Abs(ActualLength - OriginalSpec.Length) > 0.01 ||
                              Math.Abs(ActualWidth - OriginalSpec.Width) > 0.01;

        /// <summary>是否为整砖</summary>
        public bool IsWhole => !IsCut;

        /// <summary>行索引</summary>
        public int Row { get; set; }

        /// <summary>列索引</summary>
        public int Column { get; set; }

        /// <summary>砖的矩形区域</summary>
        public Rect2D GetRect()
        {
            return new Rect2D(
                Position.X, Position.Y,
                Position.X + ActualLength, Position.Y + ActualWidth);
        }

        public override string ToString() =>
            $"砖#{Id} [{RegionName}] @({Position.X:F1},{Position.Y:F1}) " +
            $"{ActualLength:F0}×{ActualWidth:F0} " +
            $"{(IsCut ? "(切割)" : "(整砖)")}";
    }

    /// <summary>
    /// 切割方案（单块砖的切割记录）
    /// </summary>
    public class CutRecord
    {
        /// <summary>关联砖编号</summary>
        public int BrickId { get; set; }

        /// <summary>切割前尺寸</summary>
        public double OriginalLength { get; set; }
        public double OriginalWidth { get; set; }

        /// <summary>切割后尺寸</summary>
        public double CutLength { get; set; }
        public double CutWidth { get; set; }

        /// <summary>废料长度</summary>
        public double WasteLength => OriginalLength - CutLength;

        /// <summary>废料宽度</summary>
        public double WasteWidth => OriginalWidth - CutWidth;

        /// <summary>废料面积（mm²）</summary>
        public double WasteArea => (OriginalLength * OriginalWidth) - (CutLength * CutWidth);
    }

    /// <summary>
    /// 材料统计汇总
    /// </summary>
    public class MaterialSummary
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

        /// <summary>废料总面积（m²）</summary>
        public double WasteArea { get; set; }

        /// <summary>损耗率（%）</summary>
        public double WasteRate => TotalArea > 0 ? (WasteArea / TotalArea) * 100.0 : 0;

        /// <summary>切割明细</summary>
        public List<CutRecord> CutRecords { get; set; } = new List<CutRecord>();
    }

    /// <summary>
    /// 排砖结果（完整输出）
    /// </summary>
    public class LayoutResult
    {
        /// <summary>排砖请求</summary>
        public LayoutRequest Request { get; set; }

        /// <summary>所有砖块放置结果</summary>
        public List<BrickPlacement> Placements { get; set; } = new List<BrickPlacement>();

        /// <summary>材料统计</summary>
        public MaterialSummary MaterialSummary { get; set; }

        /// <summary>是否成功</summary>
        public bool Success { get; set; } = true;

        /// <summary>错误信息</summary>
        public string ErrorMessage { get; set; }

        /// <summary>执行时间（毫秒）</summary>
        public long ExecutionTimeMs { get; set; }
    }
}
