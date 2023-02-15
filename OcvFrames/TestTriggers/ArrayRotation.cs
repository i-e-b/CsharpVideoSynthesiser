using System;
using System.IO;
using NUnit.Framework;
using OcvFrames.RotateAndShuffle;

namespace OcvFrames.TestTriggers
{
    [TestFixture]
    public class ArrayRotation
    {
        [Test]
        public void Array_rotation_with_3_reversals()
        {
            const string path = "3_rev_array_rotate_video_out.mp4";

            const int width = 1920;//640;//1920;
            const int height = 1080;//448;//1080;
            const int fps = 60;

            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new ArrayRotate_3Reversals(width, height, length:1024, rotatePlaces:256));

            Assert.That(File.Exists(path), "file was not written");
        }
        
        [Test]
        public void Trinity_array_rotation()
        {
            const string path = "trinity_array_rotate_video_out.mp4";

            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;

            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new ArrayRotate_Trinity(width, height, length:1024, rotatePlaces:256));

            Assert.That(File.Exists(path), "file was not written");
            Console.WriteLine(Path.GetFullPath(path));
        }
        
        [Test]
        public void Trinity_array_rotation_FFMPEG()
        {
            const string path = "trinity_array_rotate_video_out_ffmpeg.mp4";

            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;

            using var subject = new FfmpegFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new ArrayRotate_Trinity(width, height, length:1024, rotatePlaces:256));

            Assert.That(File.Exists(path), "file was not written");
            Console.WriteLine(Path.GetFullPath(path));
        }
    }
}