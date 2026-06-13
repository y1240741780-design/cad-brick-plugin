using System.Collections.Generic;
using Fanben.BrickPlugin.CAD.Abstractions;
using Fanben.BrickPlugin.Core.Models;

using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace Fanben.BrickPlugin.CAD.AutoCAD
{
    /// <summary>
    /// AutoCAD 选择服务实现
    /// </summary>
    public class AcadSelectionService : ICADSelectionService
    {
        private readonly Document _doc;
        private readonly Database _db;
        private readonly Editor _ed;

        public AcadSelectionService()
        {
            _doc = Application.DocumentManager.MdiActiveDocument;
            _db = _doc.Database;
            _ed = _doc.Editor;
        }

        public List<List<Point2D>> SelectClosedPolylines(string prompt)
        {
            var result = new List<List<Point2D>>();

            var options = new PromptSelectionOptions();
            options.MessageForAdding = $"\n{prompt}";
            options.SingleOnly = false;

            var filter = new SelectionFilter(
                new TypedValue[] { new TypedValue((int)DxfCode.Start, "LWPOLYLINE") });

            var selection = _ed.GetSelection(options, filter);
            if (selection.Status != PromptStatus.OK)
                return result;

            using (var tr = _db.TransactionManager.StartTransaction())
            {
                foreach (SelectedObject selObj in selection.Value)
                {
                    var poly = tr.GetObject(selObj.ObjectId, OpenMode.ForRead) as Polyline;
                    if (poly == null || !poly.Closed) continue;

                    var points = new List<Point2D>();
                    for (int i = 0; i < poly.NumberOfVertices; i++)
                    {
                        var pt = poly.GetPoint2dAt(i);
                        points.Add(new Point2D(pt.X, pt.Y));
                    }
                    result.Add(points);
                }
                tr.Commit();
            }

            return result;
        }

        public Point2D? PickPoint(string prompt)
        {
            var options = new PromptPointOptions($"\n{prompt}");
            var pointResult = _ed.GetPoint(options);

            if (pointResult.Status != PromptStatus.OK)
                return null;

            return new Point2D(pointResult.Value.X, pointResult.Value.Y);
        }

        public List<List<Point2D>> SelectOpeningPolylines(string prompt)
        {
            // 门窗洞口也是闭合多段线，复用选择逻辑
            return SelectClosedPolylines(prompt);
        }

        public string GetCurrentDocumentPath()
        {
            return _doc.Name;
        }
    }
}
