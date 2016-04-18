using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Geometry;
using AcadLib;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Errors;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Развертка стены
    /// </summary>
    public class Roll
    {
        public string  Num { get; set; }
        public List<RollSegment> Segments { get; set; }
        public Room Room { get; set; }
        public View View { get; set; }
        /// <summary>
        /// Длина развертки всех сегментов
        /// </summary>
        public double Length { get; set; }
        /// <summary>
        /// Максимальная высота сегмента
        /// </summary>
        public double Height { get; set; }
        public Vector2d Direction { get; set; }
        public Vector2d VectorFromRightToLeft { get; set; }
        public Extents3d Extents { get; set; }

        public Roll(Room room)
        {
            Room = room;
            Segments = new List<RollSegment>();
        }

        public void AddSegments(List<RollSegment> segSomes)
        {
            if (segSomes == null || segSomes.Count == 0)
            {
                return;
            }

            if (Segments.Count == 0)
            {
                Direction = segSomes.First().Direction;
            }

            foreach (var seg in segSomes)
            {
                Segments.Add(seg);
                seg.Roll = this;
            }            
            
            Length += segSomes.Sum(s => s.Length);
            Height = Segments.Max(s => s.Height);            
        }

        /// <summary>
        /// Определение порядка сегментов - Справа-налево со стороны вида
        /// </summary>
        private void FromRightToLeft()
        {
            if (View == null)
                return;
            // Направление справа-налево - определяется по направлению вида - с поворотом на 90градусов по часовой стрелке
            VectorFromRightToLeft = View.Direction.RotateBy(-90d.ToRadians());
            if (!View.Direction.IsCodirectionalTo(VectorFromRightToLeft, RollUpService.Tolerance))
            {
                // Нужно поменять порядок разверток
                Segments.Reverse();
            }
        }

        public void Check()
        {
            if (Segments.Count == 0) return;

            FromRightToLeft();            

            // Проверка определен ли вид для развертки
            if (View == null)
            {
                Extents = getExtents();
                // Ошибка - вид неопределен
                Inspector.AddError($"Неопределен номер вида для развертки.",
                    Extents, Room.IdPolyline,  System.Drawing.SystemIcons.Warning);
            }
        }

        private Extents3d getExtents()
        {
            var pt1 = Segments.First().Center;
            var vec1 = VectorFromRightToLeft.Negate() * 500;
            pt1 = pt1.Add(vec1);

            var pt2 = Segments.Last().Center;
            var vec2 = VectorFromRightToLeft * 500;
            pt2 = pt2.Add(vec2);

            var pt3 = pt1 + Direction.GetPerpendicularVector() * 500;
            var pt4 = pt1 - Direction.GetPerpendicularVector() * 500;

            Extents3d ext = new Extents3d();
            ext.AddPoint(pt1.Convert3d());
            ext.AddPoint(pt2.Convert3d());
            ext.AddPoint(pt3.Convert3d());
            ext.AddPoint(pt4.Convert3d());
            ext.TransformBy(Room.TransToModel);
            return ext;
        }
    }
}
