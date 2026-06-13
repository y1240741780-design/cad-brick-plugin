using System.Collections.Generic;
using System.Linq;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.AreaAnalyzer
{
    /// <summary>
    /// 区域分析器 — 处理多边形边界，提取包围盒，识别门窗洞口
    /// </summary>
    public class AreaAnalyzer
    {
        /// <summary>
        /// 从边界点列表中计算包围盒
        /// </summary>
        public Rect2D ComputeBoundingBox(List<Point2D> boundaryPoints)
        {
            if (boundaryPoints == null || boundaryPoints.Count == 0)
                return new Rect2D(0, 0, 0, 0);

            double minX = boundaryPoints[0].X, maxX = minX;
            double minY = boundaryPoints[0].Y, maxY = minY;

            foreach (var pt in boundaryPoints)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            return new Rect2D(minX, minY, maxX, maxY);
        }

        /// <summary>
        /// 自动检测区域内的洞口（内部闭合多边形）
        /// 简化实现：假设洞口边界已作为 OpeningInfo 提供
        /// 在实际 CAD 集成时，此方法通过 AutoCAD API 读取内部闭合多段线
        /// </summary>
        public List<OpeningInfo> DetectOpenings(TileRegion region,
            List<List<Point2D>> innerPolylines)
        {
            var openings = new List<OpeningInfo>();
            int idx = 1;

            foreach (var polyline in innerPolylines)
            {
                var box = ComputeBoundingBox(polyline);
                openings.Add(new OpeningInfo
                {
                    Name = $"洞口{idx++}",
                    Boundary = box,
                    Type = DetectOpeningType(box)
                });
            }

            return openings;
        }

        /// <summary>
        /// 根据尺寸推断洞口类型（门/窗）
        /// </summary>
        private string DetectOpeningType(Rect2D box)
        {
            double width = box.Width;
            double height = box.Height;

            // 竖直方向长的 → 窗；水平方向宽的 → 门
            if (height > width * 1.5) return "窗";
            if (width > height * 1.5) return "门";
            return "洞口";
        }

        /// <summary>
        /// 检查一个点是否在闭合多段线内部（射线法）
        /// </summary>
        public bool IsPointInsidePolygon(Point2D point, List<Point2D> polygon)
        {
            if (polygon == null || polygon.Count < 3) return false;

            bool inside = false;
            int n = polygon.Count;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var pi = polygon[i];
                var pj = polygon[j];

                if ((pi.Y > point.Y) != (pj.Y > point.Y) &&
                    point.X < (pj.X - pi.X) * (point.Y - pi.Y) / (pj.Y - pi.Y) + pi.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}
