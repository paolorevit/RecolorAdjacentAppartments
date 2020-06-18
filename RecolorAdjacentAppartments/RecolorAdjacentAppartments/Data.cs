using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RecolorAdjacentAppartments
{
    public enum RoomParams
    {
        RomZone,
        BSBlock,
        RomSubzone,
        RomCalcSubzoneId,
        RomSubzoneIndex
    }
    public static class Data
    {
        public static Dictionary<RoomParams, string> RoomParamsDict = new Dictionary<RoomParams, string>()
        {

            {RoomParams.RomZone,"ROM_Зона"},
            {RoomParams.BSBlock,"BS_Блок"},
            {RoomParams.RomSubzone,"ROM_Подзона"},
            {RoomParams.RomCalcSubzoneId,"ROM_Расчетная_подзона_ID"},
            {RoomParams.RomSubzoneIndex,"ROM_Подзона_Index"}
        };
        public static string appartmentNameIdentifier = "квартира";
        public static Regex regexForRoomZone = new Regex( appartmentNameIdentifier+@"(\s*)(\d+)");
        public static string suffix = ".Полутон";
    }
}

