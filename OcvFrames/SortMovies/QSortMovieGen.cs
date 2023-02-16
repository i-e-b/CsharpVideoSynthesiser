using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using OcvFrames.Helpers;

namespace OcvFrames.SortMovies
{
    public class QSortMovieGen : IVideoGenerator, IDisposable
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
        private int _activeSpanLeft, _activeSpanWidth;
        private int _compareCount;
        private int _copyCount;
        private int _swapCount;
        private readonly Stack<IdxSpan> _stack;
        private int _pivotPoint;
        private int _left;
        private int _right;
        private byte _pivotValue;

        public QSortMovieGen(int width, int height, string name, byte[] data)
        {
            _width = width;
            _height = height;
            _name = name;
            _itemCount = data.Length;
            _data = data;
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);

            _estComplex = (int) (Math.Log2(_itemCount) * _itemCount);

            _steps = 0;
            _stack = new Stack<IdxSpan>();
            _iterator = RecursiveQSort().GetEnumerator();
        }

        public bool DrawFrame(int videoFrameNumber, Graphics g)
        {
            //if (frameNumber > 5_000) return false; // reduce for testing

            var mid = _height * 0.75f;
            var xs = _width / (_itemCount + 1.0f);
            var ys = mid / 255.0f;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            // group
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.Blue, 0, 0, _width, mid); // top shows data, bottom shows stack span "flame graph"
            
            // indicators
            var pp = mid - (_pivotValue * ys);
            g.FillRectangle(Brushes.Aqua, _activeSpanLeft*xs, mid + 1, _activeSpanWidth*xs, 10); // active span
            g.FillRectangle(Brushes.DarkCyan, _pivotPoint*xs, pp, 4, mid - pp); // pivot
            g.FillRectangle(Brushes.Fuchsia, _left*xs, mid + 1, 4, 10); // cursor left
            g.FillRectangle(Brushes.Red, _right*xs, mid + 1, 4, 10); // cursor right
            
            // draw stack
            var currentStack = _stack.ToArray();
            var pos = mid + 15;
            foreach (var span in currentStack)
            {
                g.FillRectangle(Brushes.Orange, span.Left*xs, pos, (span.Right-span.Left)*xs, 4); // active span
                pos += 4;
            }

            var mx = 0.0;
            for (int i = 0; i < _itemCount; i++)
            {
                var a = mid - (_data[i] * ys);
                var x = i * xs;
                mx = Math.Max(x, mx);

                g.FillRectangle(Brushes.LightBlue, x - 2, a - 2, 4, 4);
            }

            // Title
            g.DrawString($"Recursive quick sort. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_compareCount} compares, {_copyCount} copies, {_swapCount} swaps", _fontSmall, Brushes.WhiteSmoke, 10, 70);
            g.DrawString($"{_steps} iterations, stack depth {_stack.Count}, span {_activeSpanWidth} elements.", _fontSmall, Brushes.WhiteSmoke, 10, 90);
            g.DrawString($"n = {_itemCount}; O(n log n) = {_estComplex}, O(log n) auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 110);


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
        
        public IEnumerable<byte> GetAudioSamples() => Array.Empty<byte>();

        IEnumerable<int> RecursiveQSort()
        {
            var len = _itemCount;
            if (len < 2) yield break;

            _stack.Push(new IdxSpan {Left = 0, Right = len - 1});

            while (_stack.Count > 0)
            {
                var span = _stack.Pop();
                _left = span.Left;
                _right = span.Right;
                _activeSpanLeft = span.Left;
                _activeSpanWidth = span.Right - span.Left;

                if (_activeSpanWidth < 2) continue;
                if (_activeSpanWidth <= 4){
                    // simple bubble sort for the smallest spans
                    var min = Math.Max(0, _left);
                    var max = Math.Min(len-2, _right);
                    for (var bi = max; bi >= min; bi--) {
                        for (var b = min; b <= bi; b++) if (CompareMore(b, _data[b+1])) Swap(b, b+1);
                    }
                    continue;
                }

                #region pivot
                // pick three sample points - centre and both edges
                var centre = span.Left + _activeSpanWidth / 2;
                
                // sort these into relative correct order
                if (!Compare(span.Left, span.Right)) { Swap(span.Left, span.Right); }
                if (!Compare(span.Left, centre)) { Swap(span.Left, centre); }
                
                // pick the middle as the pivot
                _pivotPoint = centre; _pivotValue = _data[centre];
                yield return _steps++;
                #endregion

                // Partition data around pivot *value* by swapping  at left and right sides of a shrinking sample window
                while (true)
                {
                    while (_left < _right && CompareLess(_left, _pivotValue))
                    {
                        _left++;
                        yield return _steps++;
                    }

                    while (_left < _right && CompareMore(_right, _pivotValue))
                    {
                        _right--;
                        yield return _steps++;
                    }

                    if (_left >= _right) { break; }

                    if (!Compare(_left, _right)) Swap(_left, _right);
                    _left++; _right--;
                    yield return _steps++;
                }

                // recurse
                var adj = (_right == span.Left) ? 1 : 0;
                _stack.Push(new IdxSpan {Left = _right + adj, Right = span.Right}); // right side
                _stack.Push(new IdxSpan {Left = span.Left, Right = _right}); // left side
                yield return _steps++;
            }
        }

        private void Swap(int t, int x)
        {
            _copyCount += 3;
            _swapCount++;
            var tmp = _data[t];
            _data[t] = _data[x];
            _data[x] = tmp;
        }
        
        private bool Compare(int i, int j)
        {
            _compareCount++;
            return _data[i] < _data[j];
        }

        private bool CompareLess(int i, int value)
        {
            _compareCount++;
            return _data[i] < value;
        }
        
        private bool CompareMore(int i, int value)
        {
            _compareCount++;
            return _data[i] > value;
        }

        public void Dispose()
        {
            _font.Dispose();
            _fontSmall.Dispose();
            _iterator.Dispose();
        }
    }
}