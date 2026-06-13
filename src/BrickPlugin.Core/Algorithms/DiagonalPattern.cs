using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 斜铺 — 整体图案旋转 45°
    /// </summary>
    public class DiagonalPattern : IPatternStrategy
    {
        public LayoutPattern Pattern => LayoutPattern.Diagonal;

        public List<BrickPlacement> Layout(TileRegion region, BrickSpec spec,
                                           JointSetting joint, Point2D startPoint)
        {
            var placements = new List<BrickPlacement>();
            var box = region.BoundingBox;

            double rotationDeg = 45.0;
            double rotationRad = rotationDeg * Math.PI / 180.0;

            // 旋转后的有效步长
            double stepX = spec.Length + joint.VerticalJoint;
            double stepY = spec.Width + joint.HorizontalJoint;

            // 扩大覆盖范围（因为旋转）
            double margin = Math.Max(spec.Length, spec.Width) * 1.5;
            double minX = box.MinX - margin;
            double minY = box.MinY - margin;
            double maxX = box.MaxX + margin;
            double maxY = box.MaxY + margin;

            int cols = (int)Math.Ceiling((maxX - startPoint.X) / stepX) + 2;
            int rows = (int)Math.Ceiling((maxY - startPoint.Y) / stepY) + 2;
            int colsBefore = (int)Math.Ceiling((startPoint.X - minX) / stepX) + 2;
            int rowsBefore = (int)Math.Ceiling((startPoint.Y - minY) / stepY) + 2;

            int idCounter = 0;

            for (int r = -rowsBefore; r < rows; r++)
            {
                for (int c = -colsBefore; c < cols; c++)
                {
                    double x = startPoint.X + c * stepX;
                    double y = startPoint.Y + r * stepY;

                    // 计算旋转前的砖四角，旋转后检查与区域是否有交集
                    // 简化：取砖中心和区域的关系
                    double cx = x + spec.Length / 2.0;
                    double cy = y + spec.Width / 2.0;

                    // 将砖中心逆旋转回「未旋转」坐标系以检查区域
                    double adjustedCx = cx - box.Center.X;
                    double adjustedCy = cy - box.Center.Y;

                    double unrotatedCx = adjustedCx * Math.Cos(-rotationRad) - adjustedCy * Math.Sin(-rotationRad) + box.Center.X;
                    double unrotatedCy = adjustedCx * Math.Sin(-rotationRad) + adjustedCy * Math.Cos(-rotationRad) + box.Center.Y;

                    // 估算旋转后的砖在区域内的占比，简化判断
                    if (!box.Contains(new Point2D(unrotatedCx, unrotatedCy)))
                    {
                        // 放宽条件：只要接近边界就保留
                        bool nearBoundary = unrotatedCx >= box.MinX - spec.Length &&
                                            unrotatedCx <= box.MaxX + spec.Length &&
                                            unrotatedCy >= box.MinY - spec.Width &&
                                            unrotatedCy <= box.MaxY + spec.Width;
                        if (!nearBoundary) continue;
                    }

                    if (StraightPattern.IsInAvoidanceZoneStatic(x, y, spec.Length, spec.Width, region))
                        continue;

                    placements.Add(new BrickPlacement
                    {
                        Id = ++idCounter,
                        RegionName = region.Name,
                        Position = new Point2D(x, y),
                        Rotation = rotationDeg,
                        ActualLength = spec.Length,
                        ActualWidth = spec.Width,
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
