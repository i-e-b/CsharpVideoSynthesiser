using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames
{
    public class QSortMovieGen : IVideoGenerator, IDisposable
    {
        private readonly Font _font;
        private readonly Font _fontSmall;

        private readonly int _width;
        private readonly int _height;
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

        public QSortMovieGen(int width, int height, int itemCount)
        {
            _width = width;
            _height = height;
            _itemCount = itemCount;
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);

            var rnd = new Random();
            _data = new byte[_itemCount];
            rnd.NextBytes(_data);

            _estComplex = (int) (Math.Log2(_itemCount) * _itemCount);

            _steps = 0;
            _stack = new Stack<IdxSpan>();
            _iterator = RecursiveQSort().GetEnumerator();
        }

        public bool DrawFrame(int frameNumber, Graphics g)
        {
            if (frameNumber > 6000) return false; // uncomment to limit for testing

            var mid = _height / 2.0f;
            var xs = _width / (_itemCount + 1.0f);
            var ys = mid / 255.0f;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            // group
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.Blue, 0, 0, _width, mid); // top shows data, bottom shows stack span "flame graph"
            
            // indicators
            g.FillRectangle(Brushes.Aqua, _activeSpanLeft*xs, mid + 1, _activeSpanWidth*xs, 10); // active span
            g.FillRectangle(Brushes.DarkCyan, _pivotPoint*xs, mid + 1, 4, 10); // pivot
            g.FillRectangle(Brushes.Fuchsia, _left*xs, mid + 1, 4, 10); // cursor left
            g.FillRectangle(Brushes.Red, _right*xs, mid + 1, 4, 10); // cursor right
            
            // draw stack
            var currentStack = _stack.ToArray();
            var pos = mid + 15;
            foreach (var span in currentStack)
            {
                g.FillRectangle(Brushes.Orange, span.Left*xs, pos, (span.Right-span.Left)*xs, 2); // active span
                pos += 2;
            }

            var mx = 0.0;
            for (int i = 0; i < _itemCount; i++)
            {
                var a = mid - (_data[i] * ys);
                var x = i * xs;
                mx = Math.Max(x, mx);

                g.FillEllipse(Brushes.Lavender, x - 2, a - 2, 4, 4);
            }

            // Title
            g.DrawString($"Recursive quick sort. {_itemCount} items (random)", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_compareCount} compares, {_copyCount} copies, {_swapCount} swaps", _fontSmall, Brushes.WhiteSmoke, 10, 70);
            g.DrawString($"{_steps} iterations, stack depth {_stack.Count}, span {_activeSpanWidth} elements.", _fontSmall, Brushes.WhiteSmoke, 10, 90);
            g.DrawString($"n = {_itemCount}; O(n log n) = {_estComplex}, n auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 110);


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

                if (_activeSpanWidth <= 4)
                {
                    if (Compare(_left, _data[_left+1])) Swap(_left, _left+1);
                    if (Compare(_right - 1, _data[_right])) Swap(_right, _right-1);
                    if (Compare(_left+1, _data[_right - 1])) Swap(_left+1, _right-1);
                    continue;
                }

                // pick a pivot, by middle-of-three
                var centre = span.Left + ((span.Right-span.Left) / 2); // smarter q-sorts pick best-out-of-n or similar
                var pL = _data[_left];
                var pC = _data[centre];
                var pR = _data[_right];
                byte pivotValue;
                
                // pick most middle:
                if (pL > pC && pC > pR) { _pivotPoint = centre; pivotValue = pC; _compareCount += 2; }
                else if (pC > pL && pL > pR) { _pivotPoint = _left; pivotValue = pL; _compareCount += 4; }
                else { _pivotPoint = _right; pivotValue = pR; _compareCount += 4;  }


                // Partition data around pivot *value* by swapping left and right sides
                for (;; _left++, _right--)
                {
                    while (_left < _right && Compare(_left, pivotValue))
                    {
                        _left++;
                        yield return _steps++;
                    }

                    while (_left < _right && !Compare(_right, pivotValue))
                    {
                        _right--;
                        yield return _steps++;
                    }

                    if (_left >= _right)
                    {
                        yield return _steps++;
                        break;
                    }

                    Swap(_left, _right);
                    yield return _steps++;
                }

                // recurse
                if (_right == span.Right) _right--;

                _stack.Push(new IdxSpan {Left = span.Left, Right = _right}); // left side
                //_stack.Push(new IdxSpan {Left = _left, Right = span.Right}); // right side
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

        private bool Compare(int l, int value)
        {
            _compareCount++;
            return _data[l] < value;
        }


        public void Dispose()
        {
            // TODO: rebuild later
        }
    }

    internal class IdxSpan
    {
        public int Left { get; set; }
        public int Right { get; set; }
    }
}