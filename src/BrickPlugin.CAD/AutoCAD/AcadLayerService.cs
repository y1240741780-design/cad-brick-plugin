using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Colors;

namespace Fanben.BrickPlugin.CAD.AutoCAD
{
    /// <summary>
    /// AutoCAD 图层服务实现
    /// </summary>
    public class AcadLayerService : Abstractions.ICADLayerService
    {
        private readonly Database _db;

        public AcadLayerService()
        {
            _db = Application.DocumentManager.MdiActiveDocument.Database;
        }

        public void EnsureLayer(string layerName, short colorIndex = 7)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);

                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();
                    var ltr = new LayerTableRecord();
                    ltr.Name = layerName;
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, colorIndex);
                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }

                tr.Commit();
            }
        }

        public void SetLayerLocked(string layerName, bool locked)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                    ltr.IsLocked = locked;
                }
                tr.Commit();
            }
        }

        public void SetLayerFrozen(string layerName, bool frozen)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    var ltr = (LayerTableRecord)tr.GetObject(lt[layerName], OpenMode.ForWrite);
                    ltr.IsFrozen = frozen;
                }
                tr.Commit();
            }
        }

        public void SetCurrentLayer(string layerName)
        {
            using (var tr = _db.TransactionManager.StartTransaction())
            {
                var lt = (LayerTable)tr.GetObject(_db.LayerTableId, OpenMode.ForRead);
                if (lt.Has(layerName))
                {
                    _db.Clayer = lt[layerName];
                }
                tr.Commit();
            }
        }
    }
}
