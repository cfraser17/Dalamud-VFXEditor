using Dalamud.Interface;
using ImGuiNET;
using System.Collections.Generic;
using VfxEditor.Utils;

namespace VfxEditor.AvfxFormat.Vfx {
    public abstract class UiWorkspaceItem : UiItem {
#nullable enable
        public string? Renamed;
#nullable disable
        private string RenamedTemp;
        private bool CurrentlyRenaming = false;

        public override string GetText() => string.IsNullOrEmpty( Renamed ) ? GetDefaultText() : Renamed;

        public abstract string GetWorkspaceId();

        public void PopulateWorkspaceMeta( Dictionary<string, string> RenameDict ) {
            if( !string.IsNullOrEmpty( Renamed ) ) {
                RenameDict[GetWorkspaceId()] = Renamed;
            }
            PopulateWorkspaceMetaChildren( RenameDict );
        }

        public virtual void PopulateWorkspaceMetaChildren( Dictionary<string, string> RenameDict ) { }

        public void ReadWorkspaceMeta( Dictionary<string, string> RenameDict ) {
            if( RenameDict.TryGetValue( GetWorkspaceId(), out var renamed ) ) {
                Renamed = renamed;
            }
            ReadWorkspaceMetaChildren( RenameDict );
        }

        public virtual void ReadWorkspaceMetaChildren( Dictionary<string, string> RenameDict ) { }

        public void DrawRename( string parentId ) {
            var id = parentId + "/Rename";
            if( CurrentlyRenaming ) {
                ImGui.InputText( $"{id}-Input", ref RenamedTemp, 255 );

                ImGui.PushFont( UiBuilder.IconFont );

                ImGui.SameLine();
                ImGui.SetCursorPosX( ImGui.GetCursorPosX() - 5 );
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Check}" + id ) ) {
                    if( string.IsNullOrEmpty( RenamedTemp ) || Renamed == GetDefaultText() ) {
                        Renamed = null;
                    }
                    else {
                        Renamed = RenamedTemp;
                    }
                    CurrentlyRenaming = false;
                }

                ImGui.SameLine();
                ImGui.SetCursorPosX( ImGui.GetCursorPosX() - 5 );
                if( UiUtils.RemoveButton( $"{( char )FontAwesomeIcon.Times}" + id ) ) {
                    CurrentlyRenaming = false;
                }

                ImGui.PopFont();

                ImGui.SameLine();
                ImGui.SetCursorPosX( ImGui.GetCursorPosX() - 5 );
                if( ImGui.Button( "Reset" + id ) ) {
                    Renamed = null;
                    CurrentlyRenaming = false;
                }
            }
            else {
                var currentText = string.IsNullOrEmpty( Renamed ) ? GetDefaultText() : Renamed;
                ImGui.PushStyleVar( ImGuiStyleVar.Alpha, 0.8f );
                ImGui.InputText( $"{id}-Input", ref currentText, 255, ImGuiInputTextFlags.ReadOnly );
                ImGui.PopStyleVar();

                ImGui.PushFont( UiBuilder.IconFont );

                ImGui.SameLine();
                ImGui.SetCursorPosX( ImGui.GetCursorPosX() - 5 );
                if( ImGui.Button( $"{( char )FontAwesomeIcon.PencilAlt}" + id ) ) {
                    CurrentlyRenaming = true;
                    RenamedTemp = currentText;
                }

                ImGui.PopFont();
            }
        }
    }
}