using System.IO;
using NUnit.Framework;

namespace OcvFrames
{
    [TestFixture]
    public class BasicEncoding
    {
        [Test]
        public void create_a_basic_movie_file()
        {
            const string path = "video_out.mp4";
            
            const int width = 800;
            const int height = 450;
            const int fps = 60;
            
            using var subject = new VideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new SimpleTestGen());

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void merge_sort_video()
        {
            // Everyone does one of these, right?
            const string path = "bottom_up_merge.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new VideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new MergeMovieGen(width, height, itemCount:1024));

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void quick_sort_video()
        {
            const string path = "simple_recursive_quicksort.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new VideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new QSortMovieGen(width, height, itemCount:1024));

            Assert.That(File.Exists(path), "file was not written");
        }
    }
}