using ImGuiNET;
using NAudio.Wave;
using System;
using Dalamud.Logging;
using Dalamud.Interface;
using ImGuiFileDialog;
using System.IO;
using System.Numerics;
using VfxEditor.Utils;
using System.Threading.Tasks;

namespace VfxEditor.ScdFormat {
    public class AudioPlayer {
        private readonly ScdAudioEntry Entry;
        private PlaybackState State => CurrentOutput == null ? PlaybackState.Stopped : CurrentOutput.PlaybackState;
        private PlaybackState PrevState = PlaybackState.Stopped;

        private WaveStream CurrentStream;
        private WaveChannel32 CurrentChannel;
        private WasapiOut CurrentOutput;

        private double TotalTime => CurrentStream == null ? 0 : CurrentStream.TotalTime.TotalSeconds - 0.01f;
        private double CurrentTime => CurrentStream == null ? 0 : CurrentStream.CurrentTime.TotalSeconds;

        private bool IsVorbis => Entry.Format == SscfWaveFormat.Vorbis;

        private int ConverterSamplesOut = 0;
        private int ConverterSecondsOut = 0;
        private int ConverterSamples = 0;
        private float ConverterSeconds = 0f;
        private bool ConverterOpen = false;

        private bool LoopTimeInitialized = false;
        private bool LoopTimeRefreshing = false;
        private double LoopStartTime = 0;
        private double LoopEndTime = 0;

        private double QueueSeek = -1;

        public AudioPlayer( ScdAudioEntry entry ) {
            Entry = entry;
        }

        public void Draw( string id, int idx ) {
            if( ImGui.CollapsingHeader( $"Index {idx}{id}", ImGuiTreeNodeFlags.DefaultOpen ) ) {
                ImGui.Indent();

                // Controls
                ImGui.PushFont( UiBuilder.IconFont );
                if( State == PlaybackState.Stopped ) {
                    if( ImGui.Button( $"{( char )FontAwesomeIcon.Play}" + id ) ) Play();
                }
                else if( State == PlaybackState.Playing ) {
                    if( ImGui.Button( $"{( char )FontAwesomeIcon.Pause}" + id ) ) CurrentOutput.Pause();
                }
                else if( State == PlaybackState.Paused ) {
                    if( ImGui.Button( $"{( char )FontAwesomeIcon.Play}" + id ) ) CurrentOutput.Play();
                }

                ImGui.PopFont();

                if( State == PlaybackState.Stopped ) ImGui.PushStyleVar( ImGuiStyleVar.Alpha, 0.5f );
                var selectedTime = ( float )CurrentTime;
                ImGui.SameLine( 50f );
                ImGui.SetNextItemWidth( 221f );
                var drawPos = ImGui.GetCursorScreenPos();
                if( ImGui.SliderFloat( $"{id}-Drag", ref selectedTime, 0, ( float )TotalTime ) ) {
                    if( State != PlaybackState.Stopped && selectedTime > 0 && selectedTime < TotalTime ) {
                        CurrentOutput.Pause();
                        CurrentStream.CurrentTime = TimeSpan.FromSeconds( selectedTime );
                    }
                }
                if( State == PlaybackState.Stopped ) ImGui.PopStyleVar();

                if( State != PlaybackState.Stopped && !Entry.NoLoop && LoopTimeInitialized && Plugin.Configuration.SimulateScdLoop ) {
                    var startX = 221f * ( LoopStartTime / TotalTime );
                    var endX = 221f * ( LoopEndTime / TotalTime );

                    var startPos = drawPos + new Vector2( ( float )startX - 2, 0 );
                    var endPos = drawPos + new Vector2( ( float )endX - 2, 0 );

                    var height = ImGui.GetFrameHeight();

                    var drawList = ImGui.GetWindowDrawList();
                    drawList.AddRectFilled( startPos, startPos + new Vector2( 4, height ), 0xFFFF0000, 1 );
                    drawList.AddRectFilled( endPos, endPos + new Vector2( 4, height ), 0xFFFF0000, 1 );
                }

                // Save
                ImGui.SameLine();
                ImGui.PushFont( UiBuilder.IconFont );
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Download}" + id ) ) {
                    if( IsVorbis ) ImGui.OpenPopup( "SavePopup" + id );
                    else SaveWaveDialog();
                }
                ImGui.PopFont();
                UiUtils.Tooltip( "Export sound file to .wav or .ogg" );

