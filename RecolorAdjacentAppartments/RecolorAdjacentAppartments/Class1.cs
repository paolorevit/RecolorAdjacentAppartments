using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RecolorAdjacentAppartments
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class Class1 : IExternalCommand
    {
        Document doc;
        int changedGroupsCount = 0;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            doc = uidoc.Document;

            if (IsMainExecutionNeeded(ref message))
            {
                using (Transaction transaction = new Transaction(doc))
                {
                    transaction.Start("RecolorAdjacentAppartments");
                    MainCommand();
                    transaction.Commit();
                }
                TaskDialog.Show("revit", "Work is finished.\nChanged group count: " + changedGroupsCount.ToString());
                return Result.Succeeded;
            }
            else return Result.Failed;
        }
        private bool IsMainExecutionNeeded(ref string message)
        {
            bool status = true;
            List<Room> presentedRooms = new FilteredElementCollector(doc).WherePasses(new RoomFilter()).Cast<Room>().ToList();
            if (presentedRooms.Any())
            {
                Room anyRoom = presentedRooms.First();
                foreach (RoomParams roomParam in Enum.GetValues(typeof(RoomParams)))
                {
                    if (!(anyRoom.GetParameters(Data.RoomParamsDict[roomParam]).Any()))
                    {
                        message += "Some missed rooms parameters: " + Data.RoomParamsDict[roomParam] + "\n";
                        status = false;
                    }
                }
            }
            else
            {
                message = "There are no rooms in document";
                status = false;
            }
            return status;
        }
        private void MainCommand()
        {
            List<List<Room>> allGroupsByLevelAndBlock = GetGroupsByLevelAndBlock();
            foreach (List<Room> eachGroupByLevelAndBlock in allGroupsByLevelAndBlock)
            {
                int groupsOnLevelCount = eachGroupByLevelAndBlock.GroupBy(x => GetZoneNumber(x)).ToList().Count;
                List<List<Room>> similarGroups = GetSimilarGroups(eachGroupByLevelAndBlock);
                RecolorSimilarAdjacentGroups(similarGroups, groupsOnLevelCount);
            }
        }
        private List<List<Room>> GetGroupsByLevelAndBlock()
        {
            List<Room> presentedRooms = new FilteredElementCollector(doc).WherePasses(new RoomFilter()).Cast<Room>().Where(IsRoomAcceptable).ToList();
            return presentedRooms.GroupBy(x => new
            {
                LevelId = x.LevelId,
                BsBlock = GetRoomParameterByName(x, (Data.RoomParamsDict[RoomParams.BSBlock])),
            }).Select(s => s.ToList()).ToList();
        }
        private bool IsRoomAcceptable(Room room)
        {
            bool areaStatus = room.Area > 1e-7;

            string roomZone = GetRoomParameterByName(room, Data.RoomParamsDict[RoomParams.RomZone]).ToLower();
            bool nameStatus = roomZone.Contains(Data.zoneNameIdentifier);

            return areaStatus && nameStatus;
        }
        private string GetRoomParameterByName(Room room, string name)
        {
            return room.GetParameters(name).First().AsString();
        }
        private int GetZoneNumber(Room room)
        {
            int result = int.MinValue + 1;
            string romZone = room.GetParameters(Data.RoomParamsDict[RoomParams.RomZone]).First().AsString();
            if (Data.regexForRoomZone.IsMatch(romZone.ToLower()))
            {
                string romZoneSubstring = Data.regexForRoomZone.Match(romZone.ToLower()).Value;
                romZoneSubstring = Regex.Replace(romZoneSubstring, @"\s+", "");
                string roomNumberText = romZoneSubstring.Split(new string[] { Data.zoneNameIdentifier }, StringSplitOptions.None)[1];
                int.TryParse(roomNumberText, out result);
            }
            return result;
        }
        private List<List<Room>> GetSimilarGroups(List<Room> eachGroupByLevelAndBlock)
        {
            return eachGroupByLevelAndBlock.GroupBy(x => new
            {
                RomSubzone = GetRoomParameterByName(x, (Data.RoomParamsDict[RoomParams.RomSubzone])),
                RomSubzoneIndex = GetRoomParameterByName(x, (Data.RoomParamsDict[RoomParams.RomSubzoneIndex])),
            }).Select(s => s.ToList()).ToList();
        }
        private void RecolorSimilarAdjacentGroups(List<List<Room>> similarGroups, int groupsOnLevelCount)
        {
            foreach (List<Room> group in similarGroups)
            {
                List<int> allZoneNumbers = group.GroupBy(x => GetZoneNumber(x)).Select(s => s.Key).OrderBy(x => x).ToList();
                List<int> preservedZoneNumbers = GetPreservedZoneNumbers(allZoneNumbers, groupsOnLevelCount);
                List<Room> roomsForColorChanging = group.Where(x => !preservedZoneNumbers.Contains(GetZoneNumber(x))).ToList();
                ChangeRoomsColor(roomsForColorChanging);

                changedGroupsCount += allZoneNumbers.Count - preservedZoneNumbers.Count;
            }
        }
        private List<int> GetPreservedZoneNumbers(List<int> allZoneNumbers, int groupsOnLevelCount)
        {
            List<int> preservedZoneNumbers = new List<int>();
            foreach (int zoneNumber in allZoneNumbers)
            {
                if (!preservedZoneNumbers.Contains(zoneNumber - 1))
                {
                    if (zoneNumber != groupsOnLevelCount) preservedZoneNumbers.Add(zoneNumber);
                    else
                    {
                        if (!preservedZoneNumbers.Contains(1)) preservedZoneNumbers.Add(zoneNumber);
                    }
                }
            }
            return preservedZoneNumbers;
        }
        private void ChangeRoomsColor(IEnumerable<Room> group)
        {
            foreach (Room room in group)
            {
                string value = GetRoomParameterByName(room, Data.RoomParamsDict[RoomParams.RomCalcSubzoneId]);
                value += Data.suffix;
                room.GetParameters(Data.RoomParamsDict[RoomParams.RomSubzoneIndex]).First().Set(value);
            }
        }
    }
}
