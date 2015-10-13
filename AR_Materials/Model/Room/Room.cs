using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model
{
   // Помещение
   public class Room :  IEquatable<Room>
   {
      private ObjectId _idBlRefRoom;
      private Point3d _position;
      private List<Material> _materials;      
      private string _name; // Название помещения
      private string _owner; // Принадлежность (Имя квартиры или МОП)
      private string _description; // Примечание
      private string _floor; // Этаж
      private int _height;
      private double _perimeter; // Длина полилинии
      private double _areaPoly; // Площадь полилинии      
      private List<Aperture> _apertures;// Проемы в этом помещении
      private List<Supplement> _sups;// Добавки к площади или длине.
      private List<Toilet> _toilets;// Ящик под унитаз. Обычно он один в туалете, но на всякий случай пусть будет список, если перейдем на универсальные ящики.
      private Workspace _ws; // Рабочая область - Секция, Этаж.

      public List<Material> Materials { get { return _materials; } }
      public int Height { get { return _height; } }
      public string Floor { get { return _floor; } }
      /// <summary>
      /// Имя помещения
      /// </summary>
      public string Name { get { return _name; } }
      public string Description { get { return _description; } }
      /// <summary>
      /// Принадлежность помещения (МОП или имя квартиры)
      /// </summary>
      public string Owner { get { return _owner; } }      
      public ObjectId IdBlRefRoom { get { return _idBlRefRoom; } }
      public Point3d Position { get { return _position; } }

      public Workspace Ws
      {
         get { return _ws; }
         set { _ws = value; }
      }      

      public Room (BlockReference blRefRoom)
      {
         _materials = new List<Material>();
         _apertures = new List<Aperture>();
         _sups = new List<Supplement>();
         _toilets = new List<Toilet>();
         _floor = "";
         _name = "";         
         _idBlRefRoom = blRefRoom.Id;
         _position = blRefRoom.Position;         
         // Определение параметров помещения
         getParamsFromAttrs(blRefRoom);
         checkParams(blRefRoom);  
      }      

      private void getParamsFromAttrs(BlockReference blRefRoom)
      {
         foreach (ObjectId idAtrRef in blRefRoom.AttributeCollection)
         {
            using (var atrRef = idAtrRef.GetObject(OpenMode.ForRead) as AttributeReference)
            {
               string tag = atrRef.Tag.ToUpper();               
               // Принадлежность - имя квартиры или МОП 
               if (tag.Equals(Options.Instance.RoomBlAttrTagOwner.ToUpper()))
               {
                  _owner = atrRef.TextString; 
               }
               // Наименование помещения
               else if (tag.Equals(Options.Instance.RoomBlAttrTagName.ToUpper()))
               {
                  _name = atrRef.TextString;
               }
               // Высота
               else if (tag.Equals(Options.Instance.RoomBlAttrTagHeight.ToUpper()))
               {
                  int.TryParse(atrRef.TextString, out _height);
               }
               // Стены
               else if (tag.Equals(Options.Instance.RoomBlAttrTagWall.ToUpper()))
               {
                  Material material = new Material(atrRef.TextString, EnumConstructionType.Wall);
                  _materials.Add(material);
               }
               // Потолок
               else if (tag.Equals(Options.Instance.RoomBlAttrTagCeil.ToUpper()))
               {
                  Material material = new Material(atrRef.TextString, EnumConstructionType.Ceil);
                  _materials.Add(material);                  
               }
               // Пол
               else if (tag.Equals(Options.Instance.RoomBlAttrTagDeck.ToUpper()))
               {
                  Material material = new Material(atrRef.TextString, EnumConstructionType.Deck);
                  _materials.Add(material);                  
               }
               // Плинтус
               else if (tag.Equals(Options.Instance.RoomBlAttrTagBaseboard.ToUpper()))
               {
                  Material material = new Material(atrRef.TextString, EnumConstructionType.Baseboard);
                  _materials.Add(material);
               }
               // Карниз
               else if (tag.Equals(Options.Instance.RoomBlAttrTagCarnice.ToUpper()))
               {
                  Material material = new Material(atrRef.TextString, EnumConstructionType.Carnice);
                  _materials.Add(material);
               }
               // Примечание
               else if (tag.Equals(Options.Instance.RoomBlAttrTagDescription.ToUpper()))
               {
                  _description = atrRef.TextString;
               }
            }
         }
      }

      // Расчет материалов
      public void CalcMaterials()
      {         
         calcAreaWall();// Площадь стен  
         calcAreaDeck(); // Площадь пола       
         calcBaseboard();// Плинтус
         calcCarnice(); // Потолочный карниз.
         calcCeil(); // Потолок
      }

      private void calcCeil()
      {
         Material material = _materials.Find(m => m.Construction == EnumConstructionType.Ceil);
         // Площадь полилинии помещения.
         material.Value = _areaPoly;
      }

      private void calcAreaDeck()
      {
         // Площадь пола по полилинии помещения.
         Material material = _materials.Find(m => m.Construction == EnumConstructionType.Deck);
         material.Value = _areaPoly;
         // Из пложади пола (по полилинии помещения), вычесть добавку в блоке Supplement если она есть применимая к полу.
         supAdds(material, (sup) => sup.Lenght * sup.Width);
         // Короба у унитазов
         foreach (var toilet in _toilets)
         {
            material.Value += toilet.AddToDeckArea();
         }
      }

      private void calcCarnice()
      {
         // Материал карниза
         Material material = _materials.Find(m => m.Construction == EnumConstructionType.Carnice);
         material.Value = _perimeter;
         // Проемы на всю высоту помещения
         double lenDoorWithHeightRoom = _apertures.Where(a => (a is DoorAperture) && a.Height >= Height).Sum(d => d.Lenght);
         material.Value -= lenDoorWithHeightRoom;
      }

      private void calcBaseboard()
      {
         // Плинтус - длина м/п.                  
         Material material = _materials.Find(m => m.Construction == EnumConstructionType.Baseboard);
         material.Value = _perimeter;
         // Длина дверей - вичитается из длины плинтуса
         double lenDoors = _apertures.Where(a => a is DoorAperture).Sum(d => d.Lenght);
         material.Value -= lenDoors;
         // Учат унитазлов
         foreach (var toilet in _toilets)
         {
            material.Value += toilet.AddToBaseboardLen();
         }
      }            

      private void calcAreaWall()
      {
         // Материал стены
         Material materialWall = _materials.Find(m => m.Construction == EnumConstructionType.Wall);
         // Площадь стен общая
         double allAreaWall = _perimeter * _height;
         // Площадь проемов
         double allAreaApertures = _apertures.Sum(a => a.Area);
         // Площадь стен без проемов
         materialWall.Value = allAreaWall - allAreaApertures;
         // Добавки
         supAdds(materialWall, (sup) => sup.Lenght* sup.Width);
         // Унитазы
         foreach (var toilet in _toilets)
         {
            materialWall.Value += toilet.AddToWallArea();
         }
      }

      private void supAdds(Material materialOwner, Func<Supplement, double> funcSupValue)
      {         
         foreach (var supOwnerConstr in _sups.Where(s => s.Owner == materialOwner.Construction))
         {
            // Значение добавки
            double supvalue = funcSupValue(supOwnerConstr); //supOwnerConstr.Lenght * supOwnerConstr.Width;
            //  Если это добавка к материалу принадлежащей конструкции
            if (supOwnerConstr.Material == "" || supOwnerConstr.Material == "0"
               || string.Equals(supOwnerConstr.Material, materialOwner.Name, StringComparison.CurrentCultureIgnoreCase))
            {
               // Операция
               switch (supOwnerConstr.Operation)
               {
                  case EnumOperation.Addition:
                     materialOwner.Value += supvalue;
                     break;
                  case EnumOperation.Subtraction:
                     materialOwner.Value -= supvalue;
                     break;
                  case EnumOperation.Both:
                     break;
                  case EnumOperation.Undefined:
                     break;
                  default:
                     break;
               }
            }
            // Материал добавки отличается от материала принадлежащей конструкции
            else
            {
               // Есть ли уже такой материал (может быть много добавок, но по идее они должны быть разных материалов, но фиг их знает)
               Material supMaterial = _materials.Find(m => string.Equals(m.Name, supOwnerConstr.Material, StringComparison.CurrentCultureIgnoreCase));
               if (supMaterial == null)
               {
                  supMaterial = new Material(supOwnerConstr.Material, materialOwner.Construction);
               }
               supMaterial.Value += supvalue;
               _materials.Add(supMaterial);
            }
         }
      }

      // Добавление поема пересекающего помещение
      public void AddApertureIntersect(Aperture aperture)
      {
         _apertures.Add(aperture);
      }

      // Добавление добавки к площади или длине 
      public void AddSupplement(Supplement sup)
      {
         _sups.Add(sup);
      }

      // Добавление ящика под унитаз
      public void AddToilet(Toilet toilet)
      {
         _toilets.Add(toilet);
      }

      // Поиск полилинии в блоке помещения. Полилиния определяет периметр помещения.
      public Polyline GetPolyline()
      {
         using (var dboCol = new DBObjectCollection())
         {
            using (var blRefRoom = IdBlRefRoom.GetObject(OpenMode.ForRead) as BlockReference)
            {
               blRefRoom.Explode(dboCol);
               foreach (DBObject dbo in dboCol)
               {
                  var poly = dbo as Polyline;
                  if (poly != null)
                  {                     
                     _perimeter = poly.Length;
                     _areaPoly = poly.Area;
                     return poly;//.Clone() as Polyline;
                  }
               }
            }
         }
         return null;
      }     

      public bool Equals(Room other)
      {
         return _name.ToUpper() == other._name.ToUpper();            
      }

      private void checkParams(BlockReference blRefRoom)
      {
         string errMsg = string.Format("Ошибки в блоке {0}: ", Options.Instance.BlockRoomName );
         bool hasError = false;
         // Принадлежность
         if (string.IsNullOrEmpty(_owner))
         {
            errMsg += "Принадлежность помещения не определена. ";
            hasError = true;
         }
         // Наименование
         if (string.IsNullOrEmpty(_name))
         {
            errMsg += "Наименование помещения не определено. ";
            hasError = true;
         }
         // Плинтус
         Material material = _materials.Find(m => m.Construction == EnumConstructionType.Baseboard);
         if (material == null)
         {
            errMsg += "Матариал плинтуса не определен. ";
            hasError = true;
         }
         // Карниз
         material = _materials.Find(m => m.Construction == EnumConstructionType.Carnice);
         if (material == null)
         {
            errMsg += "Матариал карниза не определен. ";
            hasError = true;
         }
         // Потолка
         material = _materials.Find(m => m.Construction == EnumConstructionType.Ceil);
         if (material == null)
         {
            errMsg += "Матариал потолка не определен. ";
            hasError = true;
         }
         // Пол
         material = _materials.Find(m => m.Construction == EnumConstructionType.Deck);
         if (material == null)
         {
            errMsg += "Матариал пола не определен. ";
            hasError = true;
         }
         // Стены
         material = _materials.Find(m => m.Construction == EnumConstructionType.Wall);
         if (material == null)
         {
            errMsg += "Матариал стен не определен. ";
            hasError = true;
         }
         // Высота
         if (_height <= 0)
         {
            errMsg += "Высота помещения не определена. ";
            hasError = true;
         }
         if (hasError)
         {
            Inspector.AddError(errMsg, blRefRoom);
         }
      }
   }
}
