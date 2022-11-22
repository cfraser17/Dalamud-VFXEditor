using ImGuiNET;
using VfxEditor.Select.Rows;

namespace VfxEditor.Select.VfxSelect {
    public class VfxActionSelect : VfxSelectTab<XivActionBase, XivActionSelected> {
        private ImGuiScene.TextureWrap Icon;

        public VfxActionSelect( string parentId, string tabId, VfxSelectDialog dialog, bool nonPlayer = false ) :
            base( parentId, tabId, !nonPlayer ? SheetManager.Actions : SheetManager.NonPlayerActions, dialog ) {
        }

        protected override bool CheckMatch( XivActionBase item, string searchInput ) => Matches( item.Name, searchInput );

        protected override void OnSelect() {
            LoadIcon( Selected.Icon, ref Icon );
        }

        protected override void DrawSelected( XivActionSelected loadedItem ) {
            if( loadedItem == null ) { return; }
            ImGui.Text( loadedItem.Action.Name );
            ImGui.SetCursorPosY( ImGui.GetCursorPosY() + 5 );

            DrawIcon( Icon );

            DrawPath( "Cast VFX Path", loadedItem.CastVfxPath, Id + "Cast", Dialog, SelectResultType.GameAction, "ACTION", loadedItem.Action.Name + " Cast", play: true );

            if( loadedItem.SelfVfxExists ) {
                ImGui.Text( "TMB Path: " );
                ImGui.SameLine();
                DisplayPath( loadedItem.SelfTmbPath );
                Copy( loadedItem.SelfTmbPath, id: Id + "CopyTmb" );

                DrawPath( "VFX", loadedItem.SelfVfxPaths, Id, Dialog, SelectResultType.GameAction, "ACTION", loadedItem.Action.Name, spawn: true );
            }
        }

        protected override string UniqueRowTitle( XivActionBase item ) => $"{item.Name}##{item.RowId}";
    }
}
