using System.IO;
using System.Linq;

namespace VfxEditor.Select.Tabs.Common {
    public class CommonTabUld : SelectTab<CommonRow> {
        public CommonTabUld( SelectDialog dialog, string name ) : base( dialog, name, "Common-Uld", SelectResultType.GameUi ) { }

        // ===== LOADING =====

        public override void LoadData() {
            var idx = 0;
            foreach( var line in File.ReadLines( SelectDataUtils.CommonUldPath ).Where( x => !string.IsNullOrEmpty( x ) ) ) {
                Items.Add( new CommonRow( idx++, line, line.Replace( ".uld", "" ).Replace( "ui/uld/", "" ), 0 ) );
            }
        }

        // ===== DRAWING ======

        protected override void DrawSelected() {
            DrawPaths( Selected.Path, Selected.Name );
        }

        protected override string GetName( CommonRow item ) => item.Name;
    }
}