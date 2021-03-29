using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames.SortMovies
{
    public class RadixMergeMovieGen : IVideoGenerator, IDisposable
    {
        private readonly Font _font;
        private readonly Font _fontSmall;
        
        private readonly int _width;
        private readonly int _height;
        private readonly int _itemCount;
        private readonly int _estComplex;
        
        private readonly byte[] _a;
        private readonly byte[] _b;
        private readonly IEnumerator<int> _iterator;
        
        private bool _aIsSource = true;
        private int _steps;
        private int _leftInsert, _rightInsert, _readPoint;
        private int _lastRadix;
        private int _inspectionCount;
        private int _copyCount;
        private int _swapCount;
        private readonly string _name;

        public RadixMergeMovieGen(int width, int height, string name, byte[] data)
        {
            _width = width;
            _height = height;
            _name = name;
            _itemCount = data.Length;
            _a = data;
            _b = new byte[_itemCount];
            
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);
            
            
            _estComplex = (int)(8 * _itemCount); // byte sized keys
            
            _steps = 0;
            _iterator = RadixMergeSort().GetEnumerator();
        }

        public bool DrawFrame(int frameNumber, Graphics g)
        {
            //if (frameNumber > 10000) return false; // uncomment to limit for testing
            
            var mid = _height / 2.0f;
            var xs = _width / (_itemCount+1.0f);
            var ys = mid / 255.0f;
            
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            
            // group
            g.Clear(Color.Black); // black is destination
            g.FillRectangle(Brushes.Blue, 0, _aIsSource ? 0 : mid, _width, mid); // blue is source
            
            // Title
            g.DrawString($"MSD Radix merge. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_inspectionCount} inspections, {_copyCount} copies, {_swapCount} swaps, {_steps} iterations, radix {_lastRadix}", _fontSmall, Brushes.WhiteSmoke, 10, 66);
            g.DrawString($"n = {_itemCount}; O(kn) = {_estComplex}, O(2n) auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 84);
            
            // indicators
            g.FillRectangle(Brushes.Red, (_leftInsert-1)*xs, _aIsSource ? mid : 0, 2, mid);
            g.FillRectangle(Brushes.Fuchsia, (_rightInsert+1)*xs, _aIsSource ? mid : 0, 2, mid);
            g.FillRectangle(Brushes.DarkCyan, _readPoint*xs, _aIsSource ? 0 : mid, 2, mid);

            for (int i = 0; i < _itemCount; i++)
            {
                var a = mid - (_a[i] * ys);
                var b = _height - (_b[i] * ys);
                var x = i*xs;
                
                g.FillRectangle(Brushes.White, x - 2, a - 2, 4, 4);
                g.FillRectangle(Brushes.White, x - 2, b - 2, 4, 4);
            }

            return _iterator.MoveNext();
        }

        public void SwapBuffers()
        {
            _aIsSource = !_aIsSource;
            _swapCount++;
        }

        public byte[] Source => _aIsSource ? _a : _b;
        public byte[] Dest => _aIsSource ? _b : _a;

        /// <summary>
        /// Radix merge is somewhere between quick-sort and a merge sort.
        /// We keep track of a list of 'done' and 'not-done' areas
        /// </summary>
        IEnumerable<int> RadixMergeSort() {
            var n = _itemCount;
            if (n < 2) yield break;

            var waitingSpans = new Queue<RadixSpan>();
            
            _lastRadix = 7;
            waitingSpans.Enqueue(new RadixSpan{Left = 0, Right = n-1, Radix = _lastRadix});
            
            // Scan through each binary digit.
            // If it's 0, we write to the left (and move it forward)
            // If it's 1, we write to the right (and move it back)
            while (waitingSpans.Count > 0)
            {
                var span = waitingSpans.Dequeue();
                if (span.Radix != _lastRadix)
                {
                    _lastRadix = span.Radix;
                    SwapBuffers();
                }

                // Split the span into low and high sets into the 'other' buffer
                _leftInsert = span.Left;
                _rightInsert = span.Right;
                for (var pos = span.Left; pos <= span.Right; pos++)
                {
                    _readPoint = pos;
                    if (BitIsHigh(pos, span.Radix)) CopyRight(pos);
                    else CopyLeft(pos);
                    yield return _steps++;
                }
                
                // Write next level of spans (split this one in half)
                if (span.Radix > 0)
                {
                    waitingSpans.Enqueue(new RadixSpan {Left = span.Left, Right = _rightInsert, Radix = span.Radix - 1});
                    waitingSpans.Enqueue(new RadixSpan {Left = _leftInsert, Right = span.Right, Radix = span.Radix - 1});
                }
            }
        }

        private class RadixSpan
        {
            public int Left;
            public int Right;
            public int Radix;
        }

        private void CopyLeft(int srcIndex)
        {
            _copyCount++;
            Dest[_leftInsert] = Source[srcIndex];
            _leftInsert++;
        }
        
        private void CopyRight(int srcIndex)
        {
            _copyCount++;
            Dest[_rightInsert] = Source[srcIndex];
            _rightInsert--;
        }

        private bool BitIsHigh(int index, int radix)
        {
            _inspectionCount++;
            return ((Source[index] >> radix) & 1) > 0 ;
        }

        public void Dispose()
        {
            _font.Dispose();
            _fontSmall.Dispose();
            _iterator.Dispose();
        }
    }
}