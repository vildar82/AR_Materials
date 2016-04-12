using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;

namespace AR_Materials.Model.Result
{
   public class SectionTable
   {
      private List<Room> _rooms;
      private CellRange _mCells;

      enum ColEnum
      {         
         Apartament, // Тип квартиры 
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

      public SectionTable(List<Room> rooms)
      {
         _rooms = rooms;
      }

      public Table GetTable()
      {
         Database db = HostApplicationServices.WorkingDatabase;         
         Table table = new Table();
         table.TableStyle = ToTable.getTableStyle(db);
         // Размер таблицы 
         table.SetSize(3, Enum.GetValues(typeof(ColEnum)).Length);
         // Ширина столбцов         
         foreach (var item in table.Columns)
         {
            item.Width = 35;            
         }
         table.Rows[0].Height = 15;
         table.Columns[0].Width = 10; // столбец типа квартиры
         table.Columns[1].Width = 10; // столбец номера помещения

         // Заголовки столбцов и название таблицы
         table.Cells[0, 0].TextString = "Ведомость отделки помещений";         
         table.Cells[1, (int)ColEnum.Apartament].TextString = "Тип кв.";
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
         FillMaterial(table);

         foreach (var item in table.Columns)
         {
            item.Alignment = CellAlignment.MiddleCenter;
         }

         table.GenerateLayout();
         return table;
      }

      // заполнение помещений
      private void FillMaterial(Table table)
      {         
         int row = table.Rows.Count - 1;
         // Группировка по секциям
         var sections = _rooms.GroupBy(r => r.Ws.Section).OrderBy(s=>s.Key);
         foreach (var section in sections)
         {
            table.InsertRows(row+1, 8, 1);
            _mCells = CellRange.Create(table, row, 0, row, Enum.GetValues(typeof(ColEnum)).Length - 1);
            table.MergeCells(_mCells);
            table.Cells[row, 0].TextString = "Секция " + section.Key;            
            row++;            
            // Группировка по квартирам
            var apartments = section.GroupBy(r => r.Owner).OrderBy(a=>a.Key);
            foreach (var apartment in apartments)
            {
               int rowApartment = row;
               table.Cells[rowApartment, (int)ColEnum.Apartament].TextString = apartment.Key;
               // сортировка помещений в квартире по номерам
               var rooms = apartment.OrderBy(n => n.Number);
               foreach (var room in rooms)
               {
                  FillRoom(table,row, room);
                  row = table.Rows.Count - 1;
               }
               if (row-1 > rowApartment)
               {
                  _mCells = CellRange.Create(table, rowApartment, (int)ColEnum.Apartament, row-1, (int)ColEnum.Apartament);
                  table.MergeCells(_mCells);
               }
            }            
         }         
      }

      private void FillRoom(Table table, int row, Room room)
      {
         // Наименование помещения
         table.Cells[row, (int)ColEnum.RoomName].TextString = room.Name;
         table.Cells[row, (int)ColEnum.RoomNumber].TextString = room.Number.ToString();
         // Примечание                                    
         table.Cells[row, (int)ColEnum.Description].TextString = room.Description;
         // матариалы в помещении      
         foreach (var item in Enum.GetValues(typeof(EnumConstructionType)).Cast<EnumConstructionType>())
         {
            if (item == EnumConstructionType.Undefined) continue;            
            FillRowMaterial(table, row, room, item);
         }
         if ((table.Rows.Count - 2) > row)
         {
            _mCells = CellRange.Create(table, row, (int)ColEnum.RoomName, table.Rows.Count - 2, (int)ColEnum.RoomName);
            table.MergeCells(_mCells);
            _mCells = CellRange.Create(table, row, (int)ColEnum.RoomNumber, table.Rows.Count - 2, (int)ColEnum.RoomNumber);
            table.MergeCells(_mCells);
         }
      }

      private void FillRowMaterial(Table table, int row, Room room, EnumConstructionType construction)
      {
         var materials = room.Materials.Where(m => m.Construction == construction && !string.IsNullOrWhiteSpace(m.Name));
         int countMaters = materials.Count();
         if (countMaters > 0)
         {
            int maxRow = (table.Rows.Count - 1) - row;
            if (countMaters > maxRow)
            {
               table.InsertRows(table.Rows.Count, 8, countMaters - maxRow);
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
   }
}
