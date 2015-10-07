using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model
{
   public class Workspace : IEquatable<Workspace>
   {
      private Extents3d _extents;      
      private string _section;
      private string _project;

      public Extents3d Extents { get { return _extents; } }
      public string Section { get { return _section; } }
      public string Project { get { return _project; } }

      public Workspace(BlockReference blRef)
      {         
         _extents = blRef.GeometricExtents;
         getAttrs(blRef);
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
                  _section = atr.TextString;
               }
               else if (tag.Equals(Options.Instance.WorkspaceBlAttrTagProject.ToUpper()))
               {
                  _project = atr.TextString;
               }
            }
         }
      }

      public bool Equals(Workspace other)
      {
         return _section.ToUpper() == other._section.ToUpper();  
      }
   }
}
