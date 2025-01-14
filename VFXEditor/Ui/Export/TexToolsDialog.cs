using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using VfxEditor.FileBrowser;
using VfxEditor.FileManager.Interfaces;
using VfxEditor.Ui.Export.Categories;

namespace VfxEditor.Ui.Export {
    public struct TTMPL {
        public string TTMPVersion;
        public string Name;
        public string Author;
        public string Version;
#nullable enable
        public string? Description;
        public string? ModPackPages;
#nullable disable
        public TTMPL_Simple[] SimpleModsList;
    }

    public struct TTMPL_Simple {
        public string Name;
        public string Category;
        public string FullPath;
        public bool IsDefault;
        public int ModOffset;
        public int ModSize;
        public string DatFile;
#nullable enable
        public string? ModPackEntry;
#nullable disable
    }

    public class TexToolsDialog : ExportDialog {
        private readonly ExportDialogCategorySet ToExport = new();

        public TexToolsDialog() : base( "TexTools" ) { }

        protected override void OnExport() {
            FileBrowserManager.SaveFileDialog( "Select a Save Location", ".ttmp2,.*", ModName, "ttmp2", ( bool ok, string res ) => {
                if( !ok ) return;
                Export( res );
                Hide();
            } );
        }

        protected override void OnDraw() => ToExport.Draw();

        protected override void OnRemoveDocument( IFileDocument document ) => ToExport.RemoveDocument( document );

        protected override void OnReset() => ToExport.Reset();

        private void Export( string saveFile ) {
            try {
                var simpleParts = new List<TTMPL_Simple>();
                var modOffset = 0;

                using var ms = new MemoryStream();
                using var writer = new BinaryWriter( ms );

                foreach( var category in ToExport.Categories ) {
                    foreach( var item in category.GetItemsToExport() ) item.TextoolsExport( writer, simpleParts, ref modOffset );
                }

                var newData = ms.ToArray();

                var mod = new TTMPL {
                    TTMPVersion = "1.3s",
                    Name = ModName,
                    Author = Author,
                    Version = Version,
                    Description = null,
                    ModPackPages = null,
                    SimpleModsList = simpleParts.ToArray()
                };

                var saveDir = Path.GetDirectoryName( saveFile );
                var tempDir = Path.Combine( saveDir, "VFXEDITOR_TEXTOOLS_TEMP" );
                Directory.CreateDirectory( tempDir );

                var mdpPath = Path.Combine( tempDir, "TTMPD.mpd" );
                var mplPath = Path.Combine( tempDir, "TTMPL.mpl" );
                var mplString = JsonConvert.SerializeObject( mod );
                File.WriteAllText( mplPath, mplString );
                File.WriteAllBytes( mdpPath, newData );

                if( File.Exists( saveFile ) ) File.Delete( saveFile );
                ZipFile.CreateFromDirectory( tempDir, saveFile );
                Directory.Delete( tempDir, true );
                Dalamud.Log( $"Exported To: {saveFile}" );
            }
            catch( Exception e ) {
                Dalamud.Error( e, "Could not export to TexTools" );
            }
        }
    }
}
