using System;
using NAudio.Lame;
using NAudio.Wave;
using iTunesLib;
using System.Collections.Generic;

namespace MP3Rec
{
    public class Recorder
    {
        private LameMP3FileWriter _wri;
        private readonly IWaveIn _waveIn = new WasapiLoopbackCapture();
        private readonly IITTrack _track;

        public Recorder(IITTrack track)
        {
            // Setup loopback recorder
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.RecordingStopped += WaveIn_RecordingStopped;
            _track = track;
        }

        private void WaveIn_RecordingStopped(object sender, StoppedEventArgs e)
        {
            _wri.Dispose();
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            // write recorded data to MP3 writer
            if (_wri != null)
                _wri.Write(e.Buffer, 0, e.BytesRecorded);
        }

        public void RecordTrack()
        {
            // flush output to finish MP3 file correctly
            if (_wri != null)
            {
                _wri.Flush();
                _waveIn.StopRecording();
            }

            // Setup MP3 writer to output at 32kbit/sec (~2 minutes per MB)
            _wri = new LameMP3FileWriter($"C:\\temp\\{_track.Name}.mp3", _waveIn.WaveFormat, 128);
            _waveIn.StartRecording();
        }
    }

    public class Program
    {
        private static readonly List<Recorder> recorders = new List<Recorder>();

        public static void Main(string[] args)
        {
            var iTunes = new iTunesApp();

            iTunes.OnPlayerPlayEvent += ITunes_OnPlayerPlayEvent;
            iTunes.OnPlayerPlayingTrackChangedEvent += ITunes_OnPlayerPlayingTrackChangedEvent;

            Console.WriteLine("Press any key to stop");
            Console.ReadLine();
            Console.WriteLine($"Number of Tracks: {recorders.Count}");
            Console.WriteLine("Press Ctrl-C to stop");
            Console.ReadLine();
        }

        private static void ITunes_OnPlayerPlayingTrackChangedEvent(object iTrack)
        {
            var t = (IITTrack) iTrack;
            Console.WriteLine($"ITunes_OnPlayerPlayingTrackChangedEvent, Track: {t.Name}");
        }

        private static void ITunes_OnPlayerPlayEvent(object iTrack)
        {
            var t = (IITTrack) iTrack;
            Console.WriteLine($"ITunes_OnPlayerPlayEvent, Track {t.Name}");

            var r = new Recorder(t);

            r.RecordTrack();

            // This keeps a reference to it so the garbage collector won't destroy it
            recorders.Add(r);
        }
    }
}