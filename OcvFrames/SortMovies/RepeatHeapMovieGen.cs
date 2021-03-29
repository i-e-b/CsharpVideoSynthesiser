using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames.SortMovies
{
    public class RepeatHeapMovieGen : IVideoGenerator, IDisposable
    {
        
        private readonly Font _font;
        private readonly Font _fontSmall;

        private readonly int _width;
        private readonly int _height;
        private readonly string _name;
        private readonly int _itemCount;
        private readonly int _estComplex;

        private readonly byte[] _data;
        private readonly IEnumerator<int> _iterator;
        private int _steps;
        private int _compareCount;
        private int _copyCount;
        private int _swapCount;
        private int _front;
        private int _back;
        private int _mid;

        public RepeatHeapMovieGen(int width, int height, string name, byte[] data)
        {
            _width = width;
            _height = height;
            _name = name;
            _data = data;
            _itemCount = data.Length;
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);

            _estComplex = (int) (_itemCount * _itemCount);

            _steps = 0;
            _iterator = IterativeHeapSort().GetEnumerator();
        }

        public bool DrawFrame(int frameNumber, Graphics g)
        {
            if (frameNumber > 12_000) return false; // reduce for testing

            var mid = _height;
            var xs = _width / (_itemCount + 1.0f);
            var ys = mid / 255.0f;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            // group
            g.Clear(Color.Black);
            
            // markers
            g.FillRectangle(Brushes.Green, (_back*xs) - 2, 0, 4, _height);
            g.FillRectangle(Brushes.DarkCyan, (_mid*xs) - 2, 0, 4, _height);
            g.FillRectangle(Brushes.Brown, (_front*xs) - 2, 0, 4, _height);

            var mx = 0.0;
            for (int i = 0; i < _itemCount; i++)
            {
                var a = mid - (_data[i] * ys);
                var x = i * xs;
                mx = Math.Max(x, mx);

                g.FillRectangle(Brushes.LightBlue, x - 2, a - 2, 4, 4);
            }

            // Title
            g.DrawString($"Repeated heap sort. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_compareCount} compares, {_copyCount} copies, {_swapCount} swaps", _fontSmall, Brushes.WhiteSmoke, 10, 70);
            g.DrawString($"{_steps} iterations", _fontSmall, Brushes.WhiteSmoke, 10, 90);
            g.DrawString($"n = {_itemCount}; O(n^2) = {_estComplex}, O(1) auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 110);

            try
            {
                return _iterator.MoveNext();
            }
            catch (Exception ex)
            {
                Console.Write(ex);
                return false;
            }
        }

        IEnumerable<int> IterativeHeapSort()
        {
            var len = _itemCount;
            if (len < 2) yield break;


            // run heap rule repeated, offset by 3
            // this is really an n^2 sort; doesn't take advantage of the heap rule
            for (int offset = 0; offset < len - 2; offset += 2)
            {
                _back = offset;
                var max = len - offset;

                for (int head = 1; head < max; head++) // can I reduce this?
                {
                    _front = head + offset;
                    var toAdd = _data[head + offset]; // item we're 'adding' the the heaped zone
                    int i;
                    for (i = head; i > 0 && Compare(_data[offset + (i >> 1)], toAdd); i >>= 1) // while the heap is out of order
                    {
                        _copyCount++;
                        _data[i + offset] = _data[offset + (i >> 1)]; // push higher values toward the right
                        _mid = i + offset;
                        yield return _steps++;
                    }
                    _copyCount++;
                    _data[i + offset] = toAdd; // then add value in place where we stopped (toward the left)
                    yield return _steps++;
                }

                // first is in place, check next two
                if (Compare(_data[offset + 1], _data[offset + 2])) Swap(offset + 1, offset + 2);
                yield return _steps++;
            }
        }

        private void Swap(int idx1, int idx2)
        {
            _copyCount += 3;
            _swapCount++;
            var tmp = _data[idx1];
            _data[idx1] = _data[idx2];
            _data[idx2] = tmp;
        }

        private bool Compare(byte a, byte b)
        {
            _compareCount++;
            return a > b; // note, we're comparing the heap property, not the item ordering
        }

        public void Dispose()
        {
            // TODO: re-build when done
        }
    }
}