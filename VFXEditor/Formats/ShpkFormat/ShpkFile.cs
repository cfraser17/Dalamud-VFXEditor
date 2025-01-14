using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using VfxEditor.FileManager;
using VfxEditor.Formats.ShpkFormat.Keys;
using VfxEditor.Formats.ShpkFormat.Materials;
using VfxEditor.Formats.ShpkFormat.Nodes;
using VfxEditor.Formats.ShpkFormat.Shaders;
using VfxEditor.Ui.Components;
using VfxEditor.Ui.Components.SplitViews;
using VfxEditor.Utils;
using static VfxEditor.Utils.ShaderUtils;

namespace VfxEditor.Formats.ShpkFormat {
    // Based on https://github.com/Ottermandias/Penumbra.GameData/blob/15ae65921468a2407ecdd068ca79947e596e24be/Files/ShpkFile.cs#L6
    // And other work by Ny

    public class ShpkFile : FileManagerFile {
        public const uint MaterialParamsConstantId = 0x64D12851u;
        public const uint TableSamplerId = 0x2005679Fu;

        private readonly uint Version;
        private readonly uint DxMagic;
        public DX DxVersion => GetDxVersion( DxMagic );

        private readonly List<ShpkShader> VertexShaders = new();
        private readonly List<ShpkShader> PixelShaders = new();

        public readonly List<ShpkMaterialParmeter> MaterialParameters = new();
        public readonly List<ShpkParameterInfo> Constants = new();
        public readonly List<ShpkParameterInfo> Samplers = new();
        public readonly List<ShpkParameterInfo> Resources = new();

        public readonly List<ShpkKey> SystemKeys = new();
        public readonly List<ShpkKey> SceneKeys = new();
        public readonly List<ShpkKey> MaterialKeys = new();
        public readonly List<ShpkKey> SubViewKeys = new();

        private readonly List<ShpkNode> Nodes = new();
        private readonly List<ShpkAlias> Aliases = new();

        private readonly CommandDropdown<ShpkShader> VertexView;
        private readonly CommandDropdown<ShpkShader> PixelView;
        private readonly CommandSplitView<ShpkMaterialParmeter> MaterialParameterView;
        private readonly CommandSplitView<ShpkParameterInfo> ConstantView;
        private readonly CommandSplitView<ShpkParameterInfo> SamplerView;
        private readonly CommandSplitView<ShpkParameterInfo> ResourceView;

        private readonly CommandSplitView<ShpkKey> SystemKeyView;
        private readonly CommandSplitView<ShpkKey> SceneKeyView;
        private readonly CommandSplitView<ShpkKey> MaterialKeyView;
        private readonly CommandSplitView<ShpkKey> SubViewKeyView;

        private readonly CommandDropdown<ShpkNode> NodeView;
        private readonly CommandSplitView<ShpkAlias> AliasView;

        public ShpkFile( BinaryReader reader, bool verify ) : this( reader, null, verify ) { }

