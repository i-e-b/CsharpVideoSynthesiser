using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames.SortMovies
{
    public class TournamentSortMovieGen : IVideoGenerator, IDisposable
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
        
        private readonly HeapSlot[] _heap;
        private int _left;
        private int _right;

        public TournamentSortMovieGen(int width, int height, string name, byte[] data)
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
            _heap = new HeapSlot[7];
            for (int i = 0; i < 7; i++) { _heap[i]=new HeapSlot{Locked = false, Occupied = false, Value = (byte)((i+1)*16)}; }
            
            _iterator = TournamentSort().GetEnumerator();
        }

        public bool DrawFrame(int frameNumber, Graphics g)
        {
            if (frameNumber > 5_000) return false; // reduce for testing

            var mid = _height * 0.5f;
            var xs = _width / (_itemCount + 1.0f);
            var ys = mid / 255.0f;

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            // group
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.Blue, 0, 0, _width, mid); // top shows data, bottom shows stack span "flame graph"
            
            // indicators
            //var pp = mid - (_pivotValue * ys);
            //g.FillRectangle(Brushes.Aqua, _activeSpanLeft*xs, mid + 1, _activeSpanWidth*xs, 10); // active span
            //g.FillRectangle(Brushes.DarkCyan, _pivotPoint*xs, pp, 4, mid - pp); // pivot
            g.FillRectangle(Brushes.Cyan, _left*xs, 0, 4, mid); // cursor left
            g.FillRectangle(Brushes.LightGreen, _right*xs, 0, 4, mid); // cursor right
            
            // draw heap
            var pos = 20;
            foreach (var slot in _heap)
            {
                var color = Brushes.Brown;
                if (slot.Occupied) color = Brushes.Orange;
                if (slot.Locked) color = Brushes.Gainsboro;
                g.FillRectangle(color, pos, mid, 8, (slot.Value+10) * ys * 0.75f); // active span
                pos += 8;
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
            g.DrawString($"Tournament sort. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_compareCount} compares, {_copyCount} copies, {_swapCount} swaps", _fontSmall, Brushes.WhiteSmoke, 10, 70);
            g.DrawString($"{_steps} iterations.", _fontSmall, Brushes.WhiteSmoke, 10, 90);
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

        IEnumerable<int> TournamentSort()
        {
            var len = _itemCount;
            if (len < 4) yield break;
            
            // This sort uses a simple fixed-size heap to build a set
            // of sorted partitions. Whenever the heap wouldn't give us
            // a correctly sorted sub-set, we start a new partition.
            // Those partitions are then merged into the final output.
            // This gives us a combination of heap and merge sorts.

            // For this example, we use a very small (7 item) heap, to make it easier to visualise
            _left = 0;
            byte lastWritten = 0;
            int i;
            
            // First, fill the bottom of our heap
            for (i = 0; i < 4; i++)
            {
                _right = i;
                FillSlot(B1 + i, i, lastWritten);
                yield return _steps++;
            }
            
            // Now, merge up and feed data in
            for (; i < len; i++)
            {
                _right = i;
                
                var lastFreeSlot = -1;
                
                // heap up, bubbling items onto the main array
                if (Occupied(A0)) { PopTop(out lastWritten);  yield return _steps++;}
                if (Occupied(M1) && Occupied(M2)) { MergeUp(M1, M2, A0, out _);  yield return _steps++;}
                
                     if (IsMobile(B1) && Occupied(B2) && !Occupied(M1)) {MergeUp(B1, B2, M1, out lastFreeSlot); yield return _steps++;}
                else if (Occupied(B1) && IsMobile(B2) && !Occupied(M1)) {MergeUp(B1, B2, M1, out lastFreeSlot); yield return _steps++;}
                else if (IsMobile(B3) && Occupied(B4) && !Occupied(M2)) {MergeUp(B3, B4, M2, out lastFreeSlot); yield return _steps++;}
                else if (Occupied(B3) && IsMobile(B4) && !Occupied(M2)) {MergeUp(B3, B4, M2, out lastFreeSlot); yield return _steps++;}
                
                if (lastFreeSlot < B1) break; // no space -- everything is locked
                if (Occupied(lastFreeSlot)) break; // this shouldn't happen?
                FillSlot(lastFreeSlot, i, lastWritten); Bubble(lastFreeSlot,lastFreeSlot>>1,A0); yield return _steps++;
            }
            
            // TODO: empty the heap; if not at end, partition and re-merge
        }

        private void Bubble(int slot1, int slot2, int slot3)
        {
            if (!IsMobile(slot1)) return;
            if (IsEmpty(slot2) || CompareHeap(slot2, slot1)) return;
            SwapHeap(slot1, slot2);
            if (IsEmpty(slot3) || CompareHeap(slot3, slot2)) return;
            SwapHeap(slot2, slot3);
        }

        private bool IsEmpty(int slot)
        {
            return !_heap[slot].Occupied;
        }

        private void SwapHeap(int slot1, int slot2)
        {
            _copyCount+=3;
            _swapCount++;
            var tmp = _heap[slot1];
            _heap[slot1] = _heap[slot2];
            _heap[slot2] = tmp;
        }

        private void MergeUp(int srcA, int srcB, int dst, out int moved)
        {
            if (IsMobile(srcA) && CompareHeap(srcA, srcB))
            {
                moved = srcA;
                _heap[dst] = _heap[srcA];
                _heap[srcA] = new HeapSlot{Locked = false, Occupied = false, Value = 0};
            }
            else if (IsMobile(srcB))
            {
                moved = srcB;
                _heap[dst] = _heap[srcB];
                _heap[srcB] = new HeapSlot{Locked = false, Occupied = false, Value = 0};
            }
            else moved = -1;
        }

        private bool Occupied(int slot)
        {
            return _heap[slot].Occupied;
        }
        
        private bool IsMobile(int slot)
        {
            return _heap[slot].Occupied && (!_heap[slot].Locked);
        }

        private bool IsSlotFree(int slot)
        {
            return !(_heap[slot].Occupied);
        }

        private void PopTop(out byte lastWritten)
        {
            lastWritten = _data[_left++] = _heap[A0].Value;
            _heap[A0] = new HeapSlot{Locked = false, Occupied = false, Value = 0};
        }

        private void FillSlot(int slot, int idx, byte lastWrittenValue)
        {
            var newValue = _data[idx];
            var shouldLock = CompareValue(newValue, lastWrittenValue); // lock this if we can't possibly put it in order in the current partition
            _heap[slot] = new HeapSlot {Locked = shouldLock, Occupied = true, Value = newValue};
            _data[idx] = 0; // not required, but makes visuals clearer
        }

        const int A0 = 0;
        const int M1 = 1;
        const int M2 = 2;
        const int B1 = 3;
        const int B2 = 4;
        const int B3 = 5;
        const int B4 = 6;
        
        private bool CompareValue(byte i, byte j)
        {
            _compareCount++;
            return i < j;
        }
        
        private bool CompareHeap(int i, int j)
        {
            _compareCount++;
            return _heap[i].Value < _heap[j].Value;
        }

        public void Dispose()
        {
            _font.Dispose();
            _fontSmall.Dispose();
            _iterator.Dispose();
        }
    }

    public struct HeapSlot
    {
        public byte Value;
        public bool Occupied;
        public bool Locked;
    }
}