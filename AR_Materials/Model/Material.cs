using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_Materials.Model
{
   public class Material
   {
      private string _name;
      private double _value;
      private EnumConstructionType _construction;

      public string Name { get { return _name; } }
      public double Value { get { return _value; } set { _value = value; } }
      public EnumConstructionType Construction { get { return _construction; } }

      public Material(string name, EnumConstructionType constrType)
      {
         _name = name;
         _construction = constrType;
         _value = 0;
      }
   }
}