                if( ImGui.BeginPopup( "SavePopup" + id ) ) {
                    if( ImGui.Selectable( ".wav" ) ) SaveWaveDialog();
                    if( ImGui.Selectable( ".ogg" ) ) SaveOggDialog();
                    ImGui.EndPopup();
                }

                // Import
                ImGui.SameLine();
                ImGui.PushFont( UiBuilder.IconFont );
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Upload}" + id ) ) ImportDialog();
                ImGui.PopFont();
                UiUtils.Tooltip( "Replace sound file" );

                var loopStartEnd = new int[2] { Entry.LoopStart, Entry.LoopEnd };
                ImGui.SetNextItemWidth( 250f );
                if( ImGui.InputInt2( $"{id}/LoopStartEnd", ref loopStartEnd[0] ) ) {
                    Entry.LoopStart = loopStartEnd[0];
                    Entry.LoopEnd = loopStartEnd[1];
                    RefreshLoopStartEndTime();
                }

                // Convert
                ImGui.SameLine();
                ImGui.PushFont( UiBuilder.IconFont );
                if( ImGui.Button( $"{( char )FontAwesomeIcon.Sync}" + id ) ) ConverterOpen = !ConverterOpen;
                ImGui.PopFont();
                UiUtils.Tooltip( "Open converter" );
                ImGui.SameLine();
                ImGui.Text( "Loop Start/End (Bytes)" );

                if( ConverterOpen ) {
                    var style = ImGui.GetStyle();
                    ImGui.BeginChild( $"{id}/Child", new Vector2( ImGui.GetContentRegionAvail().X, ImGui.GetFrameHeight() * 2 + style.FramePadding.Y * 4 + style.ItemSpacing.Y * 2 ), true );

                    // Bytes
                    ImGui.SetNextItemWidth( 100 ); ImGui.InputInt( $"{id}/SamplesIn", ref ConverterSamples, 0, 0 );
                    ImGui.SameLine();
                    ImGui.PushFont( UiBuilder.IconFont ); ImGui.Text( $"{( char )FontAwesomeIcon.ArrowRight}" ); ImGui.PopFont();
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth( 100 ); ImGui.InputInt( $"{id}/SamplesOut", ref ConverterSamplesOut, 0, 0, ImGuiInputTextFlags.ReadOnly );
                    ImGui.SameLine();
                    if( ImGui.Button( $"Samples to Bytes{id}") ) {
                        ConverterSamplesOut = Entry.Data.SamplesToBytes( ConverterSamples );
                    }

                    // Time
                    ImGui.SetNextItemWidth( 100 ); ImGui.InputFloat( $"{id}/SecondsIn", ref ConverterSeconds, 0, 0 );
                    ImGui.SameLine();
                    ImGui.PushFont( UiBuilder.IconFont ); ImGui.Text( $"{( char )FontAwesomeIcon.ArrowRight}" ); ImGui.PopFont();
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth( 100 ); ImGui.InputInt( $"{id}/SecondsOut", ref ConverterSecondsOut, 0, 0, ImGuiInputTextFlags.ReadOnly );
                    ImGui.SameLine();
                    if( ImGui.Button( $"Seconds to Bytes{id}" ) ) {
                        ConverterSecondsOut = Entry.Data.TimeToBytes( ConverterSeconds );
                    }
                    ImGui.EndChild();
                }

                ImGui.TextDisabled( $"{Entry.Format} / {Entry.NumChannels} Ch / {Entry.SampleRate}Hz / 0x{Entry.DataLength:X8} bytes" );

