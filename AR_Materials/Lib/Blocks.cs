using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials
{
   public static class Blocks
   {      
      public static string GetEffectiveName(this BlockReference br)
      {
         using (var btr = br.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord)
         {
            return btr.Name; 
         }
      }
   }
}
