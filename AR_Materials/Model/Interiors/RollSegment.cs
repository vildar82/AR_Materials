﻿using System;
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
        public static Tolerance ToleranceView = new Tolerance(0.1, 200);
        public Room Room { get; set; }
        public double Length { get; set; }
        public double Height { get; set; }        
        public bool IsPartition { get; set; }        
        /// <summary>
        /// Направление сегмента - от первой точки к конечной
        /// </summary>
        public Vector2d Direction { get; set; }
        public Point2d Center { get; set; }        
        public View View { get; set; }             
        // Все виды которые смотрят на этот сегмент
        private List<View> lookViews { get; set; }

        public RollSegment(Room room, LineSegment2d segLine)
        {
            Room = room;
            Length = segLine.Length;
            Height = 2800;
            Direction = segLine.Direction;            
            Center = segLine.MidPoint;            
            if (Length<300)
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
                if (!Direction.IsPerpendicularTo(new Vector2d (view.Vector.X, view.Vector.Y), RollUpService.Tolerance))                
                    continue;
                // Проверка попадает ли вид в створ сегмента
                if (!IsOnFront(segLine, view))
                    continue;
                viewsDefined.Add(view);
            }            
            if (viewsDefined.Count == 1)
            {
                View = viewsDefined[0];
                View.Segment = this;
            }
            else if (viewsDefined.Count>1)
            {
                // Подходит тот который ближе к стене
                View = viewsDefined.OrderBy(v => v.DistToSegment).First();
            }
        }

        private bool IsOnFront(LineSegment2d segLine, View view)
        {
            // Опустить перпендикуляр на сегмент            
            try
            {
                var ptRes = segLine.GetNormalPoint(view.Position.Convert2d(), ToleranceView).Point;
                Point3d ptNormal = new Point3d(ptRes.X, ptRes.Y, 0);
                view.DistToSegment = (view.Position.Convert2d() - ptRes).Length;
                // Вектор из точки вида на точку перпендикуляра на сегменте
                var vecViewToNormal = ptNormal - view.Position;                
                //var angleVecNormal = vecViewToNormal.AngleOnPlane(new Plane());
                //var angleView = view.Vector.AngleOnPlane(new Plane());
                return view.Vector.IsCodirectionalTo(vecViewToNormal, ToleranceView);
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
