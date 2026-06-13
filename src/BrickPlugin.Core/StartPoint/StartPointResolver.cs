using System;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.StartPoint
{
    /// <summary>
    /// 起铺点解析器 — 根据模式计算实际起铺坐标
    /// </summary>
    public class StartPointResolver
    {
        /// <summary>
        /// 计算实际起铺点
        /// </summary>
        /// <param name="region">排砖区域</param>
        /// <param name="spec">砖规格</param>
        /// <param name="joint">灰缝设置</param>
        /// <param name="mode">起铺点模式</param>
        /// <param name="cornerDirection">角落方向（角落起铺时用）</param>
        /// <param name="manualPoint">手动指定点（手动起铺时用）</param>
        /// <returns>实际起铺点坐标</returns>
        public Point2D Resolve(TileRegion region, BrickSpec spec,
            JointSetting joint, StartPointMode mode,
            CornerDirection cornerDirection = CornerDirection.BottomLeft,
            Point2D? manualPoint = null)
        {
            var box = region.BoundingBox;

            switch (mode)
            {
                case StartPointMode.Center:
                    return ResolveCenter(box, spec, joint);

                case StartPointMode.Corner:
                    return ResolveCorner(box, spec, joint, cornerDirection);

                case StartPointMode.Manual:
                    return manualPoint ?? box.Center; // 未指定则 fallback 居中

                default:
                    return box.Center;
            }
        }

        /// <summary>
        /// 居中起铺 — 从区域中心向四周扩散
        /// </summary>
        private Point2D ResolveCenter(Rect2D box, BrickSpec spec, JointSetting joint)
        {
            double stepX = spec.Length + joint.VerticalJoint;
            double stepY = spec.Width + joint.HorizontalJoint;

            // 计算从中心出发的最接近整砖步长位置
            double centerX = box.Center.X;
            double centerY = box.Center.Y;

            // 调整使中心在砖的零点上（让对称排列更自然）
            double offsetX = (box.Width / stepX) % 1.0;
            double offsetY = (box.Height / stepY) % 1.0;

            double startX = box.MinX + offsetX * stepX / 2.0;
            double startY = box.MinY + offsetY * stepY / 2.0;

            return new Point2D(startX, startY);
        }

        /// <summary>
        /// 角落起铺 — 从指定角落整砖起步
        /// </summary>
        private Point2D ResolveCorner(Rect2D box, BrickSpec spec,
            JointSetting joint, CornerDirection direction)
        {
            switch (direction)
            {
                case CornerDirection.BottomLeft:
                    return box.BottomLeft;
                case CornerDirection.BottomRight:
                    return new Point2D(box.MaxX - spec.Length, box.MinY);
                case CornerDirection.TopLeft:
                    return new Point2D(box.MinX, box.MaxY - spec.Width);
                case CornerDirection.TopRight:
                    return new Point2D(box.MaxX - spec.Length, box.MaxY - spec.Width);
                default:
                    return box.BottomLeft;
            }
        }
    }
}
