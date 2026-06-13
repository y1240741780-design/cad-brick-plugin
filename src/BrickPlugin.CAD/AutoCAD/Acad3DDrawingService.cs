using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.Core.Models;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace Fanben.BrickPlugin.CAD.AutoCAD
{
    /// <summary>
    /// AutoCAD 3D 绘图服务 — Solid3d 实体 / 拉伸体 / 布尔减法挖洞
    /// </summary>
    public class Acad3DDrawingService
    {
        private readonly Document _doc;
        private readonly Database _db;

        /// <summary>3D 图层名常量</summary>
        public static class Layers3D
        {
            public const string Wall3D = "BRICK-3D-WALL";
            public const string Floor3D = "BRICK-3D-FLOOR";
            public const string Opening3D = "BRICK-3D-OPENING";
        }

        public Acad3DDrawingService()
        {
            _doc = Application.DocumentManager.MdiActiveDocument;
            _db = _doc.Database;
        }

        // ==================== 墙砖 3D 实体 ====================

        /// <summary>
        /// 创建单块墙砖 Solid3d（Box 实体）
        /// </summary>
        public void CreateWallBrickSolid(Brick3DPlacement placement, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // Solid3d.CreateBox 参数：(xLength, yLength, zLength)
                // 注意 AutoCAD 坐标系: X=长度, Y=墙厚方向, Z=高度方向
                using (var solid = new Solid3d())
                {
                    solid.RecordHistory = true;
                    solid.CreateBox(
                        placement.ActualLength,      // X = 砖的长度
                        placement.ActualThickness,   // Y = 墙厚
                        placement.ActualHeight       // Z = 砖的高度
                    );

                    // 移动到正确位置
                    var moveTo = Matrix3d.Displacement(
                        new Vector3d(
                            placement.Position.X,
                            placement.Position.Y,
                            placement.Position.Z));

                    solid.TransformBy(moveTo);

                    // 如果有旋转（绕Z轴）
                    if (Math.Abs(placement.Rotation) > 0.001)
                    {
                        var rotCenter = new Point3d(
                            placement.Position.X + placement.ActualLength / 2.0,
                            placement.Position.Y + placement.ActualThickness / 2.0,
                            placement.Position.Z + placement.ActualHeight / 2.0);
                        solid.TransformBy(Matrix3d.Rotation(
                            placement.Rotation * Math.PI / 180.0,
                            Vector3d.ZAxis, rotCenter));
                    }

                    solid.Layer = layerName;
                    btr.AppendEntity(solid);
                    tr.AddNewlyCreatedDBObject(solid, true);
                }

                tr.Commit();
            }
        }

        /// <summary>
        /// 批量创建墙砖 Solid3d
        /// </summary>
        public void CreateWallBrickSolids(List<Brick3DPlacement> placements, string layerName)
        {
            foreach (var placement in placements)
                CreateWallBrickSolid(placement, layerName);
        }

        // ==================== 地砖 3D 拉伸体 ====================

        /// <summary>
        /// 创建地砖拉伸体（Polyline + Thickness 属性）
        /// 轻量级 3D，速度快
        /// </summary>
        public void CreateFloorBrickExtrusion(BrickPlacement placement,
            double thickness, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                double x = placement.Position.X;
                double y = placement.Position.Y;
                double w = placement.ActualLength;
                double h = placement.ActualWidth;

                var poly = new Polyline();
                poly.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                poly.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                poly.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                poly.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                poly.Closed = true;

                // 设置拉伸厚度（正数向上 = +Z 方向）
                poly.Thickness = thickness;
                poly.Elevation = 0; // 从 Z=0 开始

                poly.Layer = layerName;
                btr.AppendEntity(poly);
                tr.AddNewlyCreatedDBObject(poly, true);
                tr.Commit();
            }
        }

        /// <summary>
        /// 批量创建地砖拉伸体
        /// </summary>
        public void CreateFloorBrickExtrusions(List<BrickPlacement> placements,
            double thickness, string layerName)
        {
            foreach (var placement in placements)
                CreateFloorBrickExtrusion(placement, thickness, layerName);
        }

        // ==================== 精确修剪（布尔减法挖洞）====================

        /// <summary>
        /// 精确修剪 — 通过布尔减法从砖块中挖掉洞口
        /// </summary>
        /// <param name="placement">原始砖块</param>
        /// <param name="openings">3D 洞口列表</param>
        /// <param name="layerName">图层名</param>
        public void PreciseTrimBrick(Brick3DPlacement placement,
            List<Opening3DInfo> openings, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 先创建完整砖块
                var solid = new Solid3d { RecordHistory = true };
                solid.CreateBox(
                    placement.ActualLength,
                    placement.ActualThickness,
                    placement.ActualHeight);

                var moveTo = Matrix3d.Displacement(new Vector3d(
                    placement.Position.X, placement.Position.Y, placement.Position.Z));
                solid.TransformBy(moveTo);

                // 对每个与之相交的洞口执行布尔减法
                foreach (var opening in openings)
                {
                    // 检查砖块是否与洞口有 3D 重叠
                    if (!BrickOverlapsOpening(placement, opening))
                        continue;

                    // 计算洞口在砖块局部坐标系统中的位置
                    var openingSolid = CreateOpeningSolid(placement, opening);
                    if (openingSolid == null) continue;

                    try
                    {
                        solid.BooleanOperation(BooleanOperationType.BoolSubtract, openingSolid);
                    }
                    catch
                    {
                        // 布尔运算失败（不重叠等），跳过
                        openingSolid.Dispose();
                    }
                }

                solid.Layer = layerName;
                btr.AppendEntity(solid);
                tr.AddNewlyCreatedDBObject(solid, true);
                tr.Commit();
            }
        }

        /// <summary>
        /// 检查砖块与洞口是否有 3D 重叠
        /// </summary>
        private bool BrickOverlapsOpening(Brick3DPlacement brick, Opening3DInfo opening)
        {
            // Z 轴重叠
            if (brick.Position.Z + brick.ActualHeight <= opening.BottomZ ||
                brick.Position.Z >= opening.TopZ)
                return false;

            // XY 平面重叠
            var brickRect = new Rect2D(
                brick.Position.X, brick.Position.Y,
                brick.Position.X + brick.ActualLength,
                brick.Position.Y + brick.ActualThickness);

            return brickRect.Intersects(opening.Boundary);
        }

        /// <summary>
        /// 创建洞口 Solid3d（用于布尔减法）
        /// </summary>
        private Solid3d CreateOpeningSolid(Brick3DPlacement brick, Opening3DInfo opening)
        {
            var solid = new Solid3d { RecordHistory = false };

            // 洞口在墙平面上的 XY 尺寸
            double ox = opening.Boundary.MinX;
            double oy = opening.Boundary.MinY; // 墙厚方向
            double ow = opening.Boundary.Width;
            double od = opening.Boundary.Height; // 实际是墙厚方向跨度

            // Z 方向
            double oz = opening.BottomZ;
            double oh = opening.Height;

            // 创建比砖稍大的洞口体（确保完全穿透）
            double margin = 10; // 10mm margin
            solid.CreateBox(
                ow + margin,
                brick.ActualThickness + margin,
                oh + margin);

            // 移动洞口到正确位置
            var moveTo = Matrix3d.Displacement(new Vector3d(
                ox - margin / 2.0,
                brick.Position.Y - margin / 2.0,
                oz - margin / 2.0));
            solid.TransformBy(moveTo);

            return solid;
        }

        /// <summary>
        /// 对墙面所有砖块执行精确修剪
        /// </summary>
        public void PreciseTrimWallBricks(List<Brick3DPlacement> placements,
            List<Opening3DInfo> openings, string layerName)
        {
            foreach (var placement in placements)
            {
                PreciseTrimBrick(placement, openings, layerName);
            }
        }

        // ==================== 3D 网格辅助线 ====================

        /// <summary>
        /// 绘制 3D 坐标网格辅助线
        /// </summary>
        public void Draw3DGrid(Wall3DRegion wall, double gridSpacing, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var box = wall.BoundingBox;

                // 水平网格线
                for (double z = wall.BaseZ; z <= wall.TopZ; z += gridSpacing)
                {
                    var line = new Line(
                        new Point3d(box.MinX, box.MinY, z),
                        new Point3d(box.MaxX, box.MinY, z));
                    line.Layer = layerName;
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                }

                // 垂直网格线
                for (double x = box.MinX; x <= box.MaxX; x += gridSpacing)
                {
                    var line = new Line(
                        new Point3d(x, box.MinY, wall.BaseZ),
                        new Point3d(x, box.MinY, wall.TopZ));
                    line.Layer = layerName;
                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                }

                tr.Commit();
            }
        }
    }
}
