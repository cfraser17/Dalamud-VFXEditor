using Lumina.Excel.GeneratedSheets;
using System.Linq;
using VfxEditor.Select.Rows;

namespace VfxEditor.Select.Sheets {
    public class StatusSheetLoader : SheetLoader<XivStatus, XivStatus> {
        public override void OnLoad() {
            var sheet = Plugin.DataManager.GetExcelSheet<Status>().Where( x => !string.IsNullOrEmpty( x.Name ) );
            foreach( var item in sheet ) {
                var status = new XivStatus( item );
                if( status.VfxExists ) Items.Add( status );
            }
        }

        public override bool SelectItem( XivStatus item, out XivStatus selectedItem ) {
            selectedItem = item;
            return true;
        }
    }
}