        public ShpkFile( BinaryReader reader, CommandManager manager, bool verify ) : base( manager ) {
            reader.ReadInt32(); // Magic
            Version = reader.ReadUInt32();
            DxMagic = reader.ReadUInt32();

            reader.ReadInt32(); // File length
            var shaderOffset = reader.ReadUInt32();
            var parameterOffset = reader.ReadUInt32();

            var numVertex = reader.ReadUInt32();
            var numPixel = reader.ReadUInt32();

            reader.ReadUInt32(); // Material parameters size
            var numMaterialParams = reader.ReadUInt32();

            var numConstants = reader.ReadUInt32();
            var numSamplers = reader.ReadUInt32();
            var numResources = reader.ReadUInt32();

            var numSystemKey = reader.ReadUInt32();
            var numSceneKey = reader.ReadUInt32();
            var numMaterialKey = reader.ReadUInt32();

            var numNode = reader.ReadUInt32();
            var numAlias = reader.ReadUInt32();

            for( var i = 0; i < numVertex; i++ ) VertexShaders.Add( new( reader, ShaderStage.Vertex, DxVersion, true, ShaderFileType.Shpk ) );
            for( var i = 0; i < numPixel; i++ ) PixelShaders.Add( new( reader, ShaderStage.Pixel, DxVersion, true, ShaderFileType.Shpk ) );

            for( var i = 0; i < numMaterialParams; i++ ) MaterialParameters.Add( new( reader ) );

            for( var i = 0; i < numConstants; i++ ) Constants.Add( new( reader, ShaderFileType.Shpk ) );
            for( var i = 0; i < numSamplers; i++ ) Samplers.Add( new( reader, ShaderFileType.Shpk ) );
            for( var i = 0; i < numResources; i++ ) Resources.Add( new( reader, ShaderFileType.Shpk ) );

            for( var i = 0; i < numSystemKey; i++ ) SystemKeys.Add( new( reader ) );
            for( var i = 0; i < numSceneKey; i++ ) SceneKeys.Add( new( reader ) );
            for( var i = 0; i < numMaterialKey; i++ ) MaterialKeys.Add( new( reader ) );

            SubViewKeys.Add( new( 1, reader.ReadUInt32() ) );
            SubViewKeys.Add( new( 2, reader.ReadUInt32() ) );

            for( var i = 0; i < numNode; i++ ) Nodes.Add( new( reader, SystemKeys.Count, SceneKeys.Count, MaterialKeys.Count, SubViewKeys.Count ) );
            for( var i = 0; i < numAlias; i++ ) Aliases.Add( new( reader ) );

            // ======= POPULATE ==========

            VertexShaders.ForEach( x => x.Read( reader, parameterOffset, shaderOffset ) );
            PixelShaders.ForEach( x => x.Read( reader, parameterOffset, shaderOffset ) );
            Constants.ForEach( x => x.Read( reader, parameterOffset ) );
            Samplers.ForEach( x => x.Read( reader, parameterOffset ) );
            Resources.ForEach( x => x.Read( reader, parameterOffset ) );

            // ====== CONSTRUCT VIEWS ==========

            VertexView = new( "Vertex Shader", VertexShaders, null, () => new( ShaderStage.Vertex, DxVersion, true, ShaderFileType.Shpk ) );
            PixelView = new( "Pixel Shader", PixelShaders, null, () => new( ShaderStage.Vertex, DxVersion, true, ShaderFileType.Shpk ) );

            MaterialParameterView = new( "Parameter", MaterialParameters, false, null, () => new() );

            ConstantView = new( "Constant", Constants, false, ( ShpkParameterInfo item, int idx ) => item.GetText(), () => new( ShaderFileType.Shpk ) );
            SamplerView = new( "Sampler", Samplers, false, ( ShpkParameterInfo item, int idx ) => item.GetText(), () => new( ShaderFileType.Shpk ) );
            ResourceView = new( "Resource", Resources, false, ( ShpkParameterInfo item, int idx ) => item.GetText(), () => new( ShaderFileType.Shpk ) );

            SystemKeyView = new( "System Key", SystemKeys, false, ( ShpkKey item, int idx ) => item.GetText( idx ), () => new() );
            SceneKeyView = new( "Scene Key", SceneKeys, false, ( ShpkKey item, int idx ) => item.GetText( idx ), () => new() );
            MaterialKeyView = new( "Material Key", MaterialKeys, false, ( ShpkKey item, int idx ) => item.GetText( idx ), () => new() );
            SubViewKeyView = new( "Sub-View Key", SubViewKeys, false, ( ShpkKey item, int idx ) => item.GetText( idx ), () => new() );

            NodeView = new( "Node", Nodes, null, () => new() );
            AliasView = new( "Alias", Aliases, false, null, () => new() );

            // TODO: don't be dumb when adding keys, actually update selectors and stuff
            // TOOD: when adding keys, make sure to do it everywhere

            if( verify ) Verified = FileUtils.Verify( reader, ToBytes(), null );
        }

        public override void Write( BinaryWriter writer ) {
            writer.Write( 0x6B506853u ); // Magic
            writer.Write( Version );
            writer.Write( DxMagic );

            var placeholderPos = writer.BaseStream.Position;
            writer.Write( 0 ); // size
            writer.Write( 0 ); // shader offset
            writer.Write( 0 ); // parameter offset

            writer.Write( VertexShaders.Count );
            writer.Write( PixelShaders.Count );

            var materialParamSize = ( Constants.FirstOrDefault( x => x.Id == MaterialParamsConstantId )?.DataSize ?? 0u );
            foreach( var param in MaterialParameters ) {
                materialParamSize = ( uint )Math.Max( materialParamSize, ( uint )param.Offset.Value + param.Size.Value );
            }
            materialParamSize = ( materialParamSize + 0xFu ) & ~0xFu;
            writer.Write( materialParamSize );
            writer.Write( MaterialParameters.Count );

            writer.Write( Constants.Count );
            writer.Write( Samplers.Count );
            writer.Write( Resources.Count );

            writer.Write( SystemKeys.Count );
            writer.Write( SceneKeys.Count );
            writer.Write( MaterialKeys.Count );

            writer.Write( Nodes.Count );
            writer.Write( Aliases.Count );

            var stringPositions = new List<(long, string)>();
            var shaderPositions = new List<(long, ShpkShader)>();

            VertexShaders.ForEach( x => x.Write( writer, stringPositions, shaderPositions ) );
            PixelShaders.ForEach( x => x.Write( writer, stringPositions, shaderPositions ) );

            MaterialParameters.ForEach( x => x.Write( writer ) );

            Constants.ForEach( x => x.Write( writer, stringPositions ) );
            Samplers.ForEach( x => x.Write( writer, stringPositions ) );
            Resources.ForEach( x => x.Write( writer, stringPositions ) );

            SystemKeys.ForEach( x => x.Write( writer ) );
            SceneKeys.ForEach( x => x.Write( writer ) );
            MaterialKeys.ForEach( x => x.Write( writer ) );

            SubViewKeys.ForEach( x => writer.Write( x.DefaultValue.Value ) );

            Nodes.ForEach( x => x.Write( writer ) );
            Aliases.ForEach( x => x.Write( writer ) );

            WriteOffsets( writer, placeholderPos, stringPositions, shaderPositions );
        }

