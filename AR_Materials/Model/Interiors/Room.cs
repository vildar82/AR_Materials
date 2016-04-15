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
        public List<View> Views { get; private set; }
        public Number Number { get;  set; }
        public List<Roll> Rolls { get; set; }
        public Matrix3d TransToModel { get; set; }

        public Room(Polyline pl, BlockReference blRefFlat)
        {
            IdPolyline = pl.Id;
            TransToModel = blRefFlat.BlockTransform;
            Views = new List<View>();            
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
            Rolls = new List<Roll>();            
            // Вычисление разверток помещения
            using (var pl = IdPolyline.GetObject(OpenMode.ForRead, false, true) as Polyline)
            {
                Roll roll = new Roll(this);
                var segSomes = new List<RollSegment>();
                Vector2d direction = Vector2d.XAxis;
                for (int i = 0; i < pl.NumberOfVertices; i++)
                {                    
                    var segType = pl.GetSegmentType(i);                    
                    if (segType == SegmentType.Line)
                    {
                        var segLine = pl.GetLineSegment2dAt(i);
                        var segLen = segLine.Length;                        
                        // Создане сегмента и определение вида который в упор смотрит на этот сегмент
                        var rollSeg = new RollSegment(this, segLine);

                        // Если этот сегмент не сонаправлен предыдущему                        
                        if (segSomes.Count>0 && !rollSeg.IsPartition && !direction.IsCodirectionalTo(rollSeg.Direction, RollSegment.ToleranceView))
                        {
                            roll.Segments.AddRange(segSomes);
                            Rolls.Add(roll);
                            roll = new Roll(this);
                            segSomes = new List<RollSegment>();
                        }
                        direction = rollSeg.Direction;

                        if (rollSeg.View != null && !rollSeg.IsPartition)
                        {                         
                            // определен вид.  
                            roll.Num = rollSeg.View.Number;
                            roll.View = rollSeg.View;
                        }
                        segSomes.Add(rollSeg);
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
            }            
        }
    }
}
