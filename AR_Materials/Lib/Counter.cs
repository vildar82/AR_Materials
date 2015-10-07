using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_Materials
{
   public static class Counter
   {
      private static Dictionary<string, int> _counter;

      static Counter()
      {
         _counter = new Dictionary<string, int>();
      }

      public static void Clear ()
      {
         _counter = new Dictionary<string, int>();
      }

      public static void AddCount (string key)
      {
         if (_counter.ContainsKey(key) )
         {
            _counter[key]++;
         }
      }

      public static int GetCount (string key)
      {
         int count;
         _counter.TryGetValue(key, out count);
         return count;
      }
   }
}
