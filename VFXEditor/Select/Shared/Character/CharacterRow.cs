using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;

namespace VfxEditor.Select.Shared.Character {
    public class CharacterRow {
        public readonly string Name;
        public readonly string SkeletonId;
        public readonly int HairOffset;

        public CharacterRow( string name, RaceStruct race ) {
            Name = name;
            SkeletonId = race.SkeletonId;
            HairOffset = race.HairOffset;
        }

        public string EidPath => $"chara/human/{SkeletonId}/skeleton/base/b0001/eid_{SkeletonId}b0001.eid";

        public string GetLoopPap( int poseId ) => $"chara/human/{SkeletonId}/animation/a0001/bt_common/emote/pose" + poseId.ToString().PadLeft( 2, '0' ) + "_loop.pap";

        public string GetStartPap( int poseId ) => $"chara/human/{SkeletonId}/animation/a0001/bt_common/emote/pose" + poseId.ToString().PadLeft( 2, '0' ) + "_start.pap";

        public string GetPap( string path ) => $"chara/human/{SkeletonId}/animation/a0001/bt_common/resident/{path}.pap";

        public List<int> GetHairIds() {
            var ret = new List<int>();
            var sheet = Plugin.DataManager.GetExcelSheet<CharaMakeCustomize>();
            for( var hair = HairOffset; hair < HairOffset + SelectUtils.HairEntries; hair++ ) {
                var hairRow = sheet.GetRow( ( uint )hair );
                var hairId = ( int )hairRow.FeatureID;
                if( hairId == 0 ) continue;

                ret.Add( hairId );
            }
            return ret;
        }
    }
}
