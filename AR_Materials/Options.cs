using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AR_Materials
{
   public class Options
   {
      private static Options _options;
      // Блоки
      private string _blRoomName = "АР_Помещение";
      private string _blApertureName = "АР_Проём";
      private string _blockWorkspaceName = "АР_Рабочая область";
      private string _blockSupplementName = "АРМ_Добавка";
      private string _blockToiletName = "АРМ_Унитаз";
      // Слои
      private string _layerApertureWindow = "АР_Окна";
      private string _layerApertureDoor = "АР_Двери";      
      // Атрибуты блоков и дин параметры.
      // Room - помещения
      private string _roomBlAttrTagOwner = "Принадлежность";
      private string _roomBlAttrTagName = "Наименование";
      private string _roomBlAttrTagHeight = "Высота";
      private string _roomBlAttrTagWall = "Стены";
      private string _roomBlAttrTagCeil = "Потолок";
      private string _roomBlAttrTagDeck = "Пол";      
      private string _roomBlAttrTagBaseboard = "Плинтус";
      private string _roomBlAttrTagCarnice = "Карниз";
      private string _roomBlAttrTagDescription = "Примечание";
      // Проемы
      private string _apertureBlDynPropLenght = "Расстояние";
      private string _apertureBlAttrTagHeight = "Высота";
      // Workspace - рабочие области
      private string _workspaceBlAttrTagSection = "Секция";
      private string _workspaceBlAttrTagProject = "Адрес";
      // Supplement - добавки к площади или длине
      private string _supplementBlAttrTagOwner = "Принадлежность";
      private string _supplementBlAttrTagMaterial = "Материал";
      private string _supplementBlAttrTagOperation = "Действие";
      // Toilet - ящик у унитаза
      private string _toiletBlAttrTagHeight = "Высота";
      private string _toiletBlAttrTagContact = "Примыкание";

      // Свойства
      public string BlockWorkspaceName { get { return _blockWorkspaceName; } }
      public string BlockRoomName { get { return _blRoomName; } }      
      public string BlockApertureName { get { return _blApertureName; } }
      public string BlockSupplementName { get { return _blockSupplementName; } }
      public string BlockToiletName { get { return _blockToiletName; } }
      public string LayerApertureWindow { get { return _layerApertureWindow; } }      
      public string LayerApertureDoor { get { return _layerApertureDoor; } }
      public string ApertureBlDynPropLenght { get { return _apertureBlDynPropLenght; } }
      public string ApertureBlAttrTagHeight { get { return _apertureBlAttrTagHeight; } }      
      public string RoomBlAttrTagName { get { return _roomBlAttrTagName; } }
      public string RoomBlAttrTagDeck { get { return _roomBlAttrTagDeck; } }
      public string RoomBlAttrTagCeil { get { return _roomBlAttrTagCeil; } }
      public string RoomBlAttrTagWall { get { return _roomBlAttrTagWall; } }
      public string RoomBlAttrTagOwner { get { return _roomBlAttrTagOwner; } }
      public string RoomBlAttrTagHeight { get { return _roomBlAttrTagHeight; } }
      public string RoomBlAttrTagBaseboard { get { return _roomBlAttrTagBaseboard; } }
      public string RoomBlAttrTagCarnice { get { return _roomBlAttrTagCarnice; } }
      public string RoomBlAttrTagDescription { get { return _roomBlAttrTagDescription; } }      
      public string WorkspaceBlAttrTagSection { get { return _workspaceBlAttrTagSection; } }
      public string WorkspaceBlAttrTagProject { get { return _workspaceBlAttrTagProject; } }
      public string SupplementBlAttrTagOwner { get { return _supplementBlAttrTagOwner; } }
      public string SupplementBlAttrTagOperation { get { return _supplementBlAttrTagOperation; } }
      public string SupplementBlAttrTagMaterial { get { return _supplementBlAttrTagMaterial; } }
      public string ToiletBlAttrTagHeight { get { return _toiletBlAttrTagHeight; } }
      public string ToiletBlAttrTagContact { get { return _toiletBlAttrTagContact; } }

      private Options()
      { }

      public static Options Instance
      {
         get
         {
            if (_options == null) _options = new Options();
            return _options;
         }
      }
   }
}
