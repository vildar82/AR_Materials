using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model
{
   public class DoorAperture : Aperture
   {
      public DoorAperture(BlockReference blRef) : base(blRef)
      { }
   }
}
