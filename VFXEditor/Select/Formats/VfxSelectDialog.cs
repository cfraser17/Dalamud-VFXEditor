using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Collections.Generic;
using VfxEditor.AvfxFormat;
using VfxEditor.Select.Tabs.Actions;
using VfxEditor.Select.Tabs.Common;
using VfxEditor.Select.Tabs.Cutscenes;
using VfxEditor.Select.Tabs.Emotes;
using VfxEditor.Select.Tabs.Gimmick;
using VfxEditor.Select.Tabs.Housing;
using VfxEditor.Select.Tabs.Items;
using VfxEditor.Select.Tabs.JournalCutscene;
using VfxEditor.Select.Tabs.Mounts;
using VfxEditor.Select.Tabs.Npc;
using VfxEditor.Select.Tabs.Statuses;
using VfxEditor.Select.Tabs.Zone;
using VfxEditor.Spawn;

namespace VfxEditor.Select.Formats {
    public class VfxSelectDialog : SelectDialog {
        public VfxSelectDialog( string id, AvfxManager manager, bool isSourceDialog ) : base( id, "avfx", manager, isSourceDialog ) {
            GameTabs.AddRange( new List<SelectTab>() {
                new ItemTabVfx( this, "Item" ),
                new StatusTabVfx( this, "Status" ),
                new ActionTabVfx( this, "Action" ),
                new ActionTabVfxNonPlayer( this, "Non-Player Action" ),
                new EmoteTabVfx( this, "Emote" ),
                new ZoneTabVfx( this, "Zone" ),
                new GimmickTab( this, "Gimmick" ),
                new HousingTab( this, "Housing" ),
                new NpcTabVfx( this, "Npc" ),
                new MountTabVfx( this, "Mount" ),
                new CutsceneTab( this, "Cutscene" ),
                new JournalCutsceneTab( this, "Journal Cutscene" ),
                new CommonTabVfx( this, "Common" )
            } );
        }

        public override bool CanPlay => true;

        public override void PlayButton( string path ) {
            using var font = ImRaii.PushFont( UiBuilder.IconFont );

            if( VfxSpawn.IsActive ) {
                if( ImGui.Button( FontAwesomeIcon.Pause.ToIconString() ) ) VfxSpawn.Remove();
            }
            else {
                if( ImGui.Button( FontAwesomeIcon.Play.ToIconString() ) ) VfxSpawn.OnSelf( path, false );
            }
        }

        public override void PlayPopupItems( string path ) {
            ImGui.Separator();
            if( ImGui.Selectable( "Spawn" ) ) VfxSpawn.OnSelf( path, false );
        }
    }
}