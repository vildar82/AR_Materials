using System.Collections.Generic;
using System.Reflection;
using System.Text;
using AcadLib.Blocks;
using AcadLib.Errors;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

[assembly: CommandClass(typeof(AR_Materials.Commands))]

namespace AR_Materials
{
    public class Commands
    {
        [CommandMethod("PIK", nameof(ARMHelp), CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void ARMHelp()
        {
            AcadLib.CommandStart.Start((doc) =>
            {
                StringBuilder msgHelp = new StringBuilder();
                msgHelp.AppendLine("ARM-MaterialsCalc - Программа расчета материалов помещений по выбранным блокам.");
                msgHelp.Append("Версия ");
                msgHelp.Append(Assembly.GetExecutingAssembly().GetName().Version);
                msgHelp.AppendLine();
                msgHelp.AppendLine("Блоки для расчета:");
                msgHelp.AppendLine("АРМ_Помещение - динамический блок помещения. Для сложного в плане помещения выполнить контестное редактирование этого блока.");
                msgHelp.AppendLine("АРМ_Проём - блок для проемов. Для дверного проема располагать этот блок на слое АР_Двери, для оконного - АР_Окна. Блок должен пересекать полилинию нужного блока помещения.");
                msgHelp.AppendLine("АРМ_Добавка - блок добавки к материалу. Величина добавки определяется по ширине и длине этого блока. Значение атрибутов: Принадлежность - стены, пол, потолок, плинтус, карниз; Материал - имя материала добавки, если пусто, то считается материал принадлежащей конструкцтии из первого атрибута; Действие - +, -, или +- (отнять из материала принадлежащей конструкции и добавить указанный материал в добавке). Точка вставки блока должна быть внутри/на полилинии нужного блока помещения.");
                msgHelp.AppendLine("АРМ_Унитаз - блок для учета короба над унитазом. Атрибут примыкания: 1 - если короб примыкает, только длинной стороной к стене; 2 - если длинной и короткой. Точка вставки блока должна быть внутри/на полилинии нужного блока помещения.");

                doc.Editor.WriteMessage("\n{0}", msgHelp);
            });
        }

        [CommandMethod("PIK", "ARM-MaterialsCalc", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void ARMMaterialsCalcCommand()
        {
            AcadLib.CommandStart.Start((doc) =>
            {
                // Поиск всех блоков помещений и проемов в чертеже.
                Model.Materials.RoomService roomService = new Model.Materials.RoomService();
                roomService.FindAllBlocks();
                if (Inspector.HasErrors)
                {
                    Inspector.Show();
                    return;
                }

                // Поиск пересечений проемов и помещений.
                roomService.FindIntersections();
                if (Inspector.HasErrors)
                {
                    Inspector.Show();
                    return;
                }

                // Подсчет материалов
                roomService.CalcMaterials();
                if (Inspector.HasErrors)
                {
                    Inspector.Show();
                    return;
                }

                // Построение таблицы.
                Model.Materials.Result.ToTable toTable = new Model.Materials.Result.ToTable(roomService.Rooms);
                toTable.Insert();
            });
        }

        /// <summary>
        /// Развертка стен выбранного блока с помещениями
        /// </summary>
        [CommandMethod("PIK", "AI-RollUp", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void ARI_RollUp()
        {
            AcadLib.CommandStart.Start((doc) =>
            {
                Model.Interiors.RollUpService.CreateRollUp();
            });            
        }
    }
}