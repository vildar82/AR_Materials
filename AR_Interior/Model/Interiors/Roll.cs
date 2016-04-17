using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Развертка стены
    /// </summary>
    public class Roll
    {
        public int Num { get; set; }
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
        public double Height { get; internal set; }

        public Roll(Room room)
        {
            Room = room;
            Segments = new List<RollSegment>();
        }

        public void AddSegments(List<RollSegment> segSomes)
        {
            foreach (var seg in segSomes)
            {
                Segments.Add(seg);
                seg.Roll = this;
            }            
            
            Length += segSomes.Sum(s => s.Length);
            Height = Segments.Max(s => s.Height);
        }
    }
}
