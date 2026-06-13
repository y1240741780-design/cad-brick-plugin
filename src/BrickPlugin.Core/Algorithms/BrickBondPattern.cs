using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 工字铺（错缝铺）— 奇数行和偶数行错开排列
    /// </summary>
    public class BrickBondPattern : IPatternStrategy
    {
        public LayoutPattern Pattern => LayoutPattern.BrickBond;

        /// <summary>错缝偏移比例（0.5 = 1/2 错缝，0.333 = 1/3 错缝）</summary>
        public double OffsetRatio { get; set; } = 0.5;

        public List<BrickPlacement> Layout(TileRegion region, BrickSpec spec,
                                           JointSetting joint, Point2D startPoint)
        {
            var placements = new List<BrickPlacement>();
            var box = region.BoundingBox;

            double stepX = spec.Length + joint.VerticalJoint;
            double stepY = spec.Width + joint.HorizontalJoint;
            double offsetAmount = spec.Length * OffsetRatio;

            // 覆盖起铺点左侧
            int colsBefore = (int)Math.Ceiling((startPoint.X - box.MinX + offsetAmount) / stepX) + 2;
            int rowsBefore = (int)Math.Ceiling((startPoint.Y - box.MinY) / stepY) + 1;
            int cols = (int)Math.Ceiling((box.MaxX - startPoint.X + offsetAmount) / stepX) + 2;
            int rows = (int)Math.Ceiling((box.MaxY - startPoint.Y) / stepY) + 1;

            int idCounter = 0;

            for (int r = -rowsBefore; r < rows; r++)
            {
                // 奇数行偏移
                double rowOffset = (Math.Abs(r) % 2 == 1) ? offsetAmount : 0;

                for (int c = -colsBefore; c < cols; c++)
                {
                    double x = startPoint.X + rowOffset + c * stepX;
                    double y = startPoint.Y + r * stepY;

                    double actualLength = spec.Length;
                    double actualWidth = spec.Width;

                    // 裁剪逻辑（同直铺）
                    if (x < box.MinX)
                    {
                        double cut = box.MinX - x;
                        if (cut >= spec.Length) continue;
                        x = box.MinX;
                        actualLength -= cut;
                    }

                    if (y < box.MinY)
                    {
                        double cut = box.MinY - y;
                        if (cut >= spec.Width) continue;
                        y = box.MinY;
                        actualWidth -= cut;
                    }

                    if (x + actualLength > box.MaxX)
                        actualLength = box.MaxX - x;

                    if (y + actualWidth > box.MaxY)
                        actualWidth = box.MaxY - y;

                    if (actualLength <= 0 || actualWidth <= 0) continue;

                    if (StraightPattern.IsInAvoidanceZoneStatic(x, y, actualLength, actualWidth, region))
                        continue;

                    placements.Add(new BrickPlacement
                    {
                        Id = ++idCounter,
                        RegionName = region.Name,
                        Position = new Point2D(x, y),
                        Rotation = 0,
                        ActualLength = actualLength,
                        ActualWidth = actualWidth,
                        OriginalSpec = spec,
                        Row = r,
                        Column = c
                    });
                }
            }

            return placements;
        }
    }
}