                ImGui.Unindent();
            }

            var currentState = State;
            var justQueued = false;

            if( currentState == PlaybackState.Stopped && PrevState == PlaybackState.Playing &&
                ( ( IsVorbis && Plugin.Configuration.LoopMusic ) || ( !IsVorbis && Plugin.Configuration.LoopSoundEffects ) ) ) {
                PluginLog.Log( "Looping..." );
                Play();
                if( !Entry.NoLoop && Plugin.Configuration.SimulateScdLoop && LoopTimeInitialized && LoopStartTime > 0 ) {
                    if( QueueSeek == -1 ) {
                        QueueSeek = LoopStartTime;
                        justQueued = true;
                    }
                }
            }
            else if( currentState == PlaybackState.Playing && !Entry.NoLoop && Plugin.Configuration.SimulateScdLoop && LoopTimeInitialized && Math.Abs( LoopEndTime - CurrentTime ) < 0.03f ) {
                if( QueueSeek == -1 ) {
                    QueueSeek = LoopStartTime;
                    justQueued = true;
                }
            }

            if( currentState == PlaybackState.Playing && QueueSeek != -1 && !justQueued ) {
                CurrentStream.CurrentTime = TimeSpan.FromSeconds( QueueSeek );
                QueueSeek = -1;
            }

            PrevState = currentState;
        }

        private void Play() {
            Reset();
            try {
                if( !LoopTimeInitialized ) RefreshLoopStartEndTime();

                var stream = Entry.Data.GetStream();
                PluginLog.Log( $"Playing @ {stream.WaveFormat.SampleRate} / {stream.WaveFormat.BitsPerSample}" );

                CurrentStream = stream.WaveFormat.Encoding switch {
                    WaveFormatEncoding.Pcm => WaveFormatConversionStream.CreatePcmStream( stream ),
                    WaveFormatEncoding.Adpcm => WaveFormatConversionStream.CreatePcmStream( stream ),
                    _ => stream
                };

                CurrentChannel = new WaveChannel32( CurrentStream ) {
                    Volume = Plugin.Configuration.ScdVolume,
                    PadWithZeroes = false,
                };
                CurrentOutput = new WasapiOut();

                CurrentOutput.Init( CurrentChannel );
                CurrentOutput.Play();
            }
            catch( Exception e ) {
                PluginLog.LogError( e, "Error playing sound" );
            }
        }

        public void UpdateVolume() {
            if( CurrentChannel == null ) return;
            CurrentChannel.Volume = Plugin.Configuration.ScdVolume;
        }

        private void ImportDialog() {
            var text = IsVorbis ? "Audio files{.ogg,.wav},.*" : "Audio files{.wav},.*";
            FileDialogManager.OpenFileDialog( "Import File", text, ( bool ok, string res ) => {
                if( ok ) ScdFile.Import( res, Entry );
            } );
        }

        private void SaveWaveDialog() {
            FileDialogManager.SaveFileDialog( "Select a Save Location", ".wav", "ExportedSound", "wav", ( bool ok, string res ) => {
                if( ok ) {
                    using var stream = Entry.Data.GetStream();
                    WaveFileWriter.CreateWaveFile( res, stream );
                }
            } );
        }

        private void SaveOggDialog() {
            FileDialogManager.SaveFileDialog( "Select a Save Location", ".ogg", "ExportedSound", "ogg", ( bool ok, string res ) => {
                if( ok ) {
                    var data = ( ScdVorbis )Entry.Data;
                    File.WriteAllBytes( res, data.DecodedData );
                }
            } );
        }

        private async void RefreshLoopStartEndTime() {
            if( LoopTimeRefreshing ) return;
            LoopTimeRefreshing = true;
            await Task.Run( () => {
                Entry.Data.BytesToLoopStartEnd( Entry.LoopStart, Entry.LoopEnd, out LoopStartTime, out LoopEndTime );
                LoopTimeInitialized = true;
                LoopTimeRefreshing = false;
            } );
        }

        public void Reset() {
            CurrentOutput?.Dispose();
            CurrentChannel?.Dispose();
            CurrentStream?.Dispose();
        }

        public void Dispose() {
            CurrentOutput?.Stop();
            Reset();
        }
    }
}
