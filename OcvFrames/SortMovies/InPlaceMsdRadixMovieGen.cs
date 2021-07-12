using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using OcvFrames.Helpers;

namespace OcvFrames.SortMovies
{
    public class InPlaceMsdRadixMovieGen : IVideoGenerator, IDisposable
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
        private int _inspectionCount;
        private int _copyCount;
        private int _swapCount;
        private readonly Stack<RadixSpan> _stack;
        private int _left;
        private int _right;
        private int _radix;

        public InPlaceMsdRadixMovieGen(int width, int height, string name, byte[] data)
        {
            _width = width;
            _height = height;
            _name = name;
            _itemCount = data.Length;
            _data = data;
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);

            _estComplex = (int) (8 * _itemCount); // 8 bit keys

            _steps = 0;
            _stack = new Stack<RadixSpan>();
            _iterator = InPlaceRadixSort().GetEnumerator();
        }

        public bool DrawFrame(int frameNumber, Graphics g)
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
            g.FillRectangle(Brushes.Aqua, _activeSpanLeft*xs, mid + 1, _activeSpanWidth*xs, 10); // active span
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
            g.DrawString($"In place MSD radix sort. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_inspectionCount} inspections, {_copyCount} copies, {_swapCount} swaps", _fontSmall, Brushes.WhiteSmoke, 10, 70);
            g.DrawString($"{_steps} iterations, stack depth {_stack.Count}, span {_activeSpanWidth} elements.", _fontSmall, Brushes.WhiteSmoke, 10, 90);
            g.DrawString($"n = {_itemCount}; O(kn) = {_estComplex}, O(log n) auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 110);


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

        IEnumerable<int> InPlaceRadixSort()
        {
            var len = _itemCount;
            if (len < 2) yield break;

            _stack.Push(new RadixSpan {Left = 0, Right = len - 1, Radix = 7});

            while (_stack.Count > 0)
            {
                var span = _stack.Pop();
                _left = span.Left;
                _right = span.Right;
                if (_left >= _right) continue;
                _radix = span.Radix;
                _activeSpanLeft = span.Left;
                _activeSpanWidth = span.Right - span.Left;
                
                // scan left and right sides. Find a pair that are both on the wrong side and swap them
                while (_left < _right)
                {
                    if (BitIsLow(_left, _radix)) _left++; // this one is in the right place
                    else if (BitIsHigh(_right, _radix)) _right--; // this one too
                    else { // both _left and _right point to opposite sides in the wrong place. Flip them
                        Swap(_left, _right);
                    }
                    yield return _steps++;
                }

                // recurse
                if (_radix > 0)
                {
                    Console.WriteLine($"R{_radix}, {span.Left}[{_left}|{_right}]{span.Right}");
                    _stack.Push(new RadixSpan {Left = _right, Right = span.Right, Radix = _radix - 1}); // right side - leave this out to get minimal element only
                    _stack.Push(new RadixSpan {Left = span.Left, Right = _right - 1, Radix = _radix - 1}); // left side
                }
            }
        }

        private void Swap(int a, int b)
        {
            _copyCount += 3;
            _swapCount++;
            var tmp = _data[a];
            _data[a] = _data[b];
            _data[b] = tmp;
        }
        
       
        private bool BitIsLow(int index, int radix)
        {
            _inspectionCount++;
            return ((_data[index] >> radix) & 1) == 0;
        } 
        
        private bool BitIsHigh(int index, int radix)
        {
            _inspectionCount++;
            return ((_data[index] >> radix) & 1) > 0;
        } 


        public void Dispose()
        {
            _font.Dispose();
            _fontSmall.Dispose();
            _iterator.Dispose();
        }
    }
}