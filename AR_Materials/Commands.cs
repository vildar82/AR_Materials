using System.Collections.Generic;
using AR_Materials.Model;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR_Materials.Commands))]

namespace AR_Materials
{
   public class Commands
   {
      [CommandMethod("PIK", "MaterialsCalc", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
      public void MaterialsCalc()
      {
         try
         {
            AutoCAD_PIK_Manager.Log.Info("Plugin AR_Materials Start Command MaterialsCalc");

            Inspector.Clear();
            Counter.Clear();
                        
            // Поиск всех блоков помещений и проемов в чертеже.
            RoomService roomService = new RoomService();
            roomService.FindAllBlocks();
            if (Inspector.HasError)
            {
               Inspector.Show();
               return;
            }

            // Поиск пересечений проемов и помещений.
            roomService.FindIntersections();
            if (Inspector.HasError)
            {
               Inspector.Show();
               return;
            }

            // Подсчет материалов
            roomService.CalcMaterials();
            if (Inspector.HasError)
            {
               Inspector.Show();
               return;
            }

            // Построение таблицы.
            ToTable toTable = new ToTable(roomService.Rooms);
            toTable.Insert();

            // Експорт в ексель.            
         }
         catch (System.Exception ex)
         {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            doc.Editor.WriteMessage("\n{0}", ex.ToString());
            AutoCAD_PIK_Manager.Log.Error(ex, "MaterialsCalc");
         }
      } 
   }
}