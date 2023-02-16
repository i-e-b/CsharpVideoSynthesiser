using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames.SortMovies
{
    public class MergeMovieGen : IVideoGenerator, IDisposable
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
        private int _steps, _mergeWindow;
        private int _lastLeft, _lastRight, _lastInsert;
        private int _compareCount;
        private int _copyCount;
        private int _swapCount;
        private string _name;

        public MergeMovieGen(int width, int height, string name, byte[] data)
        {
            _width = width;
            _height = height;
            _name = name;
            _itemCount = data.Length;
            _a = data;
            _b = new byte[_itemCount];
            
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);
            
            
            _estComplex = (int)(Math.Log2(_itemCount) * _itemCount);
            
            _steps = 0;
            _mergeWindow = 0;
            _iterator = IterativeMergeSort().GetEnumerator();
        }

        public bool DrawFrame(int videoFrameNumber, Graphics g)
        {
            //if (frameNumber > 1000) return false; // uncomment to limit for testing
            
            var mid = _height / 2.0f;
            var xs = _width / (_itemCount+1.0f);
            var ys = mid / 255.0f;
            
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            
            // group
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.Blue, 0, _aIsSource ? 0 : mid, _width, mid);
            
            // Title
            g.DrawString($"Bottom up merge. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_compareCount} compares, {_copyCount} copies, {_swapCount} swaps, {_steps} iterations, window {_mergeWindow} wide.", _fontSmall, Brushes.WhiteSmoke, 10, 66);
            g.DrawString($"n = {_itemCount}; O(n log n) = {_estComplex}, n auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 84);
            
            // indicators
            g.FillRectangle(Brushes.Red, (_lastLeft-1)*xs, _aIsSource ? 0 : mid, 2, mid);
            g.FillRectangle(Brushes.Fuchsia, (_lastRight+1)*xs, _aIsSource ? 0 : mid, 2, mid);
            g.FillRectangle(Brushes.DarkCyan, _lastInsert*xs, _aIsSource ? mid : 0, 2, mid);

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
        

        public bool GetAudioSamples(int videoFrameNumber, int audioFrameNumber, out byte[]? samples)
        {
            samples = null;
            return false;
        }


        public byte[] Source => _aIsSource ? _a : _b;
        public byte[] Dest => _aIsSource ? _b : _a;

        IEnumerable<int> IterativeMergeSort() {
            var n = _itemCount;
            if (n < 2) yield break;

            for (var stride = 1; stride < n; stride <<= 1) { // doubling merge width
                _mergeWindow = stride;
                
                int t = 0; // incrementing point in target array
                for (var left = 0; left < n; left += stride << 1) {
                    var right = left + stride;
                    var end = right + stride;
                    if (end > n) end = n; // some merge windows will run off the end of the data array
                    if (right > n) right = n; // some merge windows will run off the end of the data array
                    var l = left;
                    var r = right; // the points we are scanning though the two sets to be merged.
                    
                    // copy the lowest candidate across from A to B
                    while (l < right && r < end) {
                        _lastLeft = l; _lastRight = r; _lastInsert = t;
                        
                        // compare the two bits to be merged
                        if (Compare(l, r)) Copy(t++, l++);
                        else Copy(t++, r++);
                        
                        yield return _steps++;
                    } // end of loop : exhausted at least one of the merge sides

                    while (l < right) { // run down left if anything remains
                        _lastLeft = l; _lastRight = r; _lastInsert = t;
                        Copy(t++, l++);
                        yield return _steps++;
                    }

                    while (r < end) { // run down right side if anything remains
                        _lastLeft = l; _lastRight = r; _lastInsert = t;
                        Copy(t++, r++);
                        yield return _steps++;
                    }
                }
                
                // swap A and B pointers after each merge set
                _aIsSource = !_aIsSource;
                _swapCount++;
            }
        }

        private void Copy(int t, int x)
        {
            _copyCount++;
            Dest[t] = Source[x];
        }

        private bool Compare(int l, int r)
        {
            _compareCount++;
            return Source[l] < Source[r];
        }

        public void Dispose()
        {
            _font.Dispose();
            _fontSmall.Dispose();
            _iterator.Dispose();
        }
    }
}