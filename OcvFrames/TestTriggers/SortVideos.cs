using System.IO;
using NUnit.Framework;
using OcvFrames.Helpers;
using OcvFrames.SortMovies;

namespace OcvFrames.TestTriggers
{
    [TestFixture]
    public class SortVideos
    {
        [Test]
        public void merge_sort_video()
        {
            // Everyone does one of these, right?
            const string path = "bottom_up_merge.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new MergeMovieGen(width, height, "scatter reverse", DataSets.ScatterReverse(512)));

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void quick_sort_video()
        {
            const string path = "simple_recursive_quicksort.mp4";
            
            const int width = 1920;//640;//1920;
            const int height = 1080;//448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new QSortMovieGen(width, height, "scatter reverse", DataSets.ScatterReverse(1024)));

            Assert.That(File.Exists(path), "file was not written");
        }
        
        [Test]
        public void quick_sort_with_pre_phase_video()
        {
            // This sort is pretty bad.
            const string path = "prephase_recursive_quicksort.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new QSortPrephaseMovieGen(width, height,"scatter reverse", DataSets.ScatterReverse(512)));

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void heap_sort_video()
        {
            const string path = "min_heap_sort.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new HeapMovieGen(width, height, "random", DataSets.Random(128)));

            Assert.That(File.Exists(path), "file was not written");
        }
        
        [Test]
        public void optimised_heap_sort_video()
        {
            const string path = "opt_heap_sort.mp4";
            
            const int width = 1920;//640;//1920;
            const int height = 1080;//448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new OptimisedHeapMovieGen(width, height, "random", DataSets.Random(1024)));

            Assert.That(File.Exists(path), "file was not written");
        }
        
        [Test]
        public void repeated_heap_sort_video()
        {
            const string path = "repeated_heap_sort.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new RepeatHeapMovieGen(width, height, "scatter reverse", DataSets.ScatterReverse(128)));

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void radix_merge_sort_video()
        {
            const string path = "radix_merge_sort.mp4";
            
            const int width = 640;//1920;
            const int height = 448;//1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new RadixMergeMovieGen(width, height, "scatter reverse", DataSets.ScatterReverse(128)));

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void radix_in_place_sort_video()
        {
            const string path = "radix_in_place_sort.mp4";
            
            const int width = 1920;//640;//1920;
            const int height = 1080;//448;//1080;
            const int fps = 60;
            
            // This does more inspections than the merge, but *can* end up with far fewer copies,
            // and only requires log2(n) aux space
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new InPlaceMsdRadixMovieGen(width, height, "random", DataSets.Random(1024)));

            Assert.That(File.Exists(path), "file was not written");
        }

        [Test]
        public void tournament_sort_video()
        {
            const string path = "tournament_sort.mp4";
            
            const int width = 1920;
            const int height = 1080;
            const int fps = 60;
            
            using var subject = new OcvVideoFileSynthesiser(path, width, height, fps);
            subject.WriteVideo(new TournamentMergeSortMovieGen(width, height, "random", DataSets.Random(256)));

            Assert.That(File.Exists(path), "file was not written");
        }
    }
}