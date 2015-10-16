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
         //Section = 0,//"Квартира";
         //ApartamentType = 1, // Тип квартиры 
         RoomNumber, // Номер помещения
         RoomName, // Наименование помещения
         CeilMaterial, // Потолок
         CeilArea, // Площадь, м2
         CarniceMaterial, // "Потолочный карниз";
         CarniceLenght, // Длина, п.м.
         WallMaterial,  // Стены
         WallArea, // Площадь стен, м2
         DeckMaterial, // Пол
         DeckArea, //Площадь, м2
         BaseboardMaterial, //Плинтус
         BaseboardLenght, //Длина, п.м.
         Description //Примечание         
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
         CellRange mCells;
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
         table.Columns[0].Width = 10;

         // Заголовки столбцов и название таблицы
         //table.Cells[0, 0].TextString = "Расход материалов";
         //table.Cells[1, (int)ColEnum.Section].TextString = "Секция";
         //table.Cells[1, (int)ColEnum.ApartamentType].TextString = "Тип кв.";
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
         // Заполнение помещений                           
         row = FillRoomsMaterial(table, row);

         // Заполнение итогов
         row++; // одна строка отступа.
         // Объединение строки между помещениями и итогом по матаериалам.
         mCells = CellRange.Create(table, row, 0, row, Enum.GetValues(typeof (ColEnum)).Length-1);
         table.MergeCells(mCells);
         table.Rows[row].Height = 15;         
         table.Cells[row, 0].TextString = "Итого материалов в этой квартире";
         table.Cells[row, 0].Alignment = CellAlignment.MiddleCenter;
         // заполнение итогов по материалам
         row = FillTotalMaterials(table, row);

         table.GenerateLayout();
         return table;
      }      

      // заполнение помещений
      private int FillRoomsMaterial(Table table, int row)
      {
         CellRange mCells;
         int i = 0;
         // Группировка по секциям
         //var sectionsGroup = _rooms.GroupBy(r => r.Ws.Section);
         //foreach (var section in sectionsGroup)
         //{
            // Группировка по квартирам
            //var apartmentsGroup = _rooms.GroupBy(r => r.Owner);
            //foreach (var apartment in apartmentsGroup)
            //{
               // сортировка помещений по номерам
         var rooms = _rooms.OrderBy(r => r.Owner).ThenBy(n => n.Number);
         foreach (var room in rooms)
         {
            // Секция
            //table.Cells[row, (int)ColEnum.Section].TextString = section.Key;
            // Тип квартиры
            //table.Cells[row, (int)ColEnum.ApartamentType].TextString = apartment.Key;
            if (row == 2)
            {
               table.Cells[0, 0].TextString = "Расход материалов в квартире " + room.Owner;
               table.Rows[0].Height = 15;
            }
            // Наименование помещения
            table.Cells[row, (int)ColEnum.RoomName].TextString = room.Name;
            table.Cells[row, (int)ColEnum.RoomNumber].TextString = room.Number.ToString();
            // матариалы потолков в квартире
            var ceilMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Ceil);
            i = 0;
            foreach (var item in ceilMaterials)
            {
               if (!string.IsNullOrWhiteSpace(item.Name))
               {
                  table.Cells[row + i, (int)ColEnum.CeilMaterial].TextString = item.Name;
                  table.Cells[row + i, (int)ColEnum.CeilArea].TextString = item.PresentValue.ToString();
                  i++;
               }               
            }
            // Матенриалы Потолочного карниза (типы)                  
            var carniceMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Carnice);
            i = 0;
            foreach (var item in carniceMaterials)
            {
               if (!string.IsNullOrWhiteSpace(item.Name))
               {
                  table.Cells[row + i, (int)ColEnum.CarniceMaterial].TextString = item.Name;
                  table.Cells[row + i, (int)ColEnum.CarniceLenght].TextString = item.PresentValue.ToString();
                  i++;
               }
            }
            // Стены                  
            var wallMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Wall);
            i = 0;
            foreach (var item in wallMaterials)
            {
               if (!string.IsNullOrWhiteSpace(item.Name))
               {
                  table.Cells[row + i, (int)ColEnum.WallMaterial].TextString = item.Name;
                  table.Cells[row + i, (int)ColEnum.WallArea].TextString = item.PresentValue.ToString();
                  i++;
               }
            }
            // Полы                  
            var deckMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Deck);
            i = 0;
            foreach (var item in deckMaterials)
            {
               if (!string.IsNullOrWhiteSpace(item.Name))
               {
                  table.Cells[row + i, (int)ColEnum.DeckMaterial].TextString = item.Name;
                  table.Cells[row + i, (int)ColEnum.DeckArea].TextString = item.PresentValue.ToString();
                  i++;
               }
            }
            // Плинтус                  
            var baseboardMaterials = room.Materials.Where(m => m.Construction == EnumConstructionType.Baseboard);
            i = 0;
            foreach (var item in baseboardMaterials)
            {
               if (!string.IsNullOrWhiteSpace(item.Name))
               {
                  table.Cells[row + i, (int)ColEnum.BaseboardMaterial].TextString = item.Name;
                  table.Cells[row + i, (int)ColEnum.BaseboardLenght].TextString = item.PresentValue.ToString();
                  i++;
               }
            }
            // Примечание                                    
            table.Cells[row, (int)ColEnum.Description].TextString = room.Description;
            // определение текущей строки
            int[] groupCounts = { ceilMaterials.Count(), wallMaterials.Count(), carniceMaterials.Count(),
                                 deckMaterials.Count(), baseboardMaterials.Count() };
            int topRow = row;
            row += groupCounts.Max();
            // Объединение столбцов номера и имени помещения
            if ((row - topRow) > 1)
            {
               mCells = CellRange.Create(table, topRow, (int)ColEnum.RoomNumber, row - 1, (int)ColEnum.RoomNumber);
               table.MergeCells(mCells);
               mCells = CellRange.Create(table, topRow, (int)ColEnum.RoomName, row-1, (int)ColEnum.RoomName);
               table.MergeCells(mCells);
            }
         }
         //   }
         //}
         // Итого по помещениям         
         mCells = CellRange.Create(table, row, (int)ColEnum.RoomNumber, row, (int)ColEnum.RoomName);
         table.MergeCells(mCells);
         table.Cells[row, (int)ColEnum.RoomNumber].TextString = "Итого";
         // всего площадь стен по всем помещениям.         
         var wallMater = _rooms.SelectMany(r => r.Materials).GroupBy(m => m.Construction);
         foreach (var item in wallMater)
         {            
            var value = item.Sum(m => m.PresentValue).ToString();
            if (!string.IsNullOrWhiteSpace(item.First().Name))
            {
               switch (item.Key)
               {
                  case EnumConstructionType.Wall:
                     table.Cells[row, (int)ColEnum.WallMaterial].TextString = "Стены, м2";
                     table.Cells[row, (int)ColEnum.WallArea].TextString = value;
                     break;
                  case EnumConstructionType.Deck:
                     table.Cells[row, (int)ColEnum.DeckMaterial].TextString = "Пол, м2";
                     table.Cells[row, (int)ColEnum.DeckArea].TextString = value;
                     break;
                  case EnumConstructionType.Ceil:
                     table.Cells[row, (int)ColEnum.CeilMaterial].TextString = "Потолок, м2";
                     table.Cells[row, (int)ColEnum.CeilArea].TextString = value;
                     break;
                  case EnumConstructionType.Baseboard:
                     table.Cells[row, (int)ColEnum.BaseboardMaterial].TextString = "Плинтус, м.п.";
                     table.Cells[row, (int)ColEnum.BaseboardLenght].TextString = value;
                     break;
                  case EnumConstructionType.Carnice:
                     table.Cells[row, (int)ColEnum.CarniceMaterial].TextString = "Карниз, м.п.";
                     table.Cells[row, (int)ColEnum.CarniceLenght].TextString = value;
                     break;
                  case EnumConstructionType.Undefined:
                     break;
                  default:
                     break;
               }
            }
         }
         return row;
      }      

      // заполнение итогов по всем материалам одной конструкции
      private int FillTotalMaterials(Table table, int row)
      {
         CellRange mCells;
         // группировка всех материалов по типу коеструкции
         var maters = _rooms.SelectMany(r => r.Materials).GroupBy(m => m.Construction);
         foreach (var matersSomeConstr in maters)
         {
            int i = row;
            // группировка по имени материала
            var materSomeName = matersSomeConstr.GroupBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
            foreach (var mater in materSomeName)
            {
               i++;
               mCells = CellRange.Create(table, i, (int)ColEnum.RoomNumber, i, (int)ColEnum.RoomName);
               table.MergeCells(mCells);
               table.Cells[i, (int)ColEnum.RoomNumber].TextString = "Итого";
               // mater - должен быть всегда один материал.
               var value = mater.Sum(m => m.PresentValue).ToString();
               switch (matersSomeConstr.Key)
               {
                  case EnumConstructionType.Wall:
                     table.Cells[i, (int)ColEnum.WallMaterial].TextString = mater.Key;
                     table.Cells[i, (int)ColEnum.WallArea].TextString = value;
                     break;
                  case EnumConstructionType.Deck:
                     table.Cells[i, (int)ColEnum.DeckMaterial].TextString = mater.Key;
                     table.Cells[i, (int)ColEnum.DeckArea].TextString = value;
                     break;
                  case EnumConstructionType.Ceil:
                     table.Cells[i, (int)ColEnum.CeilMaterial].TextString = mater.Key;
                     table.Cells[i, (int)ColEnum.CeilArea].TextString = value;
                     break;
                  case EnumConstructionType.Baseboard:
                     table.Cells[i, (int)ColEnum.BaseboardMaterial].TextString = mater.Key;
                     table.Cells[i, (int)ColEnum.BaseboardLenght].TextString = value;
                     break;
                  case EnumConstructionType.Carnice:
                     table.Cells[i, (int)ColEnum.CarniceMaterial].TextString = mater.Key;
                     table.Cells[i, (int)ColEnum.CarniceLenght].TextString = value;
                     break;
                  case EnumConstructionType.Undefined:
                     break;
                  default:
                     break;
               }
            }            
         }
         return row;
      }

      // Определение количества строк для таблицы
      private int getRowCount()
      {
         int resRowCount = 2;//название и заголовки.
         // строки материалов помещений
         foreach (var room in _rooms)
         {
            // максимальное количество материалов одной конструкции
            resRowCount += room.Materials.GroupBy(m=>m.Construction).Max(k => k.Count());
         }
         // строка итогов 
         resRowCount++;
         // пустая строка
         resRowCount++;
         // итого по каждому материалу (кол материалов одной конмстр)
         var maxMatersSomeConstrAndName = _rooms.SelectMany(r => r.Materials).GroupBy(m => new { m.Construction, m.Name }).Max(m => m.Count());         
         resRowCount += maxMatersSomeConstrAndName;
         return ++resRowCount;
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
