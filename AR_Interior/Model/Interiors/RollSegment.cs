using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using AcadLib;
using AcadLib.Geometry;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Сегиент развертки
    /// </summary>
    public class RollSegment : IComparable<RollSegment>
    {        
        public Room Room { get; set; }
        public Roll Roll { get; set; }
        public double Length { get; set; }
        public double Height { get; set; }        
        public bool IsPartition { get; set; }        
        /// <summary>
        /// Направление сегмента - от первой точки к конечной
        /// </summary>
        public Vector2d Direction { get; set; }
        public Point2d Center { get; set; }        
        public View View { get; set; }             
        public int Index { get; set; }

        public RollSegment(Room room, LineSegment2d segLine, int index)
        {
            Index = index;
            Room = room;
            Length = segLine.Length;
            Height = Options.Instance.RollHeight;
            Direction = segLine.Direction;            
            Center = segLine.MidPoint;            
            if (Length<Options.Instance.RollSegmentPartitionLength)
            {
                IsPartition = true;
            }
            else
            {
                DefineLookViews(segLine);
            }            
        }

        /// <summary>
        /// Определение видов котроые смотрят на этот сегмент
        /// </summary>
        private void DefineLookViews(LineSegment2d segLine)
        {
            List<View> viewsDefined = new List<View>();
            foreach (var view in Room.Views.Where(v=>v.Segment == null))
            {
                // Проверка перпендикулирности стрелки вида сегменту
                if (!Direction.IsPerpendicularTo(new Vector2d (view.Direction.X, view.Direction.Y), RollUpService.Tolerance))                
                    continue;
                // Проверка попадает ли вид в створ сегмента
                if (!IsOnFront(segLine, view))
                    continue;
                viewsDefined.Add(view);
            }            
            if (viewsDefined.Count == 1)
            {
                View = viewsDefined[0];                
            }
            else if (viewsDefined.Count>1)
            {
                // Подходит тот который ближе к стене
                View = viewsDefined.OrderBy(v => v.DistToSegment).First();                
            }
            if (View != null)            
                View.Segment = this;            
        }

        private bool IsOnFront(LineSegment2d segLine, View view)
        {
            // Опустить перпендикуляр на сегмент            
            try
            {
                var ptRes = segLine.GetNormalPoint(view.Position.Convert2d()).Point;
                Point2d ptNormal = new Point2d(ptRes.X, ptRes.Y);
                view.DistToSegment = (view.Position.Convert2d() - ptRes).Length;
                // Вектор из точки вида на точку перпендикуляра на сегменте
                var vecViewToNormal = ptNormal - view.Position.Convert2d();                
                //var angleVecNormal = vecViewToNormal.AngleOnPlane(new Plane());
                //var angleView = view.Vector.AngleOnPlane(new Plane());
                return view.Direction.IsCodirectionalTo(vecViewToNormal, RollUpService.Tolerance);
            }
            catch
            {
                return false;
            }            
        }

        public int CompareTo(RollSegment other)
        {
            return View.CompareTo(other?.View);
        }
    }
}
