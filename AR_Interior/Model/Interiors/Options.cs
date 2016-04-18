using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using AcadLib.Files;
using Autodesk.AutoCAD.ApplicationServices;

namespace AR_Materials.Model.Interiors
{
    [Serializable]
    public class Options
    {
        private static readonly string fileOptions = Path.Combine(
                       AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder,
                       "АР\\Interior\\AR_Interior_Options.xml");
        private static Options _instance;
        public static Options Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;
            }
        }

        private Options() { }

        //
        // Развертки стен
        //
                
        [Category("Развертки")]
        [DisplayName("Расстояние между видами")]
        [Description("Расстояние между видами при построении разверток помещения")]
        [DefaultValue(1000)]
        public double RollViewOffset { get; set; } = 1000;

        [Category("Развертки")]
        [DisplayName("Минимальная длина сегмента")]
        [Description("Сегменты меньше заданной длины будут включаться в обшую развертку соседних сегментов.")]
        [DefaultValue(300)]
        public double RollSegmentPartitionLength { get; set; } = 300;

        [Category("Блоки")]
        [DisplayName("Имя блока вида")]
        [Description("Имя блока вида начинается с 'АИ_Вид'")]
        [DefaultValue("АИ_Вид")]
        public string BlockNameViewStart { get; set; } = "АИ_Вид";

        [Category("Блоки")]
        [DisplayName("Имя блока номера помещение")]
        [Description("Имя блока номера помещение")]
        [DefaultValue("АИ_Номер_Помещения")]
        public string BlockNameRoomNumber { get; set; } = "АИ_Номер_Помещения";

        [Category("Блоки")]
        [DisplayName("Атрибут номера помещения")]
        [Description("Атрибут номера помещения в блоке номера помещения")]
        [DefaultValue("Номер")]
        public string AttrRoomNumber { get; set; } = "Номер";

        public static Options Load()
        {
            Options options = null;
            // загрузка из файла настроек
            if (File.Exists(fileOptions))
            {
                SerializerXml xmlSer = new SerializerXml(fileOptions);
                try
                {
                    options = xmlSer.DeserializeXmlFile<Options>();
                    if (options != null)
                    {
                        return options;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex, $"Не удалось десериализовать настройки из файла {fileOptions}");
                }
            }
            options = new Options();
            options.Save();
            return options;
        }

        public void Save()
        {
            try
            {
                if (!File.Exists(fileOptions))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileOptions));
                }
                SerializerXml xmlSer = new SerializerXml(fileOptions);
                xmlSer.SerializeList(this);
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex, $"Не удалось сериализовать настройки в {fileOptions}");
            }
        }        

        public static void Show()
        {
            AcadLib.UI.FormProperties formOpt = new AcadLib.UI.FormProperties();
            var copyOptions = (Options)Instance.MemberwiseClone();
            formOpt.propertyGrid1.SelectedObject = copyOptions;
            if (Application.ShowModalDialog(formOpt) == System.Windows.Forms.DialogResult.OK)
            {
                _instance = copyOptions;
                _instance.Save();
            }
        }
    }
}