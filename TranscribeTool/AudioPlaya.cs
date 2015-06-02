using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NAudio.Wave;

namespace TranscribeTool
{
    public class AudioPlaya
    {
        private IWavePlayer waveOutDevice;
        private WaveStream stream;
        private Mp3FileReader readerMp3;
        private WaveFileReader readerWave;

        public AudioPlaya(string filename)
        {
            waveOutDevice = new WaveOut();
            waveOutDevice.Init(stream = loadFromFile(filename));
        }

        public void Play()
        {
            waveOutDevice.Play();
        }
        public void Pause()
        {
            waveOutDevice.Pause();
        }
        public void Stop()
        {
            waveOutDevice.Stop();
        }

        public TimeSpan Position
        {
            get
            {
                //return TimeSpan.FromMilliseconds(stream.Position);
                return stream.CurrentTime;
            }
            set
            {
                if (value < TimeSpan.Zero)
                    value = TimeSpan.Zero;
                stream.CurrentTime = value; // (long)value.TotalMilliseconds;
            }
        }

        private WaveStream loadFromFile(string filename)
        {
            if (".mp3".Equals(Path.GetExtension(filename), StringComparison.OrdinalIgnoreCase))
                return (readerMp3 = new Mp3FileReader(filename));
            else
                return (readerWave = new WaveFileReader(filename));
        }

        public void Dispose()
        {
            if (readerMp3 != null)
                readerMp3.Dispose();
            if (readerWave != null)
                readerWave.Dispose();
        }

        public bool Playing {
            get {
                return waveOutDevice.PlaybackState == PlaybackState.Playing;
            }
        }
    }
}
