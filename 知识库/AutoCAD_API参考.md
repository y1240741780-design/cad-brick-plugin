# AutoCAD .NET API 参考（排砖插件相关）

## 核心命名空间

| 命名空间 | 用途 |
|---------|------|
| `Autodesk.AutoCAD.Runtime` | 命令注册 `[CommandMethod]` |
| `Autodesk.AutoCAD.ApplicationServices` | 应用程序/文档管理 |
| `Autodesk.AutoCAD.DatabaseServices` | 数据库操作（实体/图层/块表） |
| `Autodesk.AutoCAD.EditorInput` | 用户交互（选择/输入） |
| `Autodesk.AutoCAD.Geometry` | 几何类型（Point3d/Matrix3d） |
| `Autodesk.AutoCAD.Windows` | PaletteSet 可停靠面板 |

## 常用 API

### 获取当前文档
```csharp
Document doc = Application.DocumentManager.MdiActiveDocument;
Database db = doc.Database;
Editor ed = doc.Editor;
```

### 事务操作
```csharp
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
    // ... 添加实体
    tr.Commit();
}
```

### 创建多段线
```csharp
Polyline poly = new Polyline();
poly.AddVertexAt(0, new Point2d(x, y), 0, 0, 0);
poly.Closed = true;
poly.Layer = "BRICK-OUTLINE";
btr.AppendEntity(poly);
tr.AddNewlyCreatedDBObject(poly, true);
```

### 用户选择
```csharp
PromptSelectionOptions opts = new PromptSelectionOptions();
opts.MessageForAdding = "\n选择多段线:";
SelectionFilter filter = new SelectionFilter(new TypedValue[] {
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
});
PromptSelectionResult sel = ed.GetSelection(opts, filter);
```

### 创建 PaletteSet
```csharp
PaletteSet ps = new PaletteSet("标题", new Guid("..."));
ps.Add("页签", userControl);
ps.Visible = true;
```

## 版本兼容性

| AutoCAD | .NET Framework | API 变化 |
|---------|---------------|----------|
| 2010–2012 | 3.5 | 部分 API 不同 |
| 2013–2014 | 4.0 | PaletteSet 支持 |
| 2015–2024 | 4.5–4.8 | 主流 API 稳定 |
| 2025–2026 | 8.0 | 新命名空间路径 |
