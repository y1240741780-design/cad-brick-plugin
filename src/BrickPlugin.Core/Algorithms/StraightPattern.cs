using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 直铺（通缝铺）— 砖对齐排列，最简单
    /// </summary>
    public class StraightPattern : IPatternStrategy
    {
        public LayoutPattern Pattern => LayoutPattern.Straight;

        public List<BrickPlacement> Layout(TileRegion region, BrickSpec spec,
                                           JointSetting joint, Point2D startPoint)
        {
            var placements = new List<BrickPlacement>();
            var box = region.BoundingBox;

            // 计算有效步长（砖尺寸 + 灰缝）
            double stepX = spec.Length + joint.VerticalJoint;
            double stepY = spec.Width + joint.HorizontalJoint;

            // 计算行列数
            int cols = (int)Math.Ceiling((box.MaxX - startPoint.X) / stepX) + 1;
            int rows = (int)Math.Ceiling((box.MaxY - startPoint.Y) / stepY) + 1;

            // 也覆盖起铺点左侧和下侧
            int colsBefore = (int)Math.Ceiling((startPoint.X - box.MinX) / stepX) + 1;
            int rowsBefore = (int)Math.Ceiling((startPoint.Y - box.MinY) / stepY) + 1;

            int idCounter = 0;

            for (int r = -rowsBefore; r < rows; r++)
            {
                for (int c = -colsBefore; c < cols; c++)
                {
                    double x = startPoint.X + c * stepX;
                    double y = startPoint.Y + r * stepY;

                    // 计算砖的实际尺寸（在边界处可能需要切割）
                    double actualLength = spec.Length;
                    double actualWidth = spec.Width;

                    // 左侧裁剪
                    if (x < box.MinX)
                    {
                        double cutAmount = box.MinX - x;
                        if (cutAmount >= spec.Length) continue; // 完全在区域外
                        x = box.MinX;
                        actualLength -= cutAmount;
                    }

                    // 下侧裁剪
                    if (y < box.MinY)
                    {
                        double cutAmount = box.MinY - y;
                        if (cutAmount >= spec.Width) continue;
                        y = box.MinY;
                        actualWidth -= cutAmount;
                    }

                    // 右侧裁剪
                    if (x + actualLength > box.MaxX)
                        actualLength = box.MaxX - x;

                    // 上侧裁剪
                    if (y + actualWidth > box.MaxY)
                        actualWidth = box.MaxY - y;

                    // 砖太小则跳过
                    if (actualLength <= 0 || actualWidth <= 0) continue;

                    // 检查是否在避让区域内
                    if (IsInAvoidanceZoneStatic(x, y, actualLength, actualWidth, region))
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

        /// <summary>
        /// 检查砖块是否在避让区域（门窗洞口/手动避让区）内
        /// </summary>
        public static bool IsInAvoidanceZoneStatic(double x, double y,
            double length, double width, TileRegion region)
        {
            var brickRect = new Rect2D(x, y, x + length, y + width);

            // 检查门窗洞口
            foreach (var opening in region.Openings)
            {
                if (brickRect.Intersects(opening.Boundary))
                    return true;
            }

            // 检查手动避让区域
            foreach (var zone in region.ManualAvoidanceZones)
            {
                if (brickRect.Intersects(zone))
                    return true;
            }

            return false;
        }
    }
}
