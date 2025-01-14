using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using VfxEditor.DirectX;
using VfxEditor.Ui.Interfaces;
using static VfxEditor.AvfxFormat.Enums;

namespace VfxEditor.AvfxFormat {
    public enum CurveType {
        Color,
        Angle,
        Base
    }

    public partial class AvfxCurve : AvfxOptional {
        public readonly int RenderId = Renderer.NewId;

        private static int EDITOR_ID = 0;
        private readonly CurveType Type;
        private readonly int Id;

        public readonly AvfxEnum<CurveBehavior> PreBehavior = new( "Pre Behavior", "BvPr" );
        public readonly AvfxEnum<CurveBehavior> PostBehavior = new( "Post Behavior", "BvPo" );
        public readonly AvfxEnum<RandomType> Random = new( "RandomType", "RanT" );
        public readonly AvfxCurveKeys KeyList;
        public List<AvfxCurveKey> Keys => KeyList.Keys;

        private readonly List<AvfxBase> Parsed;
        private readonly List<IUiItem> Display;

        private readonly string Name;
        private readonly bool Locked;

        public AvfxCurve( string name, string avfxName, CurveType type = CurveType.Base, bool locked = false ) : base( avfxName ) {
            Name = name;
            Type = type;
            Locked = locked;

            Id = EDITOR_ID++;
            KeyList = new( this );

            Parsed = new() {
                PreBehavior,
                PostBehavior,
                Random,
                KeyList
            };

            Display = new() {
                PreBehavior,
                PostBehavior,
            };
            if( type != CurveType.Color ) Display.Add( Random );
        }

        public override void ReadContents( BinaryReader reader, int size ) => ReadNested( reader, Parsed, size );

        public override void WriteContents( BinaryWriter writer ) {
            WriteLeaf( writer, "KeyC", 4, KeyList.Keys.Count );
            if( Type == CurveType.Color ) Random.SetAssigned( false );
            WriteNested( writer, Parsed );
        }

        protected override IEnumerable<AvfxBase> GetChildren() {
            foreach( var item in Parsed ) yield return item;
        }

        public override void DrawUnassigned() {
            using var _ = ImRaii.PushId( Name );

            AssignedCopyPaste( Name );
            DrawAssignButton( Name, true );
        }

        public override void DrawAssigned() {
            using var _ = ImRaii.PushId( Name );

            AssignedCopyPaste( Name );
            if( !Locked && DrawUnassignButton( Name ) ) return;
            DrawItems( Display );
            DrawEditor();
        }

        public override string GetDefaultText() => Name;

        // ======== STATIC DRAW ==========

        public static void DrawUnassignedCurves( List<AvfxCurve> curves ) {
            var first = true;
            var currentX = 0f;
            var maxX = ImGui.GetContentRegionAvail().X;
            var imguiStyle = ImGui.GetStyle();
            var spacing = imguiStyle.ItemInnerSpacing.X;

            using var style = ImRaii.PushStyle( ImGuiStyleVar.ItemSpacing, new Vector2( spacing, imguiStyle.ItemSpacing.Y ) );

            foreach( var curve in curves.Where( x => !x.IsAssigned() ) ) {
                var width = ImGui.CalcTextSize( $"+ {curve.Name}" ).X + imguiStyle.FramePadding.X * 2 + spacing;
                if( first ) {
                    currentX += width;
                }
                else {
                    if( ( maxX - currentX - width ) > spacing + 4 ) {
                        currentX += width;
                        ImGui.SameLine();
                    }
                    else {
                        currentX = width;
                    }
                }

                curve.Draw();
                first = false;
            }
        }

        public static void DrawAssignedCurves( List<AvfxCurve> curves ) {
            using var tabBar = ImRaii.TabBar( "Tabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton );
            if( !tabBar ) return;

            foreach( var curve in curves.Where( x => x.IsAssigned() ) ) {
                if( ImGui.BeginTabItem( curve.Name ) ) {
                    curve.DrawAssigned();
                    ImGui.EndTabItem();
                }
            }
        }
    }
}
