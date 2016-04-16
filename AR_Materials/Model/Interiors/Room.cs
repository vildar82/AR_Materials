using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Geometry;
using Autodesk.AutoCAD.Geometry;
using AcadLib.Errors;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Помещение - набор из полилинии и вставленных внутри блоков номера и видов.
    /// </summary>
    public class Room
    {    
        public ObjectId IdPolyline { get; private set; }
        public List<View> Views { get; private set; } = new List<View>();
        public Number Number { get;  set; }
        public List<Roll> Rolls { get; private set; } = new List<Roll>();
        public Matrix3d TransToModel { get; set; }
        /// <summary>
        /// Полная длина разверток помещения
        /// </summary>
        public double Length { get; internal set; }
        /// <summary>
        /// Максимальная высота развертки
        /// </summary>
        public double Height { get; internal set; }

        private HashSet<Roll> hRolls = new HashSet<Roll>();

        public Room(Polyline pl, BlockReference blRefFlat)
        {
            IdPolyline = pl.Id;
            TransToModel = blRefFlat.BlockTransform;
        }

        /// <summary>
        /// Определяет принадлежность точки помещению
        /// </summary>        
        public bool IsPointInRoom(Point3d pt)
        {
            using (var pl = IdPolyline.GetObject(OpenMode.ForRead, false, true) as Polyline)
            {
                if (pl.IsPointInsidePolygon(pt))
                {                    
                    return true;
                }
            }
            return false;
        }

        public void CalcRolls()
        {            
            // Вычисление разверток помещения
            using (var pl = IdPolyline.GetObject(OpenMode.ForRead, false, true) as Polyline)
            {
                Roll roll = new Roll(this);
                var segSomes = new List<RollSegment>();
                Vector2d direction = Vector2d.XAxis;
                RollSegment segFirst = null;
                RollSegment segLast = null;
                RollSegment seg = null;
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {                    
                    var segType = pl.GetSegmentType(i);                    
                    if (segType == SegmentType.Line)
                    {
                        var segLine = pl.GetLineSegment2dAt(i);
                        var segLen = segLine.Length;
                        // Создане сегмента и определение вида который в упор смотрит на этот сегмент
                        seg = new RollSegment(this, segLine, i);

                        // Если этот сегмент не сонаправлен предыдущему                        
                        if (checkNewRoll(segSomes, direction, seg))
                        {
                            roll.AddSegments(segSomes);
                            AddRoll(roll);
                            roll = new Roll(this);
                            segSomes = new List<RollSegment>();
                        }

                        if (!seg.IsPartition)
                        {
                            direction = seg.Direction;
                            if (segFirst == null)
                                segFirst = seg;
                            segLast = seg;
                        }

                        if (seg.View != null && !seg.IsPartition)
                        {
                            // определен вид.  
                            roll.Num = seg.View.Number;
                            roll.View = seg.View;
                        }
                        segSomes.Add(seg);
                    }
                    else if (segType == SegmentType.Arc)
                    {
                        var segBounds = pl.GetArcSegmentAt(i).BoundBlock;
                        Extents3d segExt = new Extents3d(segBounds.GetMinimumPoint(), segBounds.GetMaximumPoint());
                        // Пока ошибка
                        Inspector.AddError($"Дуговой сегмент полилинии помещения. Пока не обрабатываеься.",
                            segExt, TransToModel, System.Drawing.SystemIcons.Warning);
                    }
                    else
                    {
                        Inspector.AddError($"Cегмент полилинии помещения типа {segType}. Пока не обрабатываеься.",
                            pl.GeometricExtents, TransToModel, System.Drawing.SystemIcons.Warning);
                    }
                }
                // Проверка последнего сегмента, если для него еще не определена развертка
                if (seg.Roll == null)
                {
                    // Проверка что последний сегмент входит в первую развертку помещения
                    Roll rollFirst = segFirst.Roll;
                    if (seg.IsPartition)
                    {
                        //rolFirst.AddSegments(new List<RollSegment>() { seg });
                        seg = segLast;
                        direction = seg.Direction;
                    }

                    // Проверка - может это сегмент первой развертки в помещении
                    var dirFirst = segFirst.Direction;
                    if (checkNewRoll(segSomes, dirFirst, seg))
                    {
                        // Новая развертка
                        roll.AddSegments(segSomes);
                        AddRoll(roll);
                    }
                    else
                    {
                        // Добавляем в первую развертку
                        segSomes.Reverse();
                        rollFirst.AddSegments(segSomes);                        
                    }                              
                }
            }            
        }

        private bool checkNewRoll(List<RollSegment> segSomes, Vector2d direction, RollSegment rollSeg)
        {
            if (segSomes.Count > 0 && !rollSeg.IsPartition && !direction.IsCodirectionalTo(rollSeg.Direction, RollSegment.ToleranceView))
            {                   
                return true;
            }
            return false;
        }

        private void AddRoll(Roll roll)
        {
            if (hRolls.Add(roll))
            {
                Rolls.Add(roll);                
            }            
        }
    }
}
