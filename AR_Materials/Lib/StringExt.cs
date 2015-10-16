using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace System
{
   public static class StringExt
   {
      /// <summary>
      /// Удаление разделителей строк и др. \r\n?|\n
      /// </summary>
      /// <param name="input"></param>
      /// <returns></returns>
      public static string ClearString(this string input)
      {
         //return Regex.Replace(input, @"\r\n?|\n", "");
         return input.Trim().Replace("\r\n", "").Replace("\n", "").Replace("\r", "");
      }
   }
}
