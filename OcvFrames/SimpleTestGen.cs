using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace OcvFrames
{
    public class SimpleTestGen : IVideoGenerator, IDisposable
    {
        private readonly Font _font;
        private readonly Pen _pen;
        private readonly byte[] _sample;

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
        }
        
        public bool DrawFrame(int videoFrameNumber, Graphics g)
        {
            if (videoFrameNumber > 60) return false;
            
            g.Clear(Color.Green);
            g.DrawArc(_pen, 10, 10, 64, 64, 0, videoFrameNumber * 6);
            g.DrawString($"Frame {videoFrameNumber}", _font, Brushes.White, 10, 80);
            g.DrawString("Video output test █▓▒░   ▤▥▦▧▨▩", _font, Brushes.White, 10, 140);
            
            return true;
        }

        public bool GetAudioSamples(int videoFrameNumber, int audioFrameNumber, out byte[]? samples)
        {
            // This causes the output to lock up.
            // sync test
            //if (audioFrameNumber > (videoFrameNumber * 7))
            //{
            //    samples = Array.Empty<byte>();
            //    return true;
            //}
            // end sync test
            
            //Console.Write('a');
            //Thread.Sleep(5);
            
            if (audioFrameNumber > 440)
            {
                Console.WriteLine($"\r\nAudio ended at audio frame count. Video at {videoFrameNumber}");
                samples = null;
                return false;
            }

            if (videoFrameNumber > 60)
            {
                Console.WriteLine($"\r\nAudio ended at video frame count. Audio at {audioFrameNumber}");
                samples = null;
                return false;
            }
            
            samples = _sample;
            return true;
        }


        public void Dispose()
        {
            _font.Dispose();
            _pen.Dispose();
        }
    }
}