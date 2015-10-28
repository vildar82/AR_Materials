using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model.Result
{
   // Вывод результатов расчета материалов в таблицу
   public class ToTable
   {
      private List<Room> _rooms;

      public ToTable(List<Room> rooms)
      {
         _rooms = rooms;
      }

      // Вставка таблицы 
      public void Insert()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Database db = doc.Database;
         Editor ed = doc.Editor;

         Table table;
         if (_rooms.GroupBy(r=>r.Ws.Section).Count()>1)
         {
            SectionTable st = new SectionTable(_rooms);
            table = st.GetTable();
         }
         else
         {
            RoomTable rt = new RoomTable(_rooms);
            table = rt.GetTable();
         }
         
         TableJig jigTable = new TableJig(table, 100);
         if (ed.Drag(jigTable).Status == PromptStatus.OK)
         {
            using (var t = db.TransactionManager.StartTransaction())
            {
               table.ScaleFactors = new Scale3d(100);
               var cs = db.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord;
               cs.AppendEntity(table);
               table.Dispose();
               t.Commit();
            }
         }
      }      

      public static ObjectId getTableStyle(Database db)
      {
         using(var dictTableStyles = db.TableStyleDictionaryId.Open(OpenMode.ForRead) as DBDictionary)
         {
            if (dictTableStyles.Contains("ПИК"))
            {
               return dictTableStyles.GetAt("ПИК");
            }
         }
         return db.Tablestyle;
      }
   }
}
