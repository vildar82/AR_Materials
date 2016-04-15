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
        [CommandMethod("PIK", "ARM-Help", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void ARMHelpCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;

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
        }

        [CommandMethod("PIK", "ARM-MaterialsCalc", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void ARMMaterialsCalcCommand()
        {
            try
            {
                AutoCAD_PIK_Manager.Log.Info("Plugin AR_Materials Start Command MaterialsCalc");

                Inspector.Clear();
                Counter.Clear();

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

                // Експорт в ексель.            
            }
            catch (System.Exception ex)
            {
                Document doc = Application.DocumentManager.MdiActiveDocument;
                doc.Editor.WriteMessage("\n{0}", ex.ToString());
                AutoCAD_PIK_Manager.Log.Error(ex, "MaterialsCalc");
            }
        }

        /// <summary>
        /// Развертка стен выбранного блока с помещениями
        /// </summary>
        [CommandMethod("PIK", "ARI-RoomRollUp", CommandFlags.Modal | CommandFlags.NoPaperSpace | CommandFlags.NoBlockEditor)]
        public void ARI_RoomsRollUp()
        {
            Logger.Log.Info("Start command ARI-RoomRollUp");
            Document doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;            
            try
            {
                Inspector.Clear();
                Model.Interiors.RollUpService.CreateRollUp();
                Inspector.Show();
            }
            catch (System.Exception ex)
            {
                doc.Editor.WriteMessage($"\nОшибка : {ex.Message}");
                if (!ex.Message.Contains("Отменено пользователем"))
                {
                    Logger.Log.Error(ex, $"Command: ARI-RoomRollUp. {doc.Name}");
                }
            }
        }
    }
}