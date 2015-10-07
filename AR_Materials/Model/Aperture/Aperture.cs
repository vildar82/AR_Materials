using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model
{
   // Проем
   public abstract class Aperture
   {
      private double _lenght; // Длина
      private int _height; // Высота
      private double _area;
      private ObjectId _idBlRef;
      //private Extents3d _extents;

      public double Area { get { return _area; } }
      public double Lenght { get { return _lenght; } }
      public int Height { get { return _height; } }
      public ObjectId IdBlRef { get { return _idBlRef; } }
      //public Extents3d Extents { get { return _extents; } }

      private Aperture() { }

      public Aperture(BlockReference blRef)
      {
         _idBlRef = blRef.Id;
         //_extents = blRef.GeometricExtents;  
         // Определение длины из динамического параметра Расстояние
         _lenght = getLenght(blRef);
         // Определение высоты
         _height = getHeight(blRef);
         _area = _lenght * _height;
      }

      private int getHeight(BlockReference blRef)
      {
         int height = 0;
         foreach (ObjectId idAtrRef in blRef.AttributeCollection)
         {
            using (var atrRef = idAtrRef.GetObject( OpenMode.ForRead)as AttributeReference ) 
            {
               string tag = atrRef.Tag.ToUpper();
               if (tag.Equals (Options.Instance.ApertureBlAttrTagHeight.ToUpper()))
               {
                  int.TryParse(atrRef.TextString, out height);
                  break;
               } 
            }
         }
         return height;
      }

      private double getLenght(BlockReference blRef)
      {
         double lenght = 0;
         foreach (DynamicBlockReferenceProperty dynProp in blRef.DynamicBlockReferencePropertyCollection)
         {
            if (dynProp.PropertyName.ToUpper().Equals(Options.Instance.ApertureBlDynPropLenght.ToUpper()))
            {
               lenght = (double)dynProp.Value;
               break;
            }            
         }
         return lenght; 
      }
   }
}
