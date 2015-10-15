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
      private bool _hasEntity;

      public string Message { get { return _msg; } }
      public ObjectId IdEnt { get { return _idEnt; } }
      public Extents3d Extents { get { return _extents; } }
      public bool HasEntity { get { return _hasEntity; } }

      public Error(string message, Entity ent)
      {
         _msg = message;
         if (ent == null)
         {
            _hasEntity = false;
         }
         else
         {
            _hasEntity = true;
            _idEnt = ent.Id;
            _extents = ent.GeometricExtents;
         }         
      }
   }
}
