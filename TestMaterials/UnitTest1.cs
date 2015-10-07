using System;
using AR_Materials.Model;
using Autodesk.AutoCAD.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestMaterials
{
   [TestClass]
   public class UnitTest1
   {
      [TestMethod]
      [CommandMethod("TestFindBlocksRoomsApertures")]
      public void TestFindBlocksRoomsApertures()
      {
         RoomService roomService = new RoomService();
         roomService.FindAllBlocks();
         roomService.FindIntersections();
         roomService.CalcMaterials();

         var rooms = roomService.Rooms;
      }
   }
}
