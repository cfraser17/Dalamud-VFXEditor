using System.IO;
using System.Linq;
using VFXEditor.AVFXLib;
using VFXEditor.AVFXLib.Binder;

namespace VFXEditor.AVFX.VFX {
    public class UIBinderView : UINodeDropdownView<UIBinder> {
        public UIBinderView( AVFXFile vfxFile, AVFXMain avfx, UINodeGroup<UIBinder> group ) : base( vfxFile, avfx, group, "Binder", true, true, "binder_default.vfxedit" ) {
        }

        public override void OnDelete( UIBinder item ) => AVFX.RemoveBinder( item.Binder );

        public override void OnExport( BinaryWriter writer, UIBinder item ) => item.Write( writer );

        public override UIBinder OnImport( BinaryReader reader, int size, bool has_dependencies = false ) {
            var item = new AVFXBinder();
            item.Read( reader, size );
            AVFX.AddBinder( item );
            return new UIBinder( item );
        }
    }
}