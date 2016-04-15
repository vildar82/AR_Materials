using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AcadLib;
using AcadLib.Blocks;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace AR_Materials.Model.Interiors
{
    /// <summary>
    /// Вид - определяет вид для развертки стен.
    /// Отдельная стрелочка вида в блоке. В блоке вида может быть несколько видов
    /// </summary>
    public class View : IComparable<View>
    {
        public ObjectId IdBlRef { get; set; }
        public Point3d Position { get; set; }
        public string BlName { get; set; }
        public Vector3d Vector { get; set; }
        public AttributeInfo AtrInfo { get; set; }
        public int Number { get; set; }
        /// <summary>
        /// Расстояние до сегмента - перпендикулярно
        /// </summary>
        public double DistToSegment { get; set; }
        /// <summary>
        /// Сегмент определенный для этого вида
        /// </summary>
        public RollSegment Segment { get; set; }

        public View(BlockReference blRef, string blName, AttributeInfo atrView, double angleView)
        {
            this.IdBlRef = blRef.Id;
            this.BlName = BlName;
            this.Position = blRef.Position;
            // Вектор направления вида
            Vector = Vector3d.XAxis.RotateBy(angleView, Vector3d.ZAxis);
            AtrInfo = atrView;
            int num;
            Number = int.TryParse(atrView.Text, out num) ? num : 0;
        }

        public static List<View> GetViews(BlockReference blRefView, string blName, BlockReference blRefFlat)
        {
            // Из блока вида - вытажить все виды
            List<View> views = new List<View>();
            var rotationBlRef = blRefView.Rotation;
            var attrs = AcadLib.Blocks.AttributeInfo.GetAttrRefs(blRefView);

            var angleView = getAngleView(blRefView);
            if(angleView.Failure)
            {
                Inspector.AddError($"Ошибка в блоке вида - {angleView.Error}", 
                    blRefView, blRefFlat.BlockTransform, System.Drawing.SystemIcons.Error);
                return views;
            }            

            // Создание вида для каждого атрибута вида
            foreach (var atrView in attrs.Where(v=>v.Tag.StartsWith("ВИД", StringComparison.OrdinalIgnoreCase)))
            {
                View view = null;
                // Индекс вида в блоке - из тега атрибута вида - от 1 до 4
                int indexView = getIndexView(atrView.Tag);
                switch (indexView)
                {
                    case 1:
                        view = new View(blRefView, blName, atrView, angleView.Value + rotationBlRef);
                        break;
                    case 2:
                        view = new View(blRefView, blName, atrView, angleView.Value + rotationBlRef-90d.ToRadians());
                        break;
                    case 3:
                        view = new View(blRefView, blName, atrView, angleView.Value + rotationBlRef - 180d.ToRadians());
                        break;
                    case 4:
                        view = new View(blRefView, blName, atrView, angleView.Value + rotationBlRef - 270d.ToRadians());
                        break;
                    default:
                        Inspector.AddError($"В блоке вида '{blName}' не определен вид по атрибуту '{atrView.Tag}'",
                            blRefView, blRefFlat.BlockTransform, System.Drawing.SystemIcons.Error);
                        break;
                }
                if (view != null)
                {
                    views.Add(view);
                }
            }

            return views;
        }

        private static int getIndexView(string tag)
        {
            if (tag.Equals("ВИД", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }
            var value = tag.Substring("ВИД".Length);
            int i;
            if (int.TryParse(value, out i))
            {
                return i;
            }
            return 0;
        }

        private static Result<double> getAngleView(BlockReference blRef)
        {
            if(blRef.DynamicBlockReferencePropertyCollection != null)
            {
                foreach (DynamicBlockReferenceProperty prop in blRef.DynamicBlockReferencePropertyCollection)
                {
                    if(prop.PropertyName.Equals("Угол1", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result.Ok(Convert.ToDouble(prop.Value));
                    }
                }
            }
            return Result.Fail<double>("Не найден динамический параметр 'Угол1'");
        }

        public int CompareTo(View other)
        {
            return Number.CompareTo(other?.Number);
        }
    }
}