using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model
{
   // Ящик в туалете для бачка унитаза (короб, с отделкой плиткой)
   public class Toilet
   {
      private Point3d _position;
      private double _lenght;      
      private double _width;
      private int _height;
      private int _contact; // Примыкание стенок ящика унитаза к стенам помещения (1 - по длинной стороне, 2 - по длинной и короткой стороне).

      public Point3d Position { get { return _position; } }

      public Toilet(BlockReference blRef)
      {
         _position = blRef.Position;
         getAttrs(blRef);
         getDynParams(blRef);
         checkParams(blRef);
      }

      /// <summary>
      /// Прибавка к площади стен (к матенриалу стены помещения).
      /// </summary>
      /// <returns></returns>
      public double AddToWallArea ()
      {
         double res = 0;
         // Площадь крышки короба унитаза
         res += _lenght * _width;
         // боковая стенка
         var lowerLen = _lenght >= _width ? _width : _lenght;// размер меньшей из сторон короба
         if (_contact == 1)         
            res += lowerLen * _height;         
         else if (_contact == 2)         
            res += lowerLen * _height * 2;         
         return res;
      }

      // Прибавка к площади пола (отрицательная величина)
      public double AddToDeckArea()
      {
         return -(_lenght * _width);
      }

      // Добавка к плинусам
      public double AddToBaseboardLen()
      {
         double res = 0;
         var lowerLen = _lenght >= _width ? _width : _lenght;// размер меньшей из сторон короба
         if (_contact == 1)         
            res += lowerLen;         
         else if (_contact == 2)         
            res += lowerLen * 2;         
         return res;
      }

      private void checkParams(BlockReference blRef)
      {
         bool haserr = false;
         string msg = string.Format("Ошибка в блоке {0}: ", Options.Instance.BlockToiletName);
         // Длина
         if (_lenght <=0)
         {
            msg += "Длина не определена. ";
            haserr = true;
         }
         // Ширина
         if (_width <= 0)
         {
            msg += "Ширина не определена. ";
            haserr = true;
         }
         // Высота
         if (_height <= 0)
         {
            msg += "Высота не определена. ";
            haserr = true;
         }
         // Примыкание
         if (_contact != 1 && _contact != 2)
         {
            msg += "Примыкание не определено (1 - длинной соторой, 2 - длинной и короткой). ";
            haserr = true;
         }

         if (haserr)
         {
            Inspector.AddError(msg, blRef);
         }
      }

      private void getDynParams(BlockReference blRef)
      {
         foreach (DynamicBlockReferenceProperty item in blRef.DynamicBlockReferencePropertyCollection)
         {
            string paramname = item.PropertyName.ToUpper();
            switch (paramname)
            {
               case "ДЛИНА":
                  double.TryParse(item.Value.ToString(), out _lenght);
                  break;
               case "ШИРИНА":
                  double.TryParse(item.Value.ToString(), out _width);
                  break;
            }
         }
      }

      private void getAttrs(BlockReference blRef)
      {
         foreach (ObjectId idAtr in blRef.AttributeCollection)
         {
            using (var atr = idAtr.GetObject(OpenMode.ForRead) as AttributeReference)
            {
               string tag = atr.Tag.ToUpper();
               // Высота
               if (tag.Equals(Options.Instance.ToiletBlAttrTagHeight.ToUpper()))
               {
                  int.TryParse( atr.TextString, out _height);
               }
               // Материал
               if (tag.Equals(Options.Instance.ToiletBlAttrTagContact.ToUpper()))
               {
                  int.TryParse (atr.TextString, out _contact);
               }               
            }
         }
      }      
   }
}
