using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model
{
   // Вывод результатов расчета материалов в таблицу
   public class ToTable
   {
      enum ColEnum
      {         
         Section = 0,//"Квартира";
         ApartamentType = 1, // Тип квартиры 
         RoomNumber = 2, // Номер помещения
         RoomName =3, // Наименование помещения
         CeilMaterial =4, // Потолок
         CeilArea =5, // Площадь, м2
         CarniceMaterial =6, // "Потолочный карниз";
         CarniceLenght =7, // Длина, п.м.
         WallMaterial =8,  // Стены
         WallArea =9, // Площадь стен, м2
         DeckMaterial = 10, // Пол
         DeckArea = 11, //Площадь, м2
         BaseboardMaterial =12, //Плинтус
         BaseboardLenght =13, //Длина, п.м.
         Description =14 //Примечание         
      }

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

         Table table = getTable();         
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

      private Table getTable()
      {
         Database db = HostApplicationServices.WorkingDatabase;
         Table table = new Table();
         table.TableStyle = getTableStyle(db);
         // Размер таблицы         
         table.SetSize(getRowCount(), Enum.GetValues(typeof(ColEnum)).Length);
         // Ширина столбцов         
         foreach (var item in table.Columns)
         {
            item.Width = 35;
            item.Alignment = CellAlignment.MiddleCenter; 
         }         
         // Заголовки столбцов и название таблицы
         table.Cells[0, 0].TextString = "Расход материалов";
         table.Cells[1, (int)ColEnum.Section].TextString = "Секция";
         table.Cells[1, (int)ColEnum.ApartamentType].TextString = "Тип кв.";
         table.Cells[1, (int)ColEnum.RoomNumber].TextString = "№пом.";
         table.Cells[1, (int)ColEnum.RoomName].TextString = "Наименование";
         table.Cells[1, (int)ColEnum.CeilMaterial].TextString = "Потолок";
         table.Cells[1, (int)ColEnum.CeilArea].TextString = "Площадь, м2";
         table.Cells[1, (int)ColEnum.CarniceMaterial].TextString = "Потолочный карниз";
         table.Cells[1, (int)ColEnum.CarniceLenght].TextString = "Длина, п.м.";
         table.Cells[1, (int)ColEnum.WallMaterial].TextString = "Стены";
         table.Cells[1, (int)ColEnum.WallArea].TextString = "Площадь стен, м2";
         table.Cells[1, (int)ColEnum.DeckMaterial].TextString = "Пол";
         table.Cells[1, (int)ColEnum.DeckArea].TextString = "Площадь, м2";
         table.Cells[1, (int)ColEnum.BaseboardMaterial].TextString = "Плинтус";
         table.Cells[1, (int)ColEnum.BaseboardLenght].TextString = "Длина, п.м.";
         table.Cells[1, (int)ColEnum.Description].TextString = "Примечание";

         int row = 2;
         int i = 0;
         // Группировка по секциям
         var sectionsGroup = _rooms.GroupBy(r => r.Ws.Section);         
         foreach (var section in sectionsGroup)
         {            
            // Группировка по квартирам
            var apartmentsGroup = section.GroupBy(r => r.Owner);
            foreach (var apartment in apartmentsGroup)
            {              
               foreach (var room in apartment)
               {
                  // Секция
                  table.Cells[row, (int)ColEnum.Section].TextString = section.Key;
                  // Тип квартиры
                  table.Cells[row, (int)ColEnum.ApartamentType].TextString = apartment.Key;
                  // Наименование помещения
                  table.Cells[row, (int)ColEnum.RoomName].TextString = room.Name;
                  // матариалы потолков в квартире
                  var ceilMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Ceil);
                  i = 0;
                  foreach (var item in ceilMaterials)
                  {
                     table.Cells[row + i, (int)ColEnum.CeilMaterial].TextString = item.Name;
                     table.Cells[row + i, (int)ColEnum.CeilArea].TextString = item.PresentValue;
                     i++;
                  }
                  // Матенриалы Потолочного карниза (типы)                  
                  var carniceMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Carnice);
                  i = 0;
                  foreach (var item in carniceMaterials)
                  {
                     table.Cells[row + i, (int)ColEnum.CarniceMaterial].TextString = item.Name;
                     table.Cells[row + i, (int)ColEnum.CarniceLenght].TextString = item.PresentValue;
                     i++;
                  }
                  // Стены                  
                  var wallMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Wall);
                  i = 0;
                  foreach (var item in wallMaterials)
                  {
                     table.Cells[row + i, (int)ColEnum.WallMaterial].TextString = item.Name;
                     table.Cells[row + i, (int)ColEnum.WallArea].TextString = item.PresentValue;
                     i++;
                  }
                  // Полы                  
                  var deckMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Deck);
                  i = 0;
                  foreach (var item in deckMaterials)
                  {
                     table.Cells[row + i, (int)ColEnum.DeckMaterial].TextString = item.Name;
                     table.Cells[row + i, (int)ColEnum.DeckArea].TextString = item.PresentValue;
                     i++;
                  }
                  // Плинтус                  
                  var baseboardMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Baseboard);
                  i = 0;
                  foreach (var item in baseboardMaterials)
                  {
                     table.Cells[row + i, (int)ColEnum.BaseboardMaterial].TextString = item.Name;
                     table.Cells[row + i, (int)ColEnum.BaseboardLenght].TextString = item.PresentValue;
                     i++;
                  }
                  // Примечание                                    
                  table.Cells[row, (int)ColEnum.Description].TextString = room.Description; 
                  // определение текущей строки
                  int[] groupCounts = { ceilMaterials.Count(), wallMaterials.Count(), carniceMaterials.Count(),
                                 deckMaterials.Count(), baseboardMaterials.Count() };
                  row += groupCounts.Max();
               }
            }
         }
         table.GenerateLayout(); 
         return table;
      }

      // Определение количества строк для таблицы
      private int getRowCount()
      {
         int resRowCount = 2;//название и заголовки.
         foreach (var room in _rooms)
         {
            // максимальное количество материалов одной конструкции
            resRowCount += room.Materials.GroupBy(m=>m.Construction).Max(k => k.Count());
         }
         return resRowCount;
      }

      private ObjectId getTableStyle(Database db)
      {
         using (var t = db.TransactionManager.StartTransaction())
         {
            var dictTableStyles = t.GetObject(db.TableStyleDictionaryId, OpenMode.ForRead) as DBDictionary;
            if (dictTableStyles.Contains("ПИК"))
            {
               return dictTableStyles.GetAt("ПИК");
            }
         }
         return db.Tablestyle;
      }
   }
}
