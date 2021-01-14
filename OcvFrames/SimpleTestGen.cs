using System;
using System.Drawing;

namespace OcvFrames
{
    public class SimpleTestGen : IVideoGenerator, IDisposable
    {
        private readonly Font _font;
        private readonly Pen _pen;

        public SimpleTestGen()
        {
            _font = new Font("Dave", 11);
            _pen = new Pen(Color.White, 3.5f);
        }
        public bool DrawFrame(int frameNumber, Graphics g)
        {
            if (frameNumber > 60) return false;
            
            g.Clear(Color.Green);
            g.DrawArc(_pen, 10, 10, 64, 64, 0, frameNumber * 6);
            g.DrawString($"Frame {frameNumber}", _font, Brushes.White, 10, 80);
            
            return true;
        }

        public void Dispose()
        {
            _font.Dispose();
            _pen.Dispose();
        }
    }
}