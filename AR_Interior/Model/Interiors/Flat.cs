﻿using System;
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
                room.Length = room.Rolls.Sum(r => r.Length);
                room.Height = room.Rolls.Max(r => r.Height);
                room.DrawLength = room.Length + room.Rolls.Count * Options.Instance.RollViewOffset;
                room.DrawHeight = room.Height + 2500;
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
            Options opt = Options.Instance;
            Transaction t = btr.Database.TransactionManager.TopTransaction;
            Point2d ptRoom = ptStart.Convert2d();
            var textHeight = 2.5 * (1 / btr.Database.Cannoscale.Scale);
            string textValue = string.Empty;
            foreach (var room in Rooms.OrderBy(r=>r.Number?.Num))
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

                        // Для Тестов - индекс сегмента
                        var ptSegCenter = new Point2d(ptSegment.X + segment.Length * 0.5, ptSegment.Y + segment.Height * 0.5);
                        addText(btr, t, ptSegCenter, segment.Index.ToString(), textHeight);

                        ptSegment = new Point2d(ptSegment.X + segment.Length, ptSegment.Y);
                    }

                    var ptText = new Point2d(ptRoll.X + roll.Length * 0.5, ptRoll.Y + roll.Height + textHeight);
                    textValue = "Вид-" + (roll.View == null ? "0" : roll.Num.ToString());
                    addText(btr, t, ptText, textValue, textHeight);

                    ptRoll = new Point2d(ptRoll.X + roll.Length + opt.RollViewOffset, ptRoll.Y);                    
                }

                // Полилиня помещения                
                var ptRoomRectangle = new Point2d(ptRoom.X - 500, ptRoom.Y - 1000);                
                addRectangle(btr, t, ptRoomRectangle, room.DrawLength + 1000, room.DrawHeight);
                
                var ptTextRoom = new Point2d(ptRoom.X + room.DrawLength * 0.5, ptRoom.Y + room.Height+1000 + textHeight);
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