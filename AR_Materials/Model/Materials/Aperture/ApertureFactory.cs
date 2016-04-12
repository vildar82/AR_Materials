using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model
{
   public static class ApertureFactory
   {
      public static Aperture GetAperture(BlockReference blRefAperture)
      {
         string layerKey = blRefAperture.Layer.ToUpper();
         Aperture aperture = null;
         if (layerKey.Equals (Options.Instance.LayerApertureDoor.ToUpper()) )
         {
            aperture = new DoorAperture(blRefAperture);
         }
         else if (layerKey.Equals(Options.Instance.LayerApertureWindow.ToUpper()))
         {
            aperture = new WindowAperture(blRefAperture);
         }
         return aperture;
      }
   }
}
