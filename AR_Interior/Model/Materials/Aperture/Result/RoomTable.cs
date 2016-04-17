using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model.Materials.Result
{
   public class RoomTable
   {
      private List<Room> _rooms;

      enum ColEnum
      {         
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

      public RoomTable(List<Room> rooms)
      {
         _rooms = rooms;
      }

      public Table GetTable()
      {         
         Database db = HostApplicationServices.WorkingDatabase;
         CellRange mCells;
         Table table = new Table();
         table.TableStyle = ToTable.getTableStyle(db);
         // Размер таблицы                  
         table.SetSize(3, Enum.GetValues(typeof(ColEnum)).Length);
         // Ширина столбцов         
         foreach (var item in table.Columns)
         {
            item.Width = 35;
            item.Alignment = CellAlignment.MiddleCenter;
         }
         table.Columns[0].Width = 10;
         table.Rows[1].Height = 15;

         // Заголовки столбцов и название таблицы         
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
         
         // Заполнение помещений                           
         FillRoomsMaterial(table);
         int row = table.Rows.Count - 1;

         // Заполнение итогов
         // одна строка отступа.         
         // Объединение строки между помещениями и итогом по матаериалам.
         mCells = CellRange.Create(table, row, 0, row, Enum.GetValues(typeof(ColEnum)).Length - 1);         
         table.MergeCells(mCells);         
         row++;
         table.InsertRowsAndInherit(row, row-1, 1);
         table.Cells[row, 0].TextString = "Итого материалов в этой квартире";
         table.Cells[row, 0].Alignment = CellAlignment.MiddleCenter;         
         row++;
         table.InsertRowsAndInherit(row, 2, 1);
         // заполнение итогов по материалам
         FillTotalMaterials(table,row);

         table.GenerateLayout();
         return table;
      }

      // заполнение помещений
      private int FillRoomsMaterial(Table table)
      {
         CellRange mCells;
         int row= table.Rows.Count-1;
         // сортировка помещений по номерам
         var rooms = _rooms.OrderBy(r => r.Owner).ThenBy(n => n.Number);
         foreach (var room in rooms)
         {            
            int topRow = row;
            // Секция            
            if (row == 2)
            {
               table.Cells[0, 0].TextString = "Расход материалов в квартире " + room.Owner;               
            }
            // Наименование помещения
            table.Cells[row, (int)ColEnum.RoomName].TextString = room.Name;
            table.Cells[row, (int)ColEnum.RoomNumber].TextString = room.Number.ToString();
            // Примечание                                                
            table.Cells[row, (int)ColEnum.Description].TextString = room.Description;
            // матариалы в квартире            
            foreach (var item in Enum.GetValues(typeof(EnumConstructionType)).Cast<EnumConstructionType>())
            {
               if (item == EnumConstructionType.Undefined) continue;
               FillRowMaterial(table, row, room, item);              
            }
            row = table.Rows.Count - 1;
            //Объединение столбцов номера и имени помещения
            if ((row - topRow) > 1)
            {
               mCells = CellRange.Create(table, topRow, (int)ColEnum.RoomNumber, row-1, (int)ColEnum.RoomNumber);
               table.MergeCells(mCells);
               mCells = CellRange.Create(table, topRow, (int)ColEnum.RoomName, row-1, (int)ColEnum.RoomName);
               table.MergeCells(mCells);
            }
         }
         // Итого по помещениям   
         mCells = CellRange.Create(table, row, (int)ColEnum.RoomNumber, row, (int)ColEnum.RoomName);
         table.MergeCells(mCells);
         table.Cells[row, (int)ColEnum.RoomNumber].TextString = "Итого";
         // всего материалов по всем помещениям.         
         var maters = _rooms.SelectMany(r => r.Materials).Where(m => !string.IsNullOrWhiteSpace(m.Name)).GroupBy(m => m.Construction);         
         table.InsertRowsAndInherit(row+1, 2, 1);
         foreach (var item in maters)
         {
            var value = item.Sum(m => m.PresentValue).ToString();
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
         return row;
      }

      // заполнение итогов по всем материалам одной конструкции
      private void FillTotalMaterials(Table table, int rowStart)
      {
         CellRange mCells;         
         // группировка всех материалов по типу коеструкции
         var maters = _rooms.SelectMany(r => r.Materials).GroupBy(m => m.Construction);
         foreach (var matersSomeConstr in maters)
         {            
            // группировка по имени материала
            var materSomeName = matersSomeConstr.GroupBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase);
            int countMaterSome = materSomeName.Count();
            int curRowsToMater = table.Rows.Count - rowStart;
            if (curRowsToMater < countMaterSome)
            {
               table.InsertRowsAndInherit(table.Rows.Count, 2, countMaterSome- curRowsToMater);
            }
            int row = rowStart;
            foreach (var mater in materSomeName)
            {
               if (matersSomeConstr.Key == EnumConstructionType.Undefined) continue;               
               mCells = CellRange.Create(table, row, (int)ColEnum.RoomNumber, row, (int)ColEnum.RoomName);
               table.MergeCells(mCells);               
               // mater - должен быть всегда один материал.
               var value = mater.Sum(m => m.PresentValue).ToString();
               ColEnum colName;
               ColEnum colValue;
               getColEnumFromConstructionEnum(matersSomeConstr.Key, out colName, out colValue);
               table.Cells[row, (int)colName].TextString = mater.Key;
               table.Cells[row, (int)colValue].TextString = value;
               row++;
            }
         }         
      }      

      private void getColEnumFromConstructionEnum(EnumConstructionType item, out ColEnum colName, out ColEnum colValue)
      {
         colName = 0;
         colValue = 0;
         switch (item)
         {
            case EnumConstructionType.Wall:
               colName = ColEnum.WallMaterial;
               colValue = ColEnum.WallArea;
               break;
            case EnumConstructionType.Deck:
               colName = ColEnum.DeckMaterial;
               colValue = ColEnum.DeckArea;
               break;
            case EnumConstructionType.Ceil:
               colName = ColEnum.CeilMaterial;
               colValue = ColEnum.CeilArea;
               break;
            case EnumConstructionType.Baseboard:
               colName = ColEnum.BaseboardMaterial;
               colValue = ColEnum.BaseboardLenght;
               break;
            case EnumConstructionType.Carnice:
               colName = ColEnum.CarniceMaterial;
               colValue = ColEnum.CarniceLenght;
               break;
            case EnumConstructionType.Undefined:
               break;
            default:
               break;
         }
      }

      private void FillRowMaterial(Table table, int row, Room room, EnumConstructionType construction)
      {
         var materials = room.Materials.Where(m => m.Construction == construction && !string.IsNullOrWhiteSpace(m.Name));
         int countMaters = materials.Count();
         int maxRow = (table.Rows.Count-1) - row; 
         if (countMaters > 0)
         {            
            if (countMaters > maxRow)
            {
               //table.InsertRowsAndInherit(row + maxRow+1,2, countMaters - maxRow);
               table.InsertRowsAndInherit(table.Rows.Count, 2, countMaters - maxRow);
               maxRow = countMaters;
            }
            ColEnum colName;
            ColEnum colValue;
            getColEnumFromConstructionEnum(construction, out colName, out colValue);
            foreach (var item in materials)
            {
               table.Cells[row, (int)colName].TextString = item.Name;
               table.Cells[row, (int)colValue].TextString = item.PresentValue.ToString();
               row++;
            }
         }
      }
   }
}
