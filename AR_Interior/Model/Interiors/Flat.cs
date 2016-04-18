using System;
using System.Collections;
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
        public string Name { get; set; }
        public List<Room> Rooms { get; set; }    
        public Matrix3d TransToModel { get; set; }
        public double DrawHeight { get; internal set; }
        public bool HasRoll { get; private set; }

        public Result Define(IEnumerable ids, Matrix3d transToModel)
        {
            TransToModel = transToModel;
            Rooms = new List<Room>();
            
            List<View> views;
            List<Number> numbers;
            IterateEnt(ids, out views, out numbers);

            // Определение принадлежности видов помещениям
            ViewsOwner(views);

            // Определение принадлежности номеров помещениям
            NumbersOwner(numbers);

            CalcRolls();

            return Result.Ok();
        }        

        public void CalcRolls()
        {
            // Вычисление разверток
            foreach (var room in Rooms)
            {
                room.CalcRolls();
                if (room.HasRoll)
                {
                    HasRoll = true;
                    room.Length = room.Rolls.Sum(r => r.Length);
                    room.Height = room.Rolls.Max(r => r.Height);
                    room.DrawLength = room.Length + room.Rolls.Count * Options.Instance.RollViewOffset;
                    room.DrawHeight = room.Height + 2500;
                }
            }
        }

        private void IterateEnt(IEnumerable ids,out List<View> views,out List<Number> numbers)
        {
            var opt = Options.Instance;
            views = new List<View>();
            numbers = new List<Number>();
            foreach (ObjectId idEnt in ids)
            {
                var ent = idEnt.GetObject(OpenMode.ForRead, false, true);

                var entPl = ent as Polyline;
                var entBlRef = ent as BlockReference;

                if (entPl != null && entPl.Area > 0)
                {
                    // Помещение
                    Room room = new Room(this,  entPl);
                    Rooms.Add(room);
                }
                else if (entBlRef != null)
                {
                    string entBlName = entBlRef.GetEffectiveName();

                    // Блок Вида
                    if (entBlName.StartsWith(opt.BlockNameViewStart))
                    {                        
                        var blViews = View.GetViews(entBlRef, entBlName, TransToModel);
                        views.AddRange(blViews);
                    }
                    // Блок номера помещения
                    else if (entBlName.Equals(opt.BlockNameRoomNumber, StringComparison.OrdinalIgnoreCase))
                    {
                        
                        Number number = new Number(entBlRef, entBlName);
                        numbers.Add(number);
                    }
                    else
                    {
                        //// Неопределенный блок
                        //Inspector.AddError($"Неопределенный блок в квартире - {entBlName}",
                        //entBlRef, TransToModel, System.Drawing.SystemIcons.Warning);
                    }
                }
            }
        }

        private void ViewsOwner(List<View> views)
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
                        view.IdBlRef, TransToModel, System.Drawing.SystemIcons.Error);
                }
            }            
        }

        private void NumbersOwner(List<Number> numbers)
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
                        number.IdBlRef, TransToModel, System.Drawing.SystemIcons.Error);
                }
            }
        }

        /// <summary>
        /// Построение разверток стены
        /// </summary>        
        public void CreateRoll(Point3d ptStart, BlockTableRecord btr)
        {
            var rooms = Rooms.Where(r => r.HasRoll).OrderBy(r => r.Number?.Num);
            if (!rooms.Any())
            {
                return;
            }

            Options opt = Options.Instance;
            Transaction t = btr.Database.TransactionManager.TopTransaction;
            Point2d ptRoom = ptStart.Convert2d();
            var textHeight = 2.5 * (1 / btr.Database.Cannoscale.Scale);
            string textValue = string.Empty;

            // Подпись квартиры
            var ptTextFlat = new Point3d(ptStart.X, ptStart.Y+DrawHeight-500, 0);
            addText(btr, t, ptTextFlat, Name, textHeight, TextHorizontalMode.TextLeft);

            foreach (var room in rooms)
            {
                Point2d ptRoll = ptRoom;                

                foreach (var roll in room.Rolls.OrderBy(r=>r.Num))
                {
                    Point2d ptSegment = ptRoll;                    

                    foreach (var segment in roll.Segments)
                    {
                        // Полилиня сегмента
                        addRectangle(btr, t, ptSegment, segment.Length, segment.Height);

                        // Размер
                        Point3d ptDim1 = ptSegment.Convert3d();
                        Point3d ptDim2 = new Point3d(ptDim1.X + segment.Length, ptDim1.Y, 0);
                        Point3d ptDimLine = new Point3d(ptDim1.X + segment.Length * 0.5, ptDim1.Y - 500, 0);
                        addDim(btr, t, ptDim1, ptDim2, ptDimLine);

                        //// Для Тестов - индекс сегмента
                        //var ptSegCenter = new Point2d(ptSegment.X + segment.Length * 0.5, ptSegment.Y + segment.Height * 0.5);
                        //addText(btr, t, ptSegCenter, segment.Index.ToString(), textHeight);

                        ptSegment = new Point2d(ptSegment.X + segment.Length, ptSegment.Y);
                    }

                    var ptText = new Point3d(ptRoll.X + roll.Length * 0.5, ptRoll.Y + roll.Height + textHeight,0);
                    textValue = "Вид-" + (roll.View == null ? "0" : roll.Num.ToString());
                    addText(btr, t, ptText, textValue, textHeight);

                    ptRoll = new Point2d(ptRoll.X + roll.Length + opt.RollViewOffset, ptRoll.Y);                    
                }

                // Полилиня помещения                
                var ptRoomRectangle = new Point2d(ptRoom.X - 500, ptRoom.Y - 1000);                
                addRectangle(btr, t, ptRoomRectangle, room.DrawLength + 1000, room.DrawHeight);
                
                var ptTextRoom = new Point3d(ptRoom.X + room.DrawLength * 0.5, ptRoom.Y + room.Height+1000 + textHeight, 0);
                textValue = "Помещение " + (room.Number == null ? "0" : room.Number.Num.ToString());
                addText(btr, t, ptTextRoom, textValue, textHeight);

                ptRoom = new Point2d(ptRoom.X+ room.DrawLength + 2000, ptRoom.Y);
            }
        }

        private static void addRectangle(BlockTableRecord btr, Transaction t, Point2d ptSegment, double length, double height)
        {
            Polyline plRol = new Polyline(4);
            plRol.SetDatabaseDefaults();
            plRol.AddVertexAt(0, ptSegment, 0, 0, 0);
            plRol.AddVertexAt(1, new Point2d(ptSegment.X, ptSegment.Y + height), 0, 0, 0);
            plRol.AddVertexAt(2, new Point2d(ptSegment.X + length, ptSegment.Y + height), 0, 0, 0);
            plRol.AddVertexAt(3, new Point2d(ptSegment.X + length, ptSegment.Y), 0, 0, 0);
            plRol.Closed = true;

            btr.AppendEntity(plRol);
            t.AddNewlyCreatedDBObject(plRol, true);            
        }

        private static RotatedDimension addDim(BlockTableRecord btr, Transaction t,
                Point3d ptPrev, Point3d ptNext, Point3d ptDimLine, double rotation = 0)
        {            

            var dim = new RotatedDimension(rotation.ToRadians(), ptPrev, ptNext, ptDimLine, "", RollUpService.IdDimStylePik);            
            dim.Dimscale = 100;
            btr.AppendEntity(dim);
            t.AddNewlyCreatedDBObject(dim, true);
            return dim;
        }

        private static void addText(BlockTableRecord btr, Transaction t, Point3d pt, string value, double height,
            TextHorizontalMode horMode = TextHorizontalMode.TextCenter)
        {
            // Подпись развертки - номер вида
            using (DBText text = new DBText())
            {
                text.SetDatabaseDefaults();
                text.Height = height;
                text.TextStyleId = RollUpService.IdTextStylePik;
                text.TextString = value;
                if (horMode == TextHorizontalMode.TextLeft)
                {
                    text.Position = pt;
                }
                else
                {
                    text.HorizontalMode = horMode;
                    text.AlignmentPoint = pt;
                    text.AdjustAlignment(btr.Database);
                }                

                btr.AppendEntity(text);
                t.AddNewlyCreatedDBObject(text, true);
            }
        }
    }
}
