using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;
using VfxEditor.Select.Rows;

namespace VfxEditor.Select.Sheets {
    public class BgmSheetLoader : SheetLoader<XivBgm, XivBgm> {
        public override void OnLoad() {
            var sheet = Plugin.DataManager.GetExcelSheet<BGM>().Where( x => !string.IsNullOrEmpty( x.File ) );
            foreach( var item in sheet ) Items.Add( new XivBgm( item ) );
        }

        public override bool SelectItem( XivBgm item, out XivBgm selectedItem ) {
            selectedItem = item;
            return true;
        }
    }
}
