using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace OcvFrames
{
    /// <summary>
    /// Write a sequence of frames to a video file.
    /// Does not support audio
    /// </summary>
    public class OcvVideoFileSynthesiser: IDisposable
    {
        private readonly VideoWriter _writer;
        private readonly Mat _writerFrame;
        private readonly Bitmap _frameImage;

        private const string ApiCode = "MSMF";
        
        public OcvVideoFileSynthesiser(string filePath, int width, int height, int framesPerSecond)
        {
            var size = new Size(width, height);
            var compressionType = VideoWriter.Fourcc('H', '2', '6', '4');

            var backends = CvInvoke.WriterBackends;
            var backendId = (from be in backends where be.Name?.Equals(ApiCode)??false select be.ID).FirstOrDefault();
            
            _writerFrame = new Mat(size, DepthType.Cv8U, 3);
            _frameImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            _writer = new VideoWriter(filePath, backendId, compressionType, framesPerSecond, size, isColor: true);
        }

        public void WriteVideo(IVideoGenerator source)
        {
            using var g = Graphics.FromImage(_frameImage);
            var more = true;
            var i = 0;
            while (more)
            {
                more = source.DrawFrame(i++, g);
                CopyFrame(_frameImage, _writerFrame);
                _writer.Write(_writerFrame);
            }
            if (source is IDisposable d) d.Dispose();
        }
        
        private static unsafe void CopyFrame(Bitmap? src, Mat? dst)
        {
            if (src == null || dst == null) return;
            var bits = src.LockBits(new Rectangle(0,0, src.Width, src.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                var length = bits.Stride * bits.Height;
                Buffer.MemoryCopy(bits.Scan0.ToPointer()!, dst.DataPointer.ToPointer()!, length, length);
            }
            finally
            {
                src.UnlockBits(bits);
            }
        }

        public void Dispose()
        {
            _writer.Dispose();
            _writerFrame.Dispose();
            _frameImage.Dispose();
        }
    }
}