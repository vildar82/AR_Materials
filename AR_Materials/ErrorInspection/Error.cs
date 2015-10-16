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
      private string _shortMsg;
      private string _msg;
      private ObjectId _idEnt;
      private Extents3d _extents;
      private bool _hasEntity;

      public string ShortMsg { get { return _shortMsg; } }
      public string Message { get { return _msg; } }
      public ObjectId IdEnt { get { return _idEnt; } }
      public Extents3d Extents { get { return _extents; } }
      public bool HasEntity { get { return _hasEntity; } }

      public Error(string message, Entity ent)
      {
         _msg = message;
         if (_msg.Length >100)         
            _shortMsg = _msg.Substring(0, 100);         
         else         
            _shortMsg = _msg;         
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
