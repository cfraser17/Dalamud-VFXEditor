using System.IO;
using System.Linq;
using VFXEditor.AVFXLib;
using VFXEditor.AVFXLib.Timeline;

namespace VFXEditor.AVFX.VFX {
    public class UITimelineView : UINodeDropdownView<UITimeline> {
        public UITimelineView( AVFXFile vfxFile, AVFXMain avfx, UINodeGroup<UITimeline> group ) : base( vfxFile, avfx, group, "Timeline", true, true, "timeline_default.vfxedit" ) {
        }

        public override void OnDelete( UITimeline item ) => AVFX.RemoveTimeline( item.Timeline );

        public override void OnExport( BinaryWriter writer, UITimeline item ) => item.Write( writer );

        public override UITimeline OnImport( BinaryReader reader, int size, bool has_dependencies = false ) {
            var item = new AVFXTimeline();
            item.Read( reader, size );
            AVFX.AddTimeline( item );
            return new UITimeline( item, VfxFile.NodeGroupSet, has_dependencies );
        }
    }
}