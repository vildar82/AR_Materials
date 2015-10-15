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
   // Помещения
   public class RoomService
   {
      private List<Room> _rooms;     // Помещения 
      private List<Aperture> _apertures; // Проемы
      private List<Workspace> _ws; // Рабочие области
      private List<Supplement> _sups; // Добавки к площади или длине
      private List<Toilet> _toilets; // Короб под унитаз в туалетье

      public List<Room> Rooms { get { return _rooms; } }
      public List<Aperture> Apertures { get { return _apertures; } }

      public RoomService()
      {
         _rooms = new List<Room>();
         _apertures = new List<Aperture>();
         _ws = new List<Workspace>();
         _sups = new List<Supplement>();
         _toilets = new List<Toilet>();
      }

      public void FindAllBlocks()
      {
         Document doc = Application.DocumentManager.MdiActiveDocument;
         Editor ed = doc.Editor;
         var resSel = ed.GetSelection();
         if (resSel.Status != PromptStatus.OK)
         {
            throw new Exception("Отменено пользователем");            
         }        
           
         // Поиск блоков помещений на чертеже и блоков проемов
         Database db = HostApplicationServices.WorkingDatabase;
         using (var t = db.TransactionManager.StartTransaction())
         {
            //var ms = t.GetObject(SymbolUtilityServices.GetBlockModelSpaceId(db), OpenMode.ForRead) as BlockTableRecord;
            foreach (ObjectId idEnt in resSel.Value.GetObjectIds())
            {
               if (idEnt.ObjectClass.Name == "AcDbBlockReference")
               {
                  var blRef = t.GetObject(idEnt, OpenMode.ForRead) as BlockReference;
                  string blName = Blocks.GetEffectiveName(blRef).ToUpper();
                  
                  // Помещение
                  if (blName.StartsWith (Options.Instance.BlockRoomName.ToUpper()))
                  {
                     Room room = new Room(blRef);
                     _rooms.Add(room);
                     Counter.AddCount(Options.Instance.BlockRoomName);
                  }
                  // Проём
                  else if (blName.Equals (Options.Instance.BlockApertureName.ToUpper()))
                  {
                     Aperture aperture = ApertureFactory.GetAperture(blRef);
                     if (aperture != null)
                     {
                        _apertures.Add(aperture);
                        Counter.AddCount(Options.Instance.BlockApertureName);
                     }
                     else
                     {
                        Inspector.AddError("Не определен тип проема. Блок проема должен быть на слое окна или двери. См. справку - MaterialsHelp.",
                                             blRef);
                     }
                  }
                  //// Рабочая область
                  //else if (blName.Equals(Options.Instance.BlockWorkspaceName.ToUpper()))
                  //{
                  //   Workspace ws = new Workspace(blRef);
                  //   _ws.Add(ws);
                  //   Counter.AddCount(Options.Instance.BlockWorkspaceName);
                  //}
                  // Добавка
                  else if (blName.Equals(Options.Instance.BlockSupplementName.ToUpper()))
                  {
                     Supplement sup = new Supplement(blRef);
                     _sups.Add(sup);
                     Counter.AddCount(Options.Instance.BlockSupplementName);
                  }
                  // Короб под унитаз
                  else if (blName.Equals(Options.Instance.BlockToiletName.ToUpper()))
                  {
                     Toilet toilet = new Toilet(blRef);
                     _toilets.Add(toilet);
                     Counter.AddCount(Options.Instance.BlockToiletName);
                  }
               }
            }
            ed.WriteMessage("\n{0}", Counter.Report());
            t.Commit();
         }
      }      

      public void CalcMaterials()
      {         
         foreach (var room in _rooms)
         {
            room.CalcMaterials();
         }
      }

      // Поиск пересечений помещений и проемов. Определение принадлежности секции.
      public void FindIntersections()
      {
         // Поиск пересечений проемов и помещений
         Database db = HostApplicationServices.WorkingDatabase;
         using (var t = db.TransactionManager.StartTransaction())
         {
            // Список блоков проемов и объектов Aperture
            var listBlRefApertures = Apertures.Select(s => new { Aperture = s, BlRef = t.GetObject(s.IdBlRef, OpenMode.ForRead) as BlockReference });
            // Список полилиний и объектов Room            
            var listPolyInRooms = Rooms.Select(r => new { Room = r, Polyline = r.GetPolyline() });
            foreach (var blRefAperture in listBlRefApertures)
            {
               bool hasIntersect = false;
               foreach (var polyRoom in listPolyInRooms)
               {
                  using (var pts = new Point3dCollection())
                  {
                     polyRoom.Polyline.IntersectWith(blRefAperture.BlRef, Intersect.ExtendBoth, pts, IntPtr.Zero, IntPtr.Zero);
                     if (pts.Count > 0)
                     {
                        polyRoom.Room.AddApertureIntersect(blRefAperture.Aperture);
                        hasIntersect = true;
                     }
                  }
               }
               if (!hasIntersect)
               {
                  // TODO: Добавить в список ошибок.                  
               }
            }
            // Определение принадлежности помещения рабочей области
            findRoomsWorkspace();

            // Если есть блоки добавок, то распределение их по помещениям.
            foreach (var sup in _sups)
            {
               bool hasIntersect = false;
               foreach (var polyRoom in listPolyInRooms)
               {
                  if (IsInsidePolygon(polyRoom.Polyline, sup.Position) ||
                     IsPointOnPolyline(polyRoom.Polyline, sup.Position))
                  {
                     polyRoom.Room.AddSupplement(sup);
                  }                  
               }
               if (!hasIntersect)
               {
                  // TODO: Добавить в список ошибок.
               }
            }
            // Если есть блоки ящиков, то распределение их по помещениям.
            foreach (var toilet in _toilets)
            {
               bool hasIntersect = false;
               foreach (var polyRoom in listPolyInRooms)
               {
                  if (IsInsidePolygon(polyRoom.Polyline, toilet.Position) ||
                     IsPointOnPolyline(polyRoom.Polyline, toilet.Position))
                  {
                     polyRoom.Room.AddToilet(toilet);
                  }
               }
               if (!hasIntersect)
               {
                  // TODO: Добавить в список ошибок.
               }
            }
            t.Abort();
         }
      }     

      // Определение принадлежности помещения рабочей области
      private void findRoomsWorkspace()
      {
         var defaultWS = new Workspace(null);
         bool isFindWS = false;
         foreach (var room in _rooms)
         {
            foreach (var ws in _ws)
            {
               if (isPointInBounds(room.Position, ws.Extents))
               {
                  room.Ws = ws;
                  isFindWS = true; 
               }
            }
            if (!isFindWS)
            {
               room.Ws = defaultWS; 
            }
         }
      }

      // Попадает ли точка внутрь границы
      private bool isPointInBounds(Point3d pt, Extents3d bounds)
      {
         bool res = false;
         if (pt.X > bounds.MinPoint.X && pt.Y > bounds.MinPoint.Y &&
            pt.X < bounds.MaxPoint.X && pt.Y < bounds.MaxPoint.Y)
         {
            res = true;
         }
         return res;
      }

      public static bool IsInsidePolygon(Polyline polygon, Point3d pt)
      {
         int n = polygon.NumberOfVertices;
         double angle = 0;
         Point pt1, pt2;

         for (int i = 0; i < n; i++)
         {
            pt1.X = polygon.GetPoint2dAt(i).X - pt.X;
            pt1.Y = polygon.GetPoint2dAt(i).Y - pt.Y;
            pt2.X = polygon.GetPoint2dAt((i + 1) % n).X - pt.X;
            pt2.Y = polygon.GetPoint2dAt((i + 1) % n).Y - pt.Y;
            angle += Angle2D(pt1.X, pt1.Y, pt2.X, pt2.Y);
         }

         if (Math.Abs(angle) < Math.PI)
            return false;
         else
            return true;
      }

      private struct Point
      {
         public double X, Y;
      }

      private static double Angle2D(double x1, double y1, double x2, double y2)
      {
         double dtheta, theta1, theta2;

         theta1 = Math.Atan2(y1, x1);
         theta2 = Math.Atan2(y2, x2);
         dtheta = theta2 - theta1;
         while (dtheta > Math.PI)
            dtheta -= (Math.PI * 2);
         while (dtheta < -Math.PI)
            dtheta += (Math.PI * 2);
         return (dtheta);
      }

      public static bool IsPointOnPolyline(Polyline pl, Point3d pt)
      {
         bool isOn = false;
         for (int i = 0; i < pl.NumberOfVertices; i++)
         {
            Curve3d seg = null;

            SegmentType segType = pl.GetSegmentType(i);
            if (segType == SegmentType.Arc)
               seg = pl.GetArcSegmentAt(i);
            else if (segType == SegmentType.Line)
               seg = pl.GetLineSegmentAt(i);

            if (seg != null)
            {
               isOn = seg.IsOn(pt);
               if (isOn)
                  break;
            }
         }
         return isOn;
      }

      /// <summary>
      /// Определение типа конструкции по строке
      /// </summary>
      /// <param name="input">Имя конструкции: Стена, Пол, Потолок, Плинтус, Карниз</param>
      /// <returns></returns>
      public static EnumConstructionType GetConstructionType(string input)
      {
         if (string.IsNullOrEmpty(input))
            return EnumConstructionType.Undefined;
         string toupper = input.ToUpper();
         if (toupper.StartsWith("СТЕН")) // Могут написать Стены, Стена         
            return EnumConstructionType.Wall;
         if (toupper.StartsWith("ПОЛ")) // Пол, Полы, Пола         
            return EnumConstructionType.Deck;
         if (toupper.StartsWith("ПОТО")) // Потолок, Потолка         
            return EnumConstructionType.Ceil;
         if (toupper.StartsWith("ПЛИН")) // Плинтус         
            return EnumConstructionType.Baseboard;
         if (toupper.StartsWith("КАРН")) // Карниз         
            return EnumConstructionType.Carnice;
         return EnumConstructionType.Undefined;
      }
   }
}
