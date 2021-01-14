using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using NUnit.Framework;

namespace OcvFrames
{
    [TestFixture]
    public class BasicEncoding
    {
        [Test]
        public void create_a_movie_file()
        {
            // ReSharper disable once StringLiteralTypo
            const string apiCode = "MSMF";
            const string path = "video_out.mp4";
            
            const int width = 800;
            const int height = 450;
            
            var size = new Size(width, height);
            var compressionType = VideoWriter.Fourcc('H', '2', '6', '4');

            var backends = CvInvoke.WriterBackends;
            var backendId = (from be in backends where be.Name?.Equals(apiCode)??false select be.ID).FirstOrDefault();
            
            using var frame = new Mat(size, DepthType.Cv8U, 3);
            
            using var frameBmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            using (var vw = new VideoWriter(path, backendId, compressionType, fps: 30, size, isColor: true))
            {
                for (int i = 0; i < 60; i++)
                {
                    DrawFrame(i, frameBmp);
                    CopyFrame(frameBmp, frame);
                    vw.Write(frame);
                }
            }

            Assert.That(File.Exists(path), "file was not written");
        }
        
        private static readonly Font _font = new Font("Dave", 11);

        private static void DrawFrame(int i, Image frameBmp)
        {
            using var g = Graphics.FromImage(frameBmp!);
            
            g.Clear(Color.Green);
            g.DrawArc(Pens.White, 10, 10, 64, 64, 0, i * 6);
            g.DrawString($"Frame {i}", _font, Brushes.White, 10, 80);
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
    }
}