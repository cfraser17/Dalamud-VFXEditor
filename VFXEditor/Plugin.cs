using System;
using System.IO;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using ImGuiNET;
using ImPlotNET;
using ImGuizmoNET;
using VFXEditor.Data;
using VFXEditor.Data.DirectX;
using VFXEditor.External;
using VFXEditor.Data.Vfx;
using System.Reflection;
using VFXEditor.Data.Texture;
using VFXSelect;
using FDialog;

namespace VFXEditor
{
    public partial class Plugin : IDalamudPlugin {
        public string Name => "VFXEditor";
        private const string CommandName = "/vfxedit";
        private bool PluginReady => PluginInterface.Framework.Gui.GetBaseUIObject() != IntPtr.Zero;

        public DalamudPluginInterface PluginInterface;
        public Configuration Configuration;
        public ResourceLoader ResourceLoader;
        public DocumentManager DocManager;
        public DirectXManager DXManager;
        public VfxTracker Tracker;
        public TextureManager TexManager;
        public SheetManager Sheets;

        public static string TemplateLocation;

        public string WriteLocation => Configuration?.WriteLocation;

        public string AssemblyLocation { get; set; } = Assembly.GetExecutingAssembly().Location;

        private IntPtr ImPlotContext;

        private FileDialog _F;

        public void Initialize( DalamudPluginInterface pluginInterface ) {
            PluginInterface = pluginInterface;
            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize( PluginInterface );
            Directory.CreateDirectory( WriteLocation ); // create if it doesn't already exist
            PluginLog.Log( "Write location: " + WriteLocation );

            ResourceLoader = new ResourceLoader( this );
            PluginInterface.CommandManager.AddHandler( CommandName, new CommandInfo( OnCommand ) {
                HelpMessage = "toggle ui"
            } );

            TemplateLocation = Path.GetDirectoryName( AssemblyLocation );

            // ==== IMGUI ====
            ImPlot.SetImGuiContext( ImGui.GetCurrentContext() );
            ImPlotContext = ImPlot.CreateContext();
            ImPlot.SetCurrentContext( ImPlotContext );

            Tracker = new VfxTracker( this );
            TexManager = new TextureManager( this );
            TexManager.OneTimeSetup();
            Sheets = new SheetManager( PluginInterface, Path.Combine( TemplateLocation, "Files", "npc.csv" ) );
            DocManager = new DocumentManager( this );
            DXManager = new DirectXManager( this );

            InitUI();

            ResourceLoader.Init();
            ResourceLoader.Enable();

            _F = new FileDialog( "TestKey", "Choose a file", ".atex", @"C:\Users\kamin\AppData\Roaming\XIVLauncher\devPlugins", "test", "atex", 1, false, ImGuiFileDialogFlags.None );
            _F.Show();

            PluginInterface.UiBuilder.OnBuildUi += Draw;
        }

        public void Draw() {
            if( !PluginReady ) return;

            _F.Draw();

            DrawUI();
            Tracker.Draw();
        }

        public void Dispose() {
            PluginInterface.UiBuilder.OnBuildUi -= Draw;
            ResourceLoader?.Dispose();

            ImPlot.DestroyContext();

            PluginInterface.CommandManager.RemoveHandler( CommandName );
            PluginInterface?.Dispose();
            SpawnVfx?.Remove();
            DXManager?.Dispose();
            DocManager?.Dispose();
            TexManager?.Dispose();
        }

        private void OnCommand( string command, string rawArgs ) {
            Visible = !Visible;
        }
    }
}