using System.IO;
using System.Linq;
using VFXEditor.AVFXLib;
using VFXEditor.AVFXLib.Model;

namespace VFXEditor.AVFX.VFX {
    public class UIModelView : UINodeSplitView<UIModel> {
        public UIModelView( AVFXFile vfxFile, AVFXMain avfx, UINodeGroup<UIModel> group ) : base( vfxFile, avfx, group, "Model", true, true ) {
        }

        public override void OnSelect( UIModel item ) => Plugin.DirectXManager.ModelView.LoadModel( item.Model );

        public override void OnDelete( UIModel item ) => Avfx.RemoveModel( item.Model );

        public override UIModel OnNew() => new UIModel( Avfx.AddModel() );

        public override UIModel OnImport( BinaryReader reader, int size, bool has_dependencies = false ) {
            var mdl = new AVFXModel();
            mdl.Read( reader, size );
            Avfx.AddModel( mdl );
            return new UIModel( mdl );
        }
    }
}