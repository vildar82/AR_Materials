using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_Materials.Model.Materials
{
   public class Material
   {
      private string _name;
      private double _value;
      private double _presentvalue;
      private EnumConstructionType _construction;

      public string Name { get { return _name; } }
      /// <summary>
      /// расчетное значение величины. в мм.
      /// </summary>
      public double Value { get { return _value; }
         set
         {
            _value = value;
            _presentvalue = getPresentValue();
         }
      }
      public double PresentValue { get { return _presentvalue; } }

      public EnumConstructionType Construction { get { return _construction; } }

      public Material(string name, EnumConstructionType constrType)
      {
         _name = name;
         _construction = constrType;         
      }

      private double getPresentValue()
      {
         // _value - в мм.
         // вернуть в м, и округленно до 2 знаков.         
         double factor = 0;

         switch (_construction)
         {
            case EnumConstructionType.Wall:               
            case EnumConstructionType.Deck:               
            case EnumConstructionType.Ceil:
               factor = 0.000001; // перевод в м2
               break;
            case EnumConstructionType.Baseboard:               
            case EnumConstructionType.Carnice:
               factor = 0.001; // перевод в м
               break;
            case EnumConstructionType.Undefined:
               break;
            default:
               break;
         }
         return Math.Round(_value * factor, 2);
      }
   }
}
