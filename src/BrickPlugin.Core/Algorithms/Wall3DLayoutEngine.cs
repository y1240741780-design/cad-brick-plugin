using System;
using System.Collections.Generic;
using System.Diagnostics;
using Fanben.BrickPlugin.Core.Models;

namespace Fanben.BrickPlugin.Core.Algorithms
{
    /// <summary>
    /// 3D 墙砖排版引擎 — 水平（2D算法）× 垂直（Z轴）双层循环
    /// </summary>
    public class Wall3DLayoutEngine
    {
        private readonly BrickLayoutEngine _engine2D;

        public Wall3DLayoutEngine()
        {
            _engine2D = new BrickLayoutEngine();
        }

        /// <summary>
        /// 执行 3D 墙砖排版 + 地砖排版
        /// </summary>
        public Layout3DResult Execute(Layout3DRequest request)
        {
            var sw = Stopwatch.StartNew();
            var result = new Layout3DResult { Request = request };

            try
            {
                // 校验
                if (request.WallRegions == null || request.WallRegions.Count == 0)
                    throw new ArgumentException("没有指定3D墙面区域");
                if (request.Layout2D?.BrickSpec == null)
                    throw new ArgumentException("没有指定砖规格");

                var spec = request.Layout2D.BrickSpec;
                var joint = request.Layout2D.JointSetting;

                // ===== 1. 地砖排版（2D，复用现有引擎）=====
                if (request.Layout2D.Regions != null && request.Layout2D.Regions.Count > 0)
                {
                    result.FloorResult = _engine2D.Execute(request.Layout2D);
                }

                // ===== 2. 墙砖 3D 排版 =====
                var allWallPlacements = new List<Brick3DPlacement>();
                int idCounter = 0;

                foreach (var wall in request.WallRegions)
                {
                    var wallPlacements = LayoutWall3D(wall, spec, joint,
                        request.VerticalStart,
                        request.Layout2D.Pattern,
                        request.Layout2D.StartPointMode,
                        request.Layout2D.CornerDirection,
                        request.Layout2D.BrickBondOffset,
                        ref idCounter);
                    allWallPlacements.AddRange(wallPlacements);
                }

                result.WallPlacements = allWallPlacements;

                // ===== 3. 材料统计 =====
                result.MaterialSummary = Generate3DSummary(allWallPlacements, spec,
                    result.FloorResult?.MaterialSummary);

                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>
        /// 对单个 3D 墙面区域执行排砖
        /// </summary>
        private List<Brick3DPlacement> LayoutWall3D(
            Wall3DRegion wall, BrickSpec spec, JointSetting joint,
            VerticalStartMode vStart, LayoutPattern pattern,
            StartPointMode hStart, CornerDirection cornerDir,
            double brickBondOffset, ref int idCounter)
        {
            var placements = new List<Brick3DPlacement>();
            var box = wall.BoundingBox;

            // === 垂直（Z轴）参数 ===
            // 砖的"宽度"在墙上变成高度方向
            double stepZ = spec.Width + joint.HorizontalJoint;

            // 计算 Z 轴起铺点
            double startZ = ResolveVerticalStart(wall, spec, joint, vStart);

            // Z 轴行列范围
            int zRowsAbove = (int)Math.Ceiling((wall.TopZ - startZ) / stepZ) + 1;
            int zRowsBelow = (int)Math.Ceiling((startZ - wall.BaseZ) / stepZ) + 1;

            // === 水平（XY平面）参数 ===
            double stepX = spec.Length + joint.VerticalJoint;

            // 水平起铺点
            double startX, startY;
            ResolveHorizontalStart(box, spec, joint, hStart, cornerDir, out startX, out startY);

            int xCols = (int)Math.Ceiling((box.MaxX - startX) / stepX) + 2;
            int xColsBefore = (int)Math.Ceiling((startX - box.MinX) / stepX) + 2;

            // === 双层循环：Z轴 × X轴 ===
            for (int z = -zRowsBelow; z < zRowsAbove; z++)
            {
                double zCoord = startZ + z * stepZ;

                // 垂直方向裁剪
                double actualHeight = spec.Width;
                if (zCoord < wall.BaseZ)
                {
                    double cut = wall.BaseZ - zCoord;
                    if (cut >= spec.Width) continue;
                    zCoord = wall.BaseZ;
                    actualHeight -= cut;
                }
                if (zCoord + actualHeight > wall.TopZ)
                    actualHeight = wall.TopZ - zCoord;
                if (actualHeight <= 0) continue;

                // 工字铺：Z轴奇数行也可错开（可选）
                double zRowOffset = 0;
                if (pattern == LayoutPattern.BrickBond)
                {
                    // 奇数Z行也错开，形成立体工字效果
                    zRowOffset = (Math.Abs(z) % 2 == 1) ? spec.Length * brickBondOffset : 0;
                }

                for (int x = -xColsBefore; x < xCols; x++)
                {
                    double xCoord = startX + zRowOffset + x * stepX;

                    // 水平裁剪
                    double actualLength = spec.Length;
                    if (xCoord < box.MinX)
                    {
                        double cut = box.MinX - xCoord;
                        if (cut >= spec.Length) continue;
                        xCoord = box.MinX;
                        actualLength -= cut;
                    }
                    if (xCoord + actualLength > box.MaxX)
                        actualLength = box.MaxX - xCoord;
                    if (actualLength <= 0) continue;

                    // 3D 洞口避让检测
                    if (IsIn3DOpening(xCoord, startY, zCoord, actualLength, actualHeight, wall))
                        continue;

                    placements.Add(new Brick3DPlacement
                    {
                        Id = ++idCounter,
                        RegionName = wall.Name,
                        Position = new Point3D(xCoord, startY, zCoord),
                        Rotation = 0,
                        ActualLength = actualLength,
                        ActualHeight = actualHeight,
                        ActualThickness = wall.WallThickness,
                        OriginalSpec = spec,
                        RowX = x,
                        RowZ = z
                    });
                }
            }

            return placements;
        }

        /// <summary>
        /// 计算垂直起铺 Z 坐标
        /// </summary>
        private double ResolveVerticalStart(Wall3DRegion wall, BrickSpec spec,
            JointSetting joint, VerticalStartMode mode)
        {
            double stepZ = spec.Width + joint.HorizontalJoint;

            switch (mode)
            {
                case VerticalStartMode.Bottom:
                    return wall.BaseZ;  // 从地面整砖起步

                case VerticalStartMode.Top:
                    // 从墙顶往下：确保顶部是整砖
                    return wall.TopZ - (Math.Ceiling(wall.WallHeight / stepZ) * stepZ) + spec.Width;

                case VerticalStartMode.Center:
                    // 居中：上下对称
                    double offset = (wall.WallHeight % stepZ) / 2.0;
                    return wall.BaseZ + offset;

                default:
                    return wall.BaseZ;
            }
        }

        /// <summary>
        /// 计算水平起铺点（复用 StartPointResolver 逻辑）
        /// </summary>
        private void ResolveHorizontalStart(Rect2D box, BrickSpec spec, JointSetting joint,
            StartPointMode mode, CornerDirection cornerDir, out double startX, out double startY)
        {
            double stepX = spec.Length + joint.VerticalJoint;
            double stepY = spec.Width + joint.HorizontalJoint;

            switch (mode)
            {
                case StartPointMode.Center:
                    double offsetX = (box.Width % stepX) / 2.0;
                    startX = box.MinX + offsetX;
                    startY = box.MinY;
                    break;

                case StartPointMode.Corner:
                    switch (cornerDir)
                    {
                        case CornerDirection.BottomLeft:
                            startX = box.MinX; startY = box.MinY; break;
                        case CornerDirection.BottomRight:
                            startX = box.MaxX - spec.Length; startY = box.MinY; break;
                        case CornerDirection.TopLeft:
                            startX = box.MinX; startY = box.MaxY - spec.Width; break;
                        case CornerDirection.TopRight:
                            startX = box.MaxX - spec.Length; startY = box.MaxY - spec.Width; break;
                        default:
                            startX = box.MinX; startY = box.MinY; break;
                    }
                    break;

                default:
                    startX = box.MinX;
                    startY = box.MinY;
                    break;
            }
        }

        /// <summary>
        /// 3D 洞口碰撞检测 — 砖块是否与任何 3D 洞口重叠
        /// </summary>
        private bool IsIn3DOpening(double x, double y, double z,
            double length, double height, Wall3DRegion wall)
        {
            // 检查 XY 平面重叠
            var brickRect = new Rect2D(x, y, x + length, y + wall.WallThickness);

            foreach (var opening in wall.Openings3D)
            {
                // Z 轴重叠检测
                if (z + height <= opening.BottomZ || z >= opening.TopZ)
                    continue;

                // XY 平面重叠
                if (brickRect.Intersects(opening.Boundary))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 生成 3D 材料统计
        /// </summary>
        private Material3DSummary Generate3DSummary(
            List<Brick3DPlacement> wallPlacements,
            BrickSpec spec, MaterialSummary floorSummary)
        {
            int whole3D = 0, cut3D = 0;
            double totalVol = 0, wasteVol = 0;

            foreach (var p in wallPlacements)
            {
                double vol = p.Volume / 1_000_000_000.0; // mm³ → m³
                totalVol += vol;

                if (p.IsWhole) whole3D++;
                else cut3D++;

                if (p.IsCut)
                {
                    double origVol = (spec.Length * spec.Width * p.ActualThickness) / 1_000_000_000.0;
                    wasteVol += origVol - vol;
                }
            }

            var summary = new Material3DSummary
            {
                SpecName = spec.Name,
                WholeBrickCount = whole3D + (floorSummary?.WholeBrickCount ?? 0),
                CutBrickCount = cut3D + (floorSummary?.CutBrickCount ?? 0),
                TotalArea = totalVol / (spec.Thickness / 1000.0) + (floorSummary?.TotalArea ?? 0),
                TotalVolume = totalVol,
                WasteVolume = wasteVol
            };

            return summary;
        }
    }
}
