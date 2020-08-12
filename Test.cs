using System;
using System.Linq;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace TestFieldInBlock
{
    public class Test
    {
        [CommandMethod(nameof(TestCreateBlock))]
        public void TestCreateBlock()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            using var t = doc.TransactionManager.StartTransaction();

            // Создане нового блока
            var btr = new BlockTableRecord
            {
                Name = Guid.NewGuid().ToString()
            };

            var bt = (BlockTable) db.BlockTableId.GetObject(OpenMode.ForWrite);
            bt.Add(btr);
            t.AddNewlyCreatedDBObject(btr, true);

            // Добавление окна в блок
            var btrWindowId = bt["Окно"];
            var blRefWindow = new BlockReference(Point3d.Origin, btrWindowId);
            btr.AppendEntity(blRefWindow);
            t.AddNewlyCreatedDBObject(blRefWindow, true);

            // Добавление атрибутов в блок окна
            AddAttributes(blRefWindow, btrWindowId, t);

            // Добавление полилини котнура в блок
            var pl = new Polyline();
            pl.AddVertexAt(0, new Point2d(-1000, 0), 0, 0, 0);
            pl.AddVertexAt(0, new Point2d(1000, 0), 0, 0, 0);
            pl.AddVertexAt(0, new Point2d(1000, 300), 0, 0, 0);
            pl.AddVertexAt(0, new Point2d(-1000, 300), 0, 0, 0);
            pl.Closed = true;
            btr.AppendEntity(pl);
            t.AddNewlyCreatedDBObject(pl, true);

            // Вставка созданного блока
            var pt = doc.Editor.GetPoint("Точка вставки").Value;
            var blRef = new BlockReference(pt, btr.Id);
            var cs = (BlockTableRecord) db.CurrentSpaceId.GetObject(OpenMode.ForWrite);
            cs.AppendEntity(blRef);
            t.AddNewlyCreatedDBObject(blRef, true);

            t.Commit();
        }

        private void AddAttributes(BlockReference blRef, ObjectId btrId, Transaction t)
        {
            var btr = (BlockTableRecord) btrId.GetObject(OpenMode.ForRead);
            var atrDefs = btr.Cast<ObjectId>()
                .Select(s => s.GetObject(OpenMode.ForRead, false, true) as AttributeDefinition)
                .Where(w => w != null).ToList();

            foreach (var atrDef in atrDefs)
            {
                if (atrDef.Constant)
                    continue;

                var atrRef = new AttributeReference();
                blRef.AttributeCollection.AppendAttribute(atrRef);
                t.AddNewlyCreatedDBObject(atrRef, true);
                atrRef.SetAttributeFromBlock(atrDef, blRef.BlockTransform);
            }
        }
    }
}