using Dalamud.Interface.Internal;
using ImGuiNET;
using OtterGui.Raii;
using System;
using System.Numerics;

namespace VfxEditor.Formats.TextureFormat.Textures {
    public class TexturePreview : TextureDrawable {
        public readonly ushort Height;
        public readonly ushort Width;
        public readonly ushort MipLevels;
        public readonly ushort Depth;
        public readonly TextureFormat Format;
        public readonly IDalamudTextureWrap Wrap;

        public TexturePreview( TextureDataFile file, string gamePath ) : base( gamePath ) {
            Format = file.Header.Format;
            MipLevels = file.Header.MipLevels;
            Width = file.Header.Width;
            Height = file.Header.Height;
            Depth = file.Header.Depth;
            Wrap = Dalamud.PluginInterface.UiBuilder.LoadImageRaw( file.ImageData, file.Header.Width, file.Header.Height, 4 );
        }

        public override void DrawImage() {
            if( Wrap == null ) return;
            ImGui.Image( Wrap.ImGuiHandle, new Vector2( Width, Height ) );
        }

        public override void DrawImage( uint u, uint v, uint w, uint h ) {
            if( Wrap == null ) return;
            var size = new Vector2( Width, Height );
            var uv0 = new Vector2( u, v ) / size;
            var uv1 = uv0 + new Vector2( w, h ) / size;
            ImGui.Image( Wrap.ImGuiHandle, new Vector2( w, h ), uv0, uv1 );
        }

        public void DrawParams() => ImGui.TextDisabled( $"Format: {Format}  Mips: {MipLevels}  Size: {Width}x{Height}" );

        protected override void DrawControls() {
            DrawParams();

            using var style = ImRaii.PushStyle( ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing );
            DrawExportReplaceButtons();
        }

        protected override void OnReplace( string importPath ) => Plugin.TextureManager.ReplaceTexture( importPath, GamePath );

        protected override TextureDataFile GetRawData() => Dalamud.DataManager.GetFile<TextureDataFile>( GamePath );

        public void Dispose() {
            if( Wrap?.ImGuiHandle == null ) return;

            if( Plugin.State == WorkspaceState.Loading ) Plugin.OnMainThread += Cleanup;
            else Cleanup();
        }

        public void Cleanup() {
            try {
                Wrap?.Dispose();
            }
            catch( Exception ) { }
        }
    }
}