using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Номер помещения
    /// </summary>
    public class Number
    {
        public ObjectId IdBlRef { get; set; }
        public Point3d Position { get; set; }
        public string BlName { get; set; }                
        public int Num { get; set; }
        public AttributeInfo AtrNum { get; set; }

        public Number(BlockReference blRef, string blName)
        {
            IdBlRef = blRef.Id;
            BlName = BlName;
            Position = blRef.Position;
            var attrs = AttributeInfo.GetAttrRefs(blRef);
            AtrNum = attrs.Find(a => a.Tag.Equals("NUM", StringComparison.OrdinalIgnoreCase));
            if (AtrNum != null)
            {
                int num;
                Num = int.TryParse(AtrNum.Text, out num) ? num : 0;
            }
        }
    }
}
