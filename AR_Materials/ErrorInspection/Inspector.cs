using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AR_Materials.ErrorInspection;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials
{
   public static class Inspector
   {
      private static List<Error> _errors;     

      public static List<Error> Errors { get { return _errors; } }
      public static bool HasError { get { return _errors.Count > 0; } }

      static Inspector()
      {
         _errors = new List<Error>();
      }

      // Очистка инспектора от старых ошибок.
      public static void Clear()
      {
         _errors = new List<Error>();
      }

      public static void AddError(string message)
      {
         AddError(message, null);
      }

      public static void AddError(string message, ObjectId idEnt)
      {
         using (var ent = idEnt.Open(OpenMode.ForRead) as Entity)
         {
            AddError(message, ent);
         }
      }      

      public static void AddError (string message, Entity ent)
      {
         _errors.Add(new Error(message, ent));
      }            

      public static void Show()
      {
         Application.ShowModelessDialog(new FormError());
      }
   }
}
