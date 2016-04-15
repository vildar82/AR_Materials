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
        public double Length { get; set; }
        public double Height { get; internal set; }

        public Roll(Room room)
        {
            Room = room;
            Segments = new List<RollSegment>();
        }        
    }
}
