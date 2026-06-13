using System;
using System.Collections.Generic;

namespace Fanben.BrickPlugin.Core.Models
{
    /// <summary>
    /// 二维点
    /// </summary>
    public struct Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y) { X = x; Y = y; }

        public static Point2D operator +(Point2D a, Point2D b) => new Point2D(a.X + b.X, a.Y + b.Y);
        public static Point2D operator -(Point2D a, Point2D b) => new Point2D(a.X - b.X, a.Y - b.Y);
        public static Point2D operator *(Point2D a, double s) => new Point2D(a.X * s, a.Y * s);

        public double DistanceTo(Point2D other)
        {
            double dx = X - other.X, dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({X:F3}, {Y:F3})";
    }

    /// <summary>
    /// 矩形区域（轴对齐）
    /// </summary>
    public struct Rect2D
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }

        public Rect2D(double minX, double minY, double maxX, double maxY)
        {
            MinX = minX; MinY = minY; MaxX = maxX; MaxY = maxY;
        }

        public double Width => MaxX - MinX;
        public double Height => MaxY - MinY;
        public Point2D Center => new Point2D((MinX + MaxX) / 2.0, (MinY + MaxY) / 2.0);
        public Point2D BottomLeft => new Point2D(MinX, MinY);
        public Point2D BottomRight => new Point2D(MaxX, MinY);
        public Point2D TopLeft => new Point2D(MinX, MaxY);
        public Point2D TopRight => new Point2D(MaxX, MaxY);

        public bool Contains(Point2D pt) =>
            pt.X >= MinX && pt.X <= MaxX && pt.Y >= MinY && pt.Y <= MaxY;

        public bool Intersects(Rect2D other) =>
            !(MaxX < other.MinX || MinX > other.MaxX ||
              MaxY < other.MinY || MinY > other.MaxY);

        public override string ToString() => $"[{MinX:F1}, {MinY:F1}] → [{MaxX:F1}, {MaxY:F1}]";
    }

    /// <summary>
    /// 砖规格定义
    /// </summary>
    public class BrickSpec
    {
        /// <summary>规格名称（如 "600×300"）</summary>
        public string Name { get; set; }

        /// <summary>长度（mm），沿排布主方向</summary>
        public double Length { get; set; }

        /// <summary>宽度（mm），垂直排布主方向</summary>
        public double Width { get; set; }

        /// <summary>厚度（mm）</summary>
        public double Thickness { get; set; }

        public override string ToString() => $"{Name} ({Length}×{Width}×{Thickness}mm)";

        /// <summary>预置常用规格</summary>
        public static List<BrickSpec> Presets => new List<BrickSpec>
        {
            new BrickSpec { Name = "600×300", Length = 600, Width = 300, Thickness = 10 },
            new BrickSpec { Name = "300×300", Length = 300, Width = 300, Thickness = 10 },
            new BrickSpec { Name = "800×800", Length = 800, Width = 800, Thickness = 12 },
            new BrickSpec { Name = "600×600", Length = 600, Width = 600, Thickness = 10 },
            new BrickSpec { Name = "300×600", Length = 300, Width = 600, Thickness = 10 },
            new BrickSpec { Name = "400×400", Length = 400, Width = 400, Thickness = 10 },
            new BrickSpec { Name = "500×500", Length = 500, Width = 500, Thickness = 10 },
            new BrickSpec { Name = "1000×1000", Length = 1000, Width = 1000, Thickness = 14 },
            new BrickSpec { Name = "150×900", Length = 150, Width = 900, Thickness = 10 },
            new BrickSpec { Name = "200×1200", Length = 200, Width = 1200, Thickness = 12 },
        };
    }
}
