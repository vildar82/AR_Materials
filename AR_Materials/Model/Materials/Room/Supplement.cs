using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model
{
   // Добавка к площади или длине
   public class Supplement
   {
      private Point3d _position;
      private EnumConstructionType _owner; // Стены, Пол, Потолок, Плинтус, Карниз
      private string _material;
      private EnumOperation _operation;
      private double _lenght;
      private double _width;
      private ObjectId _idBlRef;

      public ObjectId IdBlRef { get { return _idBlRef; } }
      public Point3d Position { get { return _position; } }
      public EnumConstructionType Owner { get { return _owner; } }
      public string Material { get { return _material; } }
      public EnumOperation Operation { get { return _operation; } }
      public double Lenght { get { return _lenght; } }
      public double Width { get { return _width; } }

      public Supplement (BlockReference blRef)
      {
         _idBlRef = blRef.Id; 
         _material = ""; // Поумолчанию = "" - материал берется с принадлежности добавки (стена, пол, потолок, плинтус, карниз).
         _position = blRef.Position;
         getAttrs(blRef);
         getDynParams(blRef);
         checkParams(blRef);
      }      

      /// <summary>
      /// Определение параметров из дин параметров блока
      /// </summary>      
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

      /// <summary>
      /// Получение параметров из атрибутов блока
      /// </summary>      
      private void getAttrs(BlockReference blRef)
      {
         foreach (ObjectId idAtr in blRef.AttributeCollection )
         {
            using (var atr = idAtr.GetObject(OpenMode.ForRead) as AttributeReference)
            {
               string tag = atr.Tag.ToUpper();
               // Принадлежность
               if (tag.Equals(Options.Instance.SupplementBlAttrTagOwner.ToUpper()))
               {
                  _owner = RoomService.GetConstructionType (atr.TextString);
               }
               // Материал
               if (tag.Equals(Options.Instance.SupplementBlAttrTagMaterial.ToUpper()))
               {
                  _material = atr.TextString.ClearString();
               }
               // Действие
               if (tag.Equals(Options.Instance.SupplementBlAttrTagOperation.ToUpper()))
               {
                  _operation = getOperationType(atr.TextString);
               }
            }
         }
      }

      private EnumOperation getOperationType(string input)
      {
         if (string.IsNullOrEmpty(input))         
            return EnumOperation.Undefined;
         if (input.StartsWith("+-") || input.StartsWith("-+"))         
            return EnumOperation.Both;         
         if (input.StartsWith("+"))         
            return EnumOperation.Addition;         
         if (input.StartsWith("-"))         
            return EnumOperation.Subtraction;
         return EnumOperation.Undefined;          
      }

      private void checkParams(BlockReference blRef)
      {
         bool hasErr = false;
         string errMsg = string.Format( "Ошибки в блоке {0}: ", Options.Instance.BlockSupplementName);

         // Длина и ширина
         if (_owner == EnumConstructionType.Baseboard || _owner == EnumConstructionType.Carnice)
         {
            // Присваиваем длину - макисальное значение из двух значений, она должна быть больше 0
            _lenght = _lenght > _width ? _lenght : _width;
            if (_lenght <=0)
            {
               errMsg += "Длина не определена. ";
               hasErr = true;
            }
         }
         else if (_owner == EnumConstructionType.Ceil || _owner == EnumConstructionType.Deck || 
            _owner == EnumConstructionType.Wall)
         {
            // Обе величины не должны быть равны 0
            if (_lenght <= 0)
            {
               errMsg += "Длина не определена. ";
               hasErr = true;
            }
            if (_width <= 0)
            {
               errMsg += "Ширина не определена. ";
               hasErr = true;
            }
         }

         // Действие
         if (_operation == EnumOperation.Undefined)
         {
            errMsg += "Действие не определено (может быть + - +-). ";
            hasErr = true;
         }

         // Принадлежность (Стена, Потлок, Пол, Карниз, Плинтус)
         if (_owner == EnumConstructionType.Undefined)
         {
            errMsg += "Не определена принадлежность добавки (Пол, Потолок, Карниз, Стена или Плинтус). ";
            hasErr = true;
         }

         // Материал
         if (string.IsNullOrEmpty(_material))
         {
            errMsg += "Материал не определен. ";
            hasErr = true;
         }         

         if (hasErr)
         {
            Inspector.AddError(errMsg, blRef, System.Drawing.SystemIcons.Error);
         }
      }
   }
}
