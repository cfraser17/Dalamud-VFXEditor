using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace VfxEditor.Select.Rows {
    public class XivHousingSelected {
        public XivHousing Housing;

        public HashSet<string> VfxPaths = new();

        public XivHousingSelected( XivHousing housing, Lumina.Data.FileResource file ) {
            Housing = housing;

            var data = file.Data;
            var stringData = Encoding.UTF8.GetString( data );
            var matches = SheetManager.AvfxRegex.Matches( stringData );
            foreach( Match m in matches ) {
                VfxPaths.Add( m.Value.Trim( '\u0000' ) );
            }
        }
    }
}
