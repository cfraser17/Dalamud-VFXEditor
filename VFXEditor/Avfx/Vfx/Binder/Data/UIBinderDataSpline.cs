using VFXEditor.AVFXLib.Binder;

namespace VFXEditor.AVFX.VFX {
    public class UIBinderDataSpline : UIData {
        public UIBinderDataSpline( AVFXBinderDataSpline data ) {
            Tabs.Add( new UICurve( data.CarryOverFactor, "Carry Over Factor" ) );
            Tabs.Add( new UICurve( data.CarryOverFactorRandom, "Carry Over Factor Random" ) );
        }
    }
}
