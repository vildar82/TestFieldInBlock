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
            var block = new BlockTableRecord
            {
                Name = Guid.NewGuid().ToString()
            };

            var bt = (BlockTable) db.BlockTableId.GetObject(OpenMode.ForWrite);
            bt.Add(block);
            t.AddNewlyCreatedDBObject(block, true);

            // Добавление окна в блок
            var btrWindowId = bt["Окно"];
            var blRef = new BlockReference(Point3d.Origin, btrWindowId);
            block.AppendEntity(blRef);

            // Добавление атрибутов в блок окна
            AddAttributes(blRef, btrWindowId, t);
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

                using var atrRef = new AttributeReference();
                atrRef.SetAttributeFromBlock(atrDef, blRef.BlockTransform);
                blRef.AttributeCollection.AppendAttribute(atrRef);
                t.AddNewlyCreatedDBObject(atrRef, true);
            }
        }
    }
}