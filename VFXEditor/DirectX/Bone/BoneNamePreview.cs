using Dalamud.Interface.Utility.Raii;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Animations;
using ImGuiNET;
using SharpDX;
using SharpDX.Direct3D11;
using System.Collections.Generic;
using System.Linq;
using VfxEditor.DirectX.Drawable;
using VfxEditor.FileManager;
using VfxEditor.SklbFormat;
using VfxEditor.SklbFormat.Bones;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using Vec2 = System.Numerics.Vector2;

namespace VfxEditor.DirectX {
    public class BoneNamePreview : BonePreview {
        private bool IsSklb = false;
        private List<Bone> BoneList;
        private int NumWireframe = 0;
        private Buffer WireframeVertices;

        private static readonly ClosenessComparator Comparator = new();

        public BoneNamePreview( Device device, DeviceContext ctx, string shaderPath ) : base( device, ctx, shaderPath ) { }

        public void LoadSkeleton( int renderId, FileManagerFile file, List<Bone> boneList, BoneSkinnedMeshGeometry3D mesh ) {
            CurrentRenderId = renderId;
            IsSklb = file is SklbFile;
            BoneList = boneList;
            LoadSkeleton( mesh );
        }

        public void LoadWireframe( MeshGeometry3D collision, MeshGeometry3D simulation, MeshGeometry3D spring ) {
            PaintColor( collision, new( 0, 1, 0, 1 ) );
            PaintColor( simulation, new( 0, 0, 1, 1 ) );
            PaintColor( spring, new( 1, 0, 0, 1 ) );

            var meshes = new List<MeshGeometry3D>() {
                collision,
                simulation,
                spring
            };

            if( meshes.Select( x => x.Positions.Count ).Sum() == 0 ) {
                NumWireframe = 0;
                WireframeVertices?.Dispose();
                UpdateDraw();
                return;
            }

            var data = GetData( meshes );

            WireframeVertices?.Dispose();
            WireframeVertices = Buffer.Create( Device, BindFlags.VertexBuffer, data );
            NumWireframe = meshes.Select( x => x.Indices.Count ).Sum();
            UpdateDraw();

            Dalamud.Log( $"Loaded wireframe {NumWireframe}" );
        }

        public void LoadEmpty( int renderId, FileManagerFile file ) {
            CurrentRenderId = renderId;
            IsSklb = file is SklbFile;
            NumWireframe = 0;
            BoneList = new();
            WireframeVertices?.Dispose();
            Model.ClearVertexes();
            UpdateDraw();
        }

        protected override void DrawPasses() {
            if( Model.ShaderError ) return;
            if( Model.Count == 0 && NumWireframe == 0 ) return;

            Model.SetupPass( Ctx, PassType.Final );
            Model.SetConstantBuffers( Ctx, VertexShaderBuffer, PixelShaderBuffer );

            if( Model.Count > 0 ) {
                Ctx.InputAssembler.SetVertexBuffers( 0, new VertexBufferBinding( Model.Data, Utilities.SizeOf<Vector4>() * Model.Span, 0 ) );
                Ctx.Draw( Model.Count, 0 );
                Ctx.Flush();
            }

            Ctx.Flush();

            if( NumWireframe > 0 ) {
                // Kind of jank, but oh well
                var wireframe = new RasterizerState( Device, new RasterizerStateDescription {
                    CullMode = CullMode.None,
                    DepthBias = 0,
                    DepthBiasClamp = 0,
                    FillMode = FillMode.Wireframe,
                    IsAntialiasedLineEnabled = false,
                    IsDepthClipEnabled = true,
                    IsFrontCounterClockwise = false,
                    IsMultisampleEnabled = true,
                    IsScissorEnabled = false,
                    SlopeScaledDepthBias = 0
                } );

                Ctx.Rasterizer.State = wireframe;

                Ctx.InputAssembler.SetVertexBuffers( 0, new VertexBufferBinding( WireframeVertices, Utilities.SizeOf<Vector4>() * 3, 0 ) );
                Ctx.Draw( NumWireframe, 0 );
                Ctx.Flush();

                Ctx.Rasterizer.State = RasterizeState;
                wireframe.Dispose();
            }

            Ctx.GeometryShader.Set( null );
        }

        public override void Dispose() {
            base.Dispose();
            WireframeVertices?.Dispose();
        }

        protected virtual void DrawInlineExtra() { }

        public override void DrawInline() {
            var viewProj = Matrix.Multiply( ViewMatrix, ProjMatrix );
            var worldViewProj = LocalMatrix * viewProj;

            using var child = ImRaii.Child( "3DChild" );

            var drawList = ImGui.GetWindowDrawList();

            DrawImage();

            var boneScreenMap = new Dictionary<string, Vec2>();
            var boneDepthMap = new Dictionary<string, float>();

            var boneScreenList = new List<Vec2>();
            var boneDepthList = new List<float>();

            foreach( var bone in BoneList ) {
                var matrix = bone.BindPose * worldViewProj;

                var pos = Vector3.Transform( new Vector3( 0 ), matrix ).ToVector3();
                var screenPos = LastMid + ( ( LastSize / 2f ) * new Vec2( pos.X, -1f * pos.Y ) );
                var depth = pos.Z;

                boneScreenMap[bone.Name] = screenPos;
                boneDepthMap[bone.Name] = depth;

                boneScreenList.Add( screenPos );
                boneDepthList.Add( depth );
            }

            // ===== CONNECTION LINES =======

            if( CurrentRenderId != -1 && IsSklb && Plugin.Configuration.SklbBoneDisplay != BoneDisplay.Connected ) {
                foreach( var bone in BoneList ) {
                    if( bone.ParentIndex == -1 ) continue;
                    if( !ValidDepth( boneDepthMap[bone.Name] ) || !ValidDepth( boneDepthList[bone.ParentIndex] ) ) continue;

                    var startPos = boneScreenMap[bone.Name];
                    var endPos = boneScreenList[bone.ParentIndex];

                    drawList.AddLine( startPos, endPos, ImGui.ColorConvertFloat4ToU32( Plugin.Configuration.SkeletonBoneLineColor ), 2f );
                }
            }

            // ===== NAMES =======

            if( Plugin.Configuration.ShowBoneNames ) {
                var groups = boneScreenMap.GroupBy( entry => entry.Value, entry => entry.Key, Comparator );
                foreach( var group in groups ) {
                    var idx = 0;
                    foreach( var item in group ) {
                        drawList.AddText( group.Key + new Vec2( 0, 12f * idx ), ImGui.ColorConvertFloat4ToU32( Plugin.Configuration.SkeletonBoneNameColor ), item );
                        idx++;
                    }
                }
            }

            DrawInlineExtra();
        }

        private static bool ValidDepth( float depth ) => depth > 0.5f && depth < 1f;

        private class ClosenessComparator : IEqualityComparer<Vec2> {
            public bool Equals( Vec2 x, Vec2 y ) => ( x - y ).Length() < 10f;
            public int GetHashCode( Vec2 obj ) => 0;
        }
    }
}