        public override void Draw() {
            ImGui.Separator();
            ImGui.TextDisabled( $"Version: {Version} DirectX: {DxVersion}" );

            using var tabBar = ImRaii.TabBar( "Tabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton );
            if( !tabBar ) return;

            using( var tab = ImRaii.TabItem( "Vertex Shaders" ) ) {
                if( tab ) VertexView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Pixel Shaders" ) ) {
                if( tab ) PixelView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Material Parameters" ) ) {
                if( tab ) {
                    DrawMaterialTable();
                    ImGui.Separator();
                    MaterialParameterView.Draw();
                }
            }

            using( var tab = ImRaii.TabItem( "Constants" ) ) {
                if( tab ) ConstantView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Samplers" ) ) {
                if( tab ) SamplerView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Resources" ) ) {
                if( tab ) ResourceView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Keys" ) ) {
                if( tab ) DrawKeys();
            }

            using( var tab = ImRaii.TabItem( "Nodes" ) ) {
                if( tab ) NodeView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Aliases" ) ) {
                if( tab ) AliasView.Draw();
            }
        }

        private void DrawKeys() {
            using var _ = ImRaii.PushId( "Keys" );

            ImGui.SetCursorPosY( ImGui.GetCursorPosY() + 2 );

            using var tabBar = ImRaii.TabBar( "Tabs", ImGuiTabBarFlags.NoCloseWithMiddleMouseButton );
            if( !tabBar ) return;

            using( var tab = ImRaii.TabItem( "System" ) ) {
                if( tab ) SystemKeyView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Scene" ) ) {
                if( tab ) SceneKeyView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Material" ) ) {
                if( tab ) MaterialKeyView.Draw();
            }

            using( var tab = ImRaii.TabItem( "Sub-View" ) ) {
                if( tab ) SubViewKeyView.Draw();
            }
        }

        private void DrawMaterialTable() {
            using var _ = ImRaii.PushId( "MaterialParameters" );

            ImGui.Dummy( Vector2.One );
            using var table = ImRaii.Table( "Table", 5, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY, new( ImGui.GetContentRegionAvail().X, 200 ) );
            if( !table ) return;

            using( var font = ImRaii.PushFont( UiBuilder.MonoFont ) ) {
                ImGui.TableSetupScrollFreeze( 0, 1 );
                ImGui.TableSetupColumn( string.Empty, ImGuiTableColumnFlags.WidthFixed, 50 );
                ImGui.TableSetupColumn( "x", ImGuiTableColumnFlags.WidthStretch );
                ImGui.TableSetupColumn( "y", ImGuiTableColumnFlags.WidthStretch );
                ImGui.TableSetupColumn( "z", ImGuiTableColumnFlags.WidthStretch );
                ImGui.TableSetupColumn( "w", ImGuiTableColumnFlags.WidthStretch );
                ImGui.TableHeadersRow();
            }

            var rows = MaterialParameters.Count == 0 ? 0 : ( int )MaterialParameters.Select( x => Math.Ceiling( ( float )x.EndSlot / 4 ) ).Max();

            for( var i = 0; i < rows; i++ ) {
                ImGui.TableNextColumn();

                using( var font = ImRaii.PushFont( UiBuilder.MonoFont ) ) {
                    ImGui.TableHeader( $" [{i}]" );
                    UiUtils.Tooltip( $"g_MaterialParameter[{i}]" );
                }

                for( var j = 0; j < 4; j++ ) {
                    var slot = ( 4 * i ) + j;
                    var parameters = MaterialParameters.FindAll( x => slot >= x.StartSlot && slot < x.EndSlot );
                    var parameter = parameters.FirstOrDefault();

                    ImGui.TableNextColumn();

                    using var disabled = ImRaii.Disabled( parameter == null || slot != parameter.StartSlot );
                    using var none = ImRaii.PushColor( ImGuiCol.Text, UiUtils.RED_COLOR, parameter == null );
                    using var selected = ImRaii.PushColor( ImGuiCol.Text, UiUtils.PARSED_GREEN, parameter != null && parameter == MaterialParameterView.GetSelected() );
                    using var multiple = ImRaii.PushColor( ImGuiCol.Text, UiUtils.DALAMUD_ORANGE, parameters.Count > 1 );

                    if( ImGui.Selectable( parameter == null ? "[NONE]" : $"Parameter {MaterialParameters.IndexOf( parameter )}" ) && parameter != null ) {
                        MaterialParameterView.SetSelected( parameter );
                    }
                }
            }
        }
    }
}
