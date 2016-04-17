using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Построение разверток стен
    /// </summary>
    public static class RollUpService
    {
        public static Tolerance Tolerance { get; set; } = new Tolerance(0.1, 1);
        public static Document Doc { get; private set; }
        public static Editor Ed { get; private set; }
        public static Database Db { get; private set; }
        public static ObjectId IdTextStylePik { get; set; }
        public static ObjectId IdDimStylePik { get; set; }

        /// <summary>
        /// Создание развертки для одного блока с помещениями
        /// </summary>
        public static void CreateRollUp()
        {
            Doc = Application.DocumentManager.MdiActiveDocument;
            Ed = Doc.Editor;
            Db = Doc.Database;

            using (var t = Db.TransactionManager.StartTransaction())
            {
                // Выбор помещения
                var flats = defineFlats();
                if (flats.Count ==0)                
                    throw new Exception("Развертки не определены.");                

                IdTextStylePik = Db.GetTextStylePIK();
                IdDimStylePik = Db.GetDimStylePIK();                

                //// Temp - построения для проверки
                //test(flat);

                // Точка вставки разверток квартиры
                double totalLen=0;
                double totalHeight =0;
                flats.ForEach(f=>
                {
                    totalLen += f.Rooms.Sum(r => r.DrawLength) + f.Rooms.Count * 1000;
                    f.DrawHeight = f.Rooms.Max(r => r.DrawHeight) + 1000;
                    totalHeight += f.DrawHeight;
                });               
                
                Point3d ptStart = promptStartPoint(totalLen, totalHeight);
                //Point3d ptStart = Ed.GetPointWCS("Точка вставки");

                // Построение разверток
                using (var cs = Db.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord)
                {
                    foreach (var flat in flats)
                    {
                        flat.CreateRoll(ptStart, cs);
                        ptStart = new Point3d(ptStart.X, ptStart.Y + flat.DrawHeight,0);
                    }                     
                }
                t.Commit();
            }
        }       

        private static List<Flat> defineFlats()
        {
            List<Flat> flats = new List<Flat>();
            var selOpt = new PromptSelectionOptions();//"Выбор блока квартиры"
            selOpt.MessageForAdding = "\nВыбор помещений с видами или блоков квартир:";            
            var selRes = Ed.GetSelection(selOpt);
            if (selRes.Status != PromptStatus.OK)
            {
                throw new Exception(AcadLib.General.CanceledByUser);
            }

            List<ObjectId> ids = new List<ObjectId>();
            foreach (var item in selRes.Value.GetObjectIds())
            {
                if (item.ObjectClass.Name == "AcDbBlockReference")
                {
                    var blRefFlat = item.GetObject(OpenMode.ForRead, false, true) as BlockReference;
                    var btrFlat = blRefFlat.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;
                    Flat flat = new Flat();
                    flat.Name = btrFlat.Name;
                    flat.Define(btrFlat, blRefFlat.BlockTransform);
                    if (flat.HasRoll)
                    {                        
                        flats.Add(flat);
                    }
                }
                ids.Add(item);
            }

            if (ids.Count>0)
            {
                Flat flatModel = new Flat();
                flatModel.Name = "Model";
                flatModel.Define(ids, Matrix3d.Identity);
                if (flatModel.HasRoll)
                {                    
                    flats.Add(flatModel);
                }
            }
            return flats;            
        }

        private static void test(Flat flat)
        {
            var cs = Db.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            Transaction t = Db.TransactionManager.TopTransaction;

            //var flatBlRef = flat.IdBlRef.GetObject(OpenMode.ForRead) as BlockReference;

            foreach (var room in flat.Rooms)
            {
                //// Построение стрелок видов
                //foreach (var view in room.Views)
                //{
                //    Point3d pt1 = view.Position.TransformBy(flatBlRef.BlockTransform);
                //    Point3d pt2 = pt1.Add(view.Vector * 100);
                //    Line line = new Line(pt1, pt2);
                //    cs.AppendEntity(line);
                //    t.AddNewlyCreatedDBObject(line, true);
                //}

                //// Построение стрелок стен
                //foreach (var roll in room.Rolls)
                //{
                //    foreach (var seg in roll.Segments)
                //    {
                //        var pt1 = seg.Center.Convert3d().TransformBy(flatBlRef.BlockTransform);
                //        var pt2 = (seg.Center + seg.Direction.GetPerpendicularVector() * 1000).Convert3d().TransformBy(flatBlRef.BlockTransform);
                //        Line line = new Line(pt1, pt2);
                //        cs.AppendEntity(line);
                //        t.AddNewlyCreatedDBObject(line, true);
                //    }
                //}

                //// Подпись номеров точек в полилинии и их координат
                //using (var btrFlat = flatBlRef.BlockTableRecord.GetObject(OpenMode.ForWrite) as BlockTableRecord)
                //{
                //    using (var pl = room.IdPolyline.GetObject(OpenMode.ForRead) as Polyline)
                //    {
                //        for (int i = 0; i < pl.NumberOfVertices; i++)
                //        {
                //            var segType = pl.GetSegmentType(i);
                //            if (segType == SegmentType.Line)
                //            {
                //                var segLine = pl.GetLineSegment2dAt(i);
                //                var ptText = segLine.MidPoint.Convert3d();
                //                DBText text = new DBText();
                //                text.SetDatabaseDefaults();
                //                text.Height = 300;
                //                text.TextString = i.ToString();
                //                text.Position = ptText;

                //                btrFlat.AppendEntity(text);
                //                t.AddNewlyCreatedDBObject(text, true);
                //            }
                //        }
                //    }
                //}
            }
        }

        private static Point3d promptStartPoint(double len, double height)
        {
            AcadLib.Jigs.RectangleJig jigRect = new AcadLib.Jigs.RectangleJig(len, height);
            var res = Ed.Drag(jigRect);
            if (res.Status != PromptStatus.OK)
                throw new Exception(AcadLib.General.CanceledByUser);
            return jigRect.Position;
        }
    }
}