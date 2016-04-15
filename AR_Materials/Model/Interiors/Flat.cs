using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Квартира или МОП - блок в котором расположены поллинии помещений и блоки номеров и видов
    /// </summary>
    public class Flat
    {
        public ObjectId IdBlRef { get; set; }
        public ObjectId IdBtr { get; set; }
        public string BlName { get; set; }
        public List<Room> Rooms { get; set; }

        public Flat(ObjectId objectId)
        {
            this.IdBlRef = objectId;            
        }

        public Result Define()
        {
            Rooms = new List<Room>();

            var blRefFlat = IdBlRef.GetObject(OpenMode.ForRead, false, true) as BlockReference;
            if (blRefFlat == null)
                return Result.Fail("Это не блок");

            BlName = blRefFlat.GetEffectiveName();
            IdBtr = blRefFlat.BlockTableRecord;

            var btrFlat = IdBtr.GetObject(OpenMode.ForRead) as BlockTableRecord;

            // Перебор объектов внктри блока                
            List<View> views;
            List<Number> numbers;
            IterateEnt(blRefFlat, btrFlat, out views, out numbers);

            // Определение принадлежности видов помещениям
            ViewsOwner(blRefFlat, views);

            // Определение принадлежности номеров помещениям
            NumbersOwner(blRefFlat, numbers);

            return Result.Ok();
        }        

        public void CalcRolls()
        {
            // Вычисление разверток
            foreach (var room in Rooms)
            {
                room.CalcRolls();
            }
        }

        private void IterateEnt(BlockReference blRefFlat, BlockTableRecord btrFlat,
                                out List<View> views,out List<Number> numbers)
        {
            views = new List<View>();
            numbers = new List<Number>();
            foreach (var idEnt in btrFlat)
            {
                var ent = idEnt.GetObject(OpenMode.ForRead, false, true);

                var entPl = ent as Polyline;
                var entBlRef = ent as BlockReference;

                if (entPl != null && entPl.Area > 0)
                {
                    // Помещение
                    Room room = new Room(entPl, blRefFlat);
                    Rooms.Add(room);
                }
                else if (entBlRef != null)
                {
                    string entBlName = entBlRef.GetEffectiveName();
                    if (entBlName.StartsWith("вид"))
                    {
                        // Блок Вида
                        var blViews = View.GetViews(entBlRef, entBlName, blRefFlat);
                        views.AddRange(blViews);
                    }
                    else if (entBlName.Equals("П_номер помещения", StringComparison.OrdinalIgnoreCase))
                    {
                        // Блок номера помещения
                        Number number = new Number(entBlRef, entBlName);
                        numbers.Add(number);
                    }
                    else
                    {
                        // Неопределенный блок
                        Inspector.AddError($"Неопределенный блок в квартире - {entBlName}",
                        entBlRef, blRefFlat.BlockTransform, System.Drawing.SystemIcons.Warning);
                    }
                }
            }
        }

        private void ViewsOwner(BlockReference blRefFlat, List<View> views)
        {
            var viewsPos = views.GroupBy(v => v.Position, AcadLib.Comparers.Point3dEqualityComparer.Comparer1);
            foreach (var viewPos in viewsPos)
            {
                bool isFind = false;
                foreach (var room in Rooms)
                {
                    if (room.IsPointInRoom(viewPos.Key))
                    {
                        room.Views.AddRange(viewPos);
                        isFind = true;
                        break;
                    }                    
                }
                if (!isFind)
                {
                    // Не определена принадлежность видов к помещению
                    var view = viewPos.First();
                    Inspector.AddError($"Не определена принадлежность вида к помещению",
                        view.IdBlRef, blRefFlat.BlockTransform, System.Drawing.SystemIcons.Error);
                }
            }            
        }

        private void NumbersOwner(BlockReference blRefFlat, List<Number> numbers)
        {            
            foreach (var number in numbers)
            {
                bool isFind = false;
                foreach (var room in Rooms)
                {
                    if (room.IsPointInRoom(number.Position))
                    {
                        room.Number = number;
                        isFind = true;
                        break;
                    }
                }
                if (!isFind)
                {
                    // Не определена принадлежность номера к помещению                    
                    Inspector.AddError($"Не определена принадлежность номера к помещению",
                        number.IdBlRef, blRefFlat.BlockTransform, System.Drawing.SystemIcons.Error);
                }
            }
        }

        /// <summary>
        /// Построение разверток стены
        /// </summary>        
        public void CreateRoll(Point3d ptStart, BlockTableRecord btr)
        {
            Transaction t = btr.Database.TransactionManager.TopTransaction;
            Point2d ptCurRoll = ptStart.Convert2d();
            var textHeight = 3.5 * (1 / btr.Database.Cannoscale.Scale);
            string textValue = string.Empty;
            foreach (var room in Rooms)
            {
                double roomLength = 0;                

                foreach (var roll in room.Rolls.OrderBy(r=>r.Num))
                {
                    Point2d ptCurSegment = ptCurRoll;
                    roll.Height = roll.Segments.Max(s => s.Height);

                    foreach (var segment in roll.Segments)
                    {
                        roll.Length += segment.Length;
                        // Полилиня контура
                        Polyline plRol = new Polyline(4);
                        plRol.SetDatabaseDefaults();
                        plRol.AddVertexAt(0, ptCurSegment, 0, 0, 0);
                        plRol.AddVertexAt(1, new Point2d(ptCurSegment.X, ptCurSegment.Y + segment.Height), 0, 0, 0);
                        plRol.AddVertexAt(2, new Point2d(ptCurSegment.X + segment.Length, ptCurSegment.Y + segment.Height), 0, 0, 0);
                        plRol.AddVertexAt(3, new Point2d(ptCurSegment.X + segment.Length, ptCurSegment.Y), 0, 0, 0);
                        plRol.Closed = true;

                        btr.AppendEntity(plRol);
                        t.AddNewlyCreatedDBObject(plRol, true);

                        ptCurSegment = new Point2d(ptCurSegment.X + segment.Length, ptCurSegment.Y);
                    }
                                        
                    var ptText = new Point3d(ptCurRoll.X + roll.Length * 0.5, ptCurRoll.Y + roll.Height + textHeight, 0);
                    textValue = "Вид - " + (roll.View == null ? "0" : roll.Num.ToString());
                    addText(btr, t, ptCurRoll, textValue, textHeight);

                    ptCurRoll = new Point2d(ptCurRoll.X + roll.Length + 1000, ptCurRoll.Y);
                    roomLength += roll.Length;
                }

                double roomHeight = room.Rolls.Max(r => r.Height);
                var ptTextRoom = new Point3d(ptCurRoll.X + roomLength * 0.5, ptCurRoll.Y + roomHeight + textHeight, 0);
                textValue = "Помещение " + (room.Number == null ? "0" : room.Number.Num.ToString());
                addText(btr, t, ptCurRoll, textValue, textHeight);
            }
        }

        private static void addText(BlockTableRecord btr, Transaction t, Point2d pt, string value, double height)
        {
            // Подпись развертки - номер вида
            DBText text = new DBText();
            text.SetDatabaseDefaults();
            text.Height = height;
            text.TextStyleId = RollUpService.IdTextStylePik;
            text.TextString = value;
            text.HorizontalMode = TextHorizontalMode.TextCenter;
            text.AlignmentPoint = pt.Convert3d();
            text.AdjustAlignment(btr.Database);

            btr.AppendEntity(text);
            t.AddNewlyCreatedDBObject(text, true);            
        }
    }
}
