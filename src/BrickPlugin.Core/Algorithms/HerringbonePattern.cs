using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 人字铺（Herringbone）— 砖块 45° 交错铺贴
    /// </summary>
    public class HerringbonePattern : IPatternStrategy
    {
        public LayoutPattern Pattern => LayoutPattern.Herringbone;

        public List<BrickPlacement> Layout(TileRegion region, BrickSpec spec,
                                           JointSetting joint, Point2D startPoint)
        {
            var placements = new List<BrickPlacement>();
            var box = region.BoundingBox;

            // 人字铺：砖以 45°/135° 交替排列
            // 砖对角线方向为主排列方向
            double brickDiag = Math.Sqrt(spec.Length * spec.Length + spec.Width * spec.Width);
            double stepMain = brickDiag + joint.HorizontalJoint;

            // 覆盖范围（人字铺需要更大的覆盖范围因为旋转）
            double margin = brickDiag * 1.5;
            double minX = box.MinX - margin;
            double minY = box.MinY - margin;
            double maxX = box.MaxX + margin;
            double maxY = box.MaxY + margin;

            int cols = (int)Math.Ceiling((maxX - startPoint.X) / stepMain) + 2;
            int rows = (int)Math.Ceiling((maxY - startPoint.Y) / stepMain) + 2;
            int colsBefore = (int)Math.Ceiling((startPoint.X - minX) / stepMain) + 2;
            int rowsBefore = (int)Math.Ceiling((startPoint.Y - minY) / stepMain) + 2;

            int idCounter = 0;

            for (int r = -rowsBefore; r < rows; r++)
            {
                for (int c = -colsBefore; c < cols; c++)
                {
                    double cx = startPoint.X + c * stepMain;
                    double cy = startPoint.Y + r * stepMain;

                    // 每格两片砖，分别 45° 和 -45°
                    for (int piece = 0; piece < 2; piece++)
                    {
                        double rotation = (piece == 0) ? 45.0 : -45.0;
                        double radRotation = rotation * Math.PI / 180.0;

                        // 人字铺中砖中心位置偏移
                        double offsetX = (piece == 0) ? 0 : stepMain / 2.0;
                        double offsetY = (piece == 0) ? 0 : stepMain / 2.0;

                        double px = cx + offsetX;
                        double py = cy + offsetY;

                        // 简单碰撞检测：砖中心是否在区域内
                        // 更精确的做法是旋转后的矩形碰撞检测，此处简化
                        if (!box.Contains(new Point2D(px, py)))
                            continue;

                        // 检查避让
                        if (IsPointInAvoidance(px, py, region))
                            continue;

                        // 边界裁剪简化处理
                        double actualLen = spec.Length;
                        double actualWid = spec.Width;

                        placements.Add(new BrickPlacement
                        {
                            Id = ++idCounter,
                            RegionName = region.Name,
                            Position = new Point2D(px - spec.Length / 2.0, py - spec.Width / 2.0),
                            Rotation = rotation,
                            ActualLength = actualLen,
                            ActualWidth = actualWid,
                            OriginalSpec = spec,
                            Row = r,
                            Column = c * 2 + piece
                        });
                    }
                }
            }

            return placements;
        }

        private static bool IsPointInAvoidance(double x, double y, TileRegion region)
        {
            foreach (var opening in region.Openings)
            {
                if (opening.Boundary.Contains(new Point2D(x, y)))
                    return true;
            }
            foreach (var zone in region.ManualAvoidanceZones)
            {
                if (zone.Contains(new Point2D(x, y)))
                    return true;
            }
            return false;
        }
    }
}
