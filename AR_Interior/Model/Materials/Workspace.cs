﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model.Materials
{
   public class Workspace : IEquatable<Workspace>
   {
      private Extents3d _extents;      
      private string _section;
      private string _storey;

      public Extents3d Extents { get { return _extents; } }
      public string Section { get { return _section; } }
      public string Storey { get { return _storey; } }

      public Workspace(BlockReference blRef)
      {
         _section = ""; // не обязательно должна быть секция
         _storey = ""; // ? не уверен. может быть помещение без этажа?
         if (blRef != null)
         {         
            _extents = blRef.GeometricExtents;            
            getAttrs(blRef);
         }         
      }

      private void getAttrs(BlockReference blRef)
      {
         foreach (ObjectId idAtrRef in blRef.AttributeCollection)
         {
            using (var atr = idAtrRef.GetObject(OpenMode.ForRead) as AttributeReference)
            {
               string tag = atr.Tag.ToUpper();
               // Секция
               if (tag.Equals(Options.Instance.WorkspaceBlAttrTagSection.ToUpper()))
               {
                  _section = atr.TextString.ClearString();
               }
               // Этаж
               else if (tag.Equals(Options.Instance.WorkspaceBlAttrTagStorey.ToUpper()))
               {
                  _storey = atr.TextString.ClearString(); 
               }
            }
         }
      }

      public bool Equals(Workspace other)
      {
         return _section.ToUpper() == other._section.ToUpper();  
      }

      private void checkParams(BlockReference blRef)
      {
         bool haserr = false;
         string msg = string.Format("Ошибка в блоке {0}: ", Options.Instance.BlockWorkspaceName);
         // Длина
         if (string.IsNullOrEmpty (_storey) )
         {
            msg += "Этаж не определен. ";
            haserr = true;
         }
         if (haserr)
         {
            Inspector.AddError(msg, blRef, System.Drawing.SystemIcons.Error);
         }
      }
   }
}
