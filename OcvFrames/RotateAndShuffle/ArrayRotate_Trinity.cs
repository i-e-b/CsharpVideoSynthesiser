using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames.RotateAndShuffle
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Rotate an array by an arbitrary amount in place.
    /// Works by reversing each side of the rotation point separately,
    /// then reversing the entire array.
    /// This is simple and reasonably fast. It is outperformed by the
    /// 'trinity' rotation, which elides some of the inversions here.
    /// </summary>
    public class ArrayRotate_Trinity : IVideoGenerator
    {
        private readonly int _rotatePlaces;
        private readonly int[] _data;
        private readonly IEnumerator<int> _iterator;
        private readonly int _itemCount;
        
        private readonly int _width;
        private readonly int _height;
        
        private readonly Font _font;
        private readonly Font _fontSmall;
        private int _copyCount;
        private int _swapCount;
        private int _steps;
        private int _centre;
        private int _pointA;
        private int _pointB;
        private int _pointC;
        private int _pointD;
        private readonly int _maxValue;

        public ArrayRotate_Trinity(int width, int height, int length, int rotatePlaces)
        {
            _width = width;
            _height = height;
            _itemCount = length;
            _rotatePlaces = rotatePlaces;
            _data = new int[length];
            _maxValue = length;
            
            for (int i = 0; i < length; i++)
            {
                _data[i] = i + 1;
            }
            
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);
            
            _iterator = TrinityArrayRotation().GetEnumerator();
        }
        
        public bool DrawFrame(int videoFrameNumber, Graphics g)
        {
            var xs = _width / (_itemCount+1.0f);
            var ys = _height / (_maxValue+5.0f);
            
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.Clear(Color.Black);
            
            // Title
            g.DrawString($"Rotate array with 3 reversals. {_itemCount} items by {_rotatePlaces} places", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_copyCount} copies, {_swapCount} swaps, {_steps} iterations.", _fontSmall, Brushes.WhiteSmoke, 10, 66);
            g.DrawString($"n = {_itemCount}; O(n) = {_itemCount}, 0 auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 84);
            
            // indicators
            var w = Math.Max(4, xs);
            g.FillRectangle(Brushes.Red, (_pointA-1)*xs, 0, w, _height);
            g.FillRectangle(Brushes.Fuchsia, (_pointB+1)*xs, 0, w, _height);
            g.FillRectangle(Brushes.Orange, (_pointC-1)*xs, 0, w, _height);
            g.FillRectangle(Brushes.Plum, (_pointD+1)*xs, 0, w, _height);
            
            g.FillRectangle(Brushes.Blue, _centre*xs, 0, w, _height);
            g.FillRectangle(Brushes.DarkCyan, (_itemCount - _rotatePlaces)*xs, 0, w, _height); // ccw rotation shown

            for (int i = 0; i < _itemCount; i++)
            {
                var top = _height - (_data[i] * ys);
                var height = _height - top;
                var width = Math.Max(2, (xs < 3) ? xs : xs-2);
                var x = i*xs;
                
                g.FillRectangle(Brushes.White, x, top, width, height);
            }
            
            return _iterator.MoveNext();
        }


        public bool GetAudioSamples(int videoFrameNumber, int audioFrameNumber, out byte[]? samples)
        {
            samples = null;
            return false;
        }


        IEnumerable<int> TrinityArrayRotation()
        {
            if (_itemCount < 2) yield break;
            
            _centre = _rotatePlaces % _itemCount;
            if (_centre == 0 || _centre == _itemCount-1) yield break; // no-op
            
            var left = _centre - 1; // length of left sub-array
            var right = _itemCount - _centre; // length of right sub-array

            _pointA = 0;
            _pointB = _pointA + left;
            _pointC = _pointB;
            _pointD = _pointC + right;

            int swap;
            var loop = left / 2;

            // reversals with overlaps
            while (loop-- > 0)
            {
                _pointB--;
                swap = _data[_pointB];
                _data[_pointB] = _data[_pointA];
                _data[_pointA] = _data[_pointC];
                _pointA++;
                
                _copyCount+=3;
                _swapCount++;
                yield return _steps++;

                _pointD--;
                _data[_pointC] = _data[_pointD];
                _data[_pointD] = swap;
                _pointC++;
                
                _copyCount+=2;
                _swapCount++;
                yield return _steps++;
            }

            loop = (_pointD - _pointC) / 2;

            // one less overlap
            while (loop-- > 0)
            {
                _pointD--;
                swap = _data[_pointC];
                _data[_pointC] = _data[_pointD];
                _data[_pointD] = _data[_pointA];
                _data[_pointA] = swap;
                _pointA++;
                _pointC++;
                
                _copyCount+=4;
                _swapCount++;
                yield return _steps++;
            }

            loop = (_pointD - _pointA) / 2;

            // last cycle
            while (loop-- > 0)
            {
                _pointD--;
                swap = _data[_pointA];
                _data[_pointA] = _data[_pointD];
                _data[_pointD] = swap;
                _pointA++;
                
                _copyCount+=3;
                _swapCount++;
                yield return _steps++;
            }

            // Done!
            yield return _steps;
            yield return _steps;
        }
    }
}