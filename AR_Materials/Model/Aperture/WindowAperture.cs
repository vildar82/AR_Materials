using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model
{
   public class WindowAperture : Aperture
   {
      public WindowAperture(BlockReference blRef) : base(blRef)
      { }
   }
}
