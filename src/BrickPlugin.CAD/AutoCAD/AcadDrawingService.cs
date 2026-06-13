using System;
using System.Collections.Generic;
using Fanben.BrickPlugin.CAD.Abstractions;
using Fanben.BrickPlugin.Core.Models;

// AutoCAD .NET API 引用
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace Fanben.BrickPlugin.CAD.AutoCAD
{
    /// <summary>
    /// AutoCAD 绘图服务实现
    /// </summary>
    public class AcadDrawingService : ICADDrawingService
    {
        private readonly Document _doc;
        private readonly Database _db;
        private readonly Editor _ed;

        public AcadDrawingService()
        {
            _doc = Application.DocumentManager.MdiActiveDocument;
            _db = _doc.Database;
            _ed = _doc.Editor;
        }

        /// <summary>图层名常量</summary>
        public static class Layers
        {
            public const string Outline = "BRICK-OUTLINE";
            public const string Hatch = "BRICK-HATCH";
            public const string Joint = "BRICK-JOINT";
            public const string Cut = "BRICK-CUT";
            public const string Annotation = "BRICK-ANNOTATION";
            public const string Avoidance = "BRICK-AVOIDANCE";
        }

        public void DrawBrickOutline(BrickPlacement placement, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 创建矩形多段线
                var poly = new Polyline();
                double x = placement.Position.X;
                double y = placement.Position.Y;
                double w = placement.ActualLength;
                double h = placement.ActualWidth;

                poly.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                poly.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                poly.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                poly.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                poly.Closed = true;

                // 旋转（如果非零）
                if (Math.Abs(placement.Rotation) > 0.001)
                {
                    var center = new Point3d(x + w / 2, y + h / 2, 0);
                    poly.TransformBy(Matrix3d.Rotation(
                        placement.Rotation * Math.PI / 180.0, Vector3d.ZAxis, center));
                }

                poly.Layer = layerName;
                btr.AppendEntity(poly);
                tr.AddNewlyCreatedDBObject(poly, true);
                tr.Commit();
            }
        }

        public void DrawBrickOutlines(List<BrickPlacement> placements, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var placement in placements)
                {
                    var poly = new Polyline();
                    double x = placement.Position.X;
                    double y = placement.Position.Y;
                    double w = placement.ActualLength;
                    double h = placement.ActualWidth;

                    poly.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                    poly.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                    poly.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                    poly.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                    poly.Closed = true;

                    if (Math.Abs(placement.Rotation) > 0.001)
                    {
                        var center = new Point3d(x + w / 2, y + h / 2, 0);
                        poly.TransformBy(Matrix3d.Rotation(
                            placement.Rotation * Math.PI / 180.0, Vector3d.ZAxis, center));
                    }

                    poly.Layer = layerName;
                    btr.AppendEntity(poly);
                    tr.AddNewlyCreatedDBObject(poly, true);
                }

                tr.Commit();
            }
        }

        public void CreateBrickBlock(BrickPlacement placement, string blockName, string layerName)
        {
            // 先检查或创建块定义，然后插入块引用
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);

                // 如果块定义不存在则创建
                if (!bt.Has(blockName))
                {
                    bt.UpgradeOpen();
                    var btrDef = new BlockTableRecord();
                    btrDef.Name = blockName;

                    // 在块定义中创建矩形
                    var poly = new Polyline();
                    poly.AddVertexAt(0, new Point2d(0, 0), 0, 0, 0);
                    poly.AddVertexAt(1, new Point2d(placement.ActualLength, 0), 0, 0, 0);
                    poly.AddVertexAt(2, new Point2d(placement.ActualLength, placement.ActualWidth), 0, 0, 0);
                    poly.AddVertexAt(3, new Point2d(0, placement.ActualWidth), 0, 0, 0);
                    poly.Closed = true;
                    poly.Layer = "0";

                    btrDef.AppendEntity(poly);
                    bt.Add(btrDef);
                    tr.AddNewlyCreatedDBObject(btrDef, true);
                }

                // 插入块引用到模型空间
                var btrModel = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
                var blockRef = new BlockReference(
                    new Point3d(placement.Position.X, placement.Position.Y, 0),
                    bt[blockName]);

                if (Math.Abs(placement.Rotation) > 0.001)
                {
                    blockRef.Rotation = placement.Rotation * Math.PI / 180.0;
                }

                blockRef.Layer = layerName;
                btrModel.AppendEntity(blockRef);
                tr.AddNewlyCreatedDBObject(blockRef, true);
                tr.Commit();
            }
        }

        public void CreateBrickHatch(BrickPlacement placement, string patternName, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 创建边界多段线
                double x = placement.Position.X;
                double y = placement.Position.Y;
                double w = placement.ActualLength;
                double h = placement.ActualWidth;

                var boundary = new Polyline();
                boundary.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
                boundary.AddVertexAt(1, new Point2d(x + w, y), 0, 0, 0);
                boundary.AddVertexAt(2, new Point2d(x + w, y + h), 0, 0, 0);
                boundary.AddVertexAt(3, new Point2d(x, y + h), 0, 0, 0);
                boundary.Closed = true;

                btr.AppendEntity(boundary);
                tr.AddNewlyCreatedDBObject(boundary, true);

                // 创建 Hatch
                var objIds = new ObjectIdCollection { boundary.ObjectId };
                var hatch = new Hatch();
                hatch.SetHatchPattern(HatchPatternType.PreDefined, patternName);
                hatch.Associative = true;
                hatch.Layer = layerName;

                btr.AppendEntity(hatch);
                tr.AddNewlyCreatedDBObject(hatch, true);
                hatch.AppendLoop(HatchLoopTypes.External, objIds);
                hatch.EvaluateHatch(true);

                tr.Commit();
            }
        }

        public void DrawJointLines(double x, double y, double length, double jointWidth, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var line = new Line(
                    new Point3d(x, y, 0),
                    new Point3d(x + length, y, 0));
                line.Layer = layerName;

                btr.AppendEntity(line);
                tr.AddNewlyCreatedDBObject(line, true);
                tr.Commit();
            }
        }

        public void DrawRegionBoundary(List<Point2D> boundaryPoints, string layerName)
        {
            if (boundaryPoints == null || boundaryPoints.Count < 2) return;

            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                for (int i = 0; i < boundaryPoints.Count; i++)
                {
                    var start = boundaryPoints[i];
                    var end = boundaryPoints[(i + 1) % boundaryPoints.Count];

                    var line = new Line(
                        new Point3d(start.X, start.Y, 0),
                        new Point3d(end.X, end.Y, 0));
                    line.Layer = layerName;
                    line.ColorIndex = 1; // 红色边界

                    btr.AppendEntity(line);
                    tr.AddNewlyCreatedDBObject(line, true);
                }

                tr.Commit();
            }
        }

        public void DrawOpeningMarkers(List<OpeningInfo> openings, string layerName)
        {
            if (openings == null || openings.Count == 0) return;

            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                foreach (var opening in openings)
                {
                    var box = opening.Boundary;
                    var poly = new Polyline();
                    poly.AddVertexAt(0, new Point2d(box.MinX, box.MinY), 0, 0, 0);
                    poly.AddVertexAt(1, new Point2d(box.MaxX, box.MinY), 0, 0, 0);
                    poly.AddVertexAt(2, new Point2d(box.MaxX, box.MaxY), 0, 0, 0);
                    poly.AddVertexAt(3, new Point2d(box.MinX, box.MaxY), 0, 0, 0);
                    poly.Closed = true;
                    poly.Layer = layerName;
                    poly.ColorIndex = 2; // 黄色洞口标记

                    btr.AppendEntity(poly);
                    tr.AddNewlyCreatedDBObject(poly, true);
                }

                tr.Commit();
            }
        }

        public void CreateAnnotation(Point2D position, string text, double height, string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                var dbText = new DBText();
                dbText.Position = new Point3d(position.X, position.Y, 0);
                dbText.TextString = text;
                dbText.Height = height;
                dbText.Layer = layerName;

                btr.AppendEntity(dbText);
                tr.AddNewlyCreatedDBObject(dbText, true);
                tr.Commit();
            }
        }

        public void CreateMaterialTable(Point2D insertPoint, MaterialSummary summary)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)tr.GetObject(_db.BlockTableId, OpenMode.ForRead);
                var btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                // 创建 Table
                var table = new Table();
                table.SetSize(3, 5, 40, 10); // 3行5列
                table.Position = new Point3d(insertPoint.X, insertPoint.Y, 0);

                // 表头
                table.Cells[0, 0].TextString = "规格";
                table.Cells[0, 1].TextString = "整砖数";
                table.Cells[0, 2].TextString = "切割砖数";
                table.Cells[0, 3].TextString = "总用量";
                table.Cells[0, 4].TextString = "损耗率";

                // 数据行
                table.Cells[1, 0].TextString = summary.SpecName;
                table.Cells[1, 1].TextString = summary.WholeBrickCount.ToString();
                table.Cells[1, 2].TextString = summary.CutBrickCount.ToString();
                table.Cells[1, 3].TextString = summary.TotalCount.ToString();
                table.Cells[1, 4].TextString = $"{summary.WasteRate:F1}%";

                table.Layer = Layers.Annotation;
                btr.AppendEntity(table);
                tr.AddNewlyCreatedDBObject(table, true);
                tr.Commit();
            }
        }

        public bool ExportToExcel(LayoutResult result, string filePath)
        {
            try
            {
                var exporter = new Fanben.BrickPlugin.Data.ExcelExporter();
                return exporter.ExportToExcel(result, filePath);
            }
            catch
            {
                return false;
            }
        }
    }
}
