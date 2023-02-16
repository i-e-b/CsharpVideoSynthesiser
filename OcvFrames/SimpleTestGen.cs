using System;
using System.Collections.Generic;
using System.Drawing;

namespace OcvFrames
{
    public class SimpleTestGen : IVideoGenerator, IDisposable
    {
        private readonly Font _font;
        private readonly Pen _pen;
        private readonly byte[] _sample;
        private readonly List<byte> _audio;
        private int _samplePosition;

        public SimpleTestGen()
        {
            _font = new Font("Dave", 18);
            _pen = new Pen(Color.White, 3.5f);
            
            var toneSamples = 44100 / 440;
            _sample = new byte[toneSamples];
            var r = Math.PI * 2.0 / toneSamples;
            for (int i = 0; i < toneSamples; i++)
            {
                _sample[i] = (byte)((Math.Sin(i * r) * 32.0) + 127.0);
            }
            _samplePosition = 0;
            
            _audio = new List<byte>();
        }
        
        public bool DrawFrame(int videoFrameNumber, Graphics g)
        {
            if (videoFrameNumber > 60) return false;
            
            g.Clear(Color.Green);
            g.DrawArc(_pen, 10, 10, 64, 64, 0, videoFrameNumber * 6);
            g.DrawString($"Frame {videoFrameNumber}", _font, Brushes.White, 10, 80);
            g.DrawString("Video output test █▓▒░   ▤▥▦▧▨▩", _font, Brushes.White, 10, 140);
            
            var rate = 44100.0 / 60.0;
            var expectedAudioSamples = (videoFrameNumber+1) * rate;
            var moreSamples = ((int)expectedAudioSamples) - _audio.Count;

            for (int i = 0; i < moreSamples; i++)
            {
                _audio.Add(_sample[_samplePosition++]);
                if (_samplePosition >= _sample.Length) _samplePosition = 0;
            }
            
            return true;
        }

        public IEnumerable<byte> GetAudioSamples()
        {
            Console.WriteLine($"Total samples = {_audio.Count}");
            return _audio;
        }

        public void Dispose()
        {
            _font.Dispose();
            _pen.Dispose();
        }
    }
}