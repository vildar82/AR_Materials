using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.ErrorInspection
{
   public class Error
   {
      private string _msg;
      private ObjectId _idEnt;
      private Extents3d _extents;

      public string Message { get { return _msg; } }
      public ObjectId IdEnt { get { return _idEnt; } }
      public Extents3d Extents { get { return _extents; } }

      public Error(string message, Entity ent)
      {
         _msg = message;
         _idEnt = ent.Id;
         _extents = ent.GeometricExtents;
      }
   }
}
