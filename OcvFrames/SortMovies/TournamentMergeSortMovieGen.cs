﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OcvFrames.SortMovies
{
    public class TournamentMergeSortMovieGen : IVideoGenerator, IDisposable
    {
        private readonly Font _font;
        private readonly Font _fontSmall;
        private readonly Font _fontTiny;

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
        private int _mergeMiddle, _mergeLeft, _mergeRight;
        
        private string _phase = "starting";
        private readonly byte[] _mergeTemp;
        private int _mergeTarget;

        public TournamentMergeSortMovieGen(int width, int height, string name, byte[] data)
        {
            _width = width;
            _height = height;
            _name = name;
            _itemCount = data.Length;
            _data = data;
            _mergeTemp = new byte[data.Length];
            _font = new Font("Dave", 24);
            _fontSmall = new Font("Dave", 18);
            _fontTiny = new Font("Dave", 9);

            _estComplex = (int) (Math.Log2(_itemCount) * _itemCount);

            _steps = 0;
            _heap = new HeapSlot[7];
            for (int i = 0; i < 7; i++) { _heap[i]=new HeapSlot{Locked = false, Occupied = false, Value = (byte)(0)}; }
            
            _iterator = TournamentSort().GetEnumerator();
        }

        public bool DrawFrame(int videoFrameNumber, Graphics g)
        {
            //if (frameNumber > 6000) return false; // reduce for testing

            var mid = _height * 0.5f;
            var xs = _width / (_itemCount + 1.0f);

            g.CompositingQuality = CompositingQuality.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;

            // group
            g.Clear(Color.Black);
            g.FillRectangle(Brushes.Blue, 0, 0, _width, mid); // top shows data, bottom shows stack span "flame graph"
            
            // indicators
            g.FillRectangle(Brushes.Cyan, _left*xs, 0, 4, mid); // cursor left
            g.FillRectangle(Brushes.LightGreen, _right*xs, 0, 4, mid); // cursor right
            
            g.FillRectangle(Brushes.Red, _mergeMiddle*xs, 0, 4, mid); // merge point
            g.FillRectangle(Brushes.Red, _mergeTarget*xs, mid, 4, mid); // merge target
            g.FillRectangle(Brushes.Gold, _mergeLeft*xs, 0, 4, mid); // merge sources
            g.FillRectangle(Brushes.Gold, _mergeRight*xs, 0, 4, mid);
            
            // draw heap
            g.DrawString($"             {Hs(A0)}", _fontTiny, Brushes.WhiteSmoke, 10, mid);
            g.DrawString($"    {Hs(M1)}          {Hs(M2)}", _fontTiny, Brushes.WhiteSmoke, 10, mid+11);
            g.DrawString($"{Hs(B1)} {Hs(B2)} {Hs(B3)} {Hs(B4)}", _fontTiny, Brushes.WhiteSmoke, 10, mid+22);


            // draw data points
            var ys = mid / 280.0f;
            for (int i = 0; i < _itemCount; i++)
            {
                var a = mid - (_data[i] * ys);
                var x = i * xs;
                g.FillRectangle(Brushes.White, x - 2, a - 2, 4, 4);
            }
            
            // draw merge data points
            for (int i = 0; i < _itemCount; i++)
            {
                var a = _height - (_mergeTemp[i] * ys);
                var x = i * xs;
                g.FillRectangle(Brushes.OldLace, x - 2, a - 2, 4, 4);
            }

            // Title
            g.DrawString($"Tournament sort with merge. {_itemCount} items ({_name})", _font, Brushes.WhiteSmoke, 10, 24);
            g.DrawString($"{_compareCount} compares, {_copyCount} copies, {_swapCount} swaps", _fontSmall, Brushes.WhiteSmoke, 10, 70);
            g.DrawString($"{_steps} iterations ({_phase}).", _fontSmall, Brushes.WhiteSmoke, 10, 90);
            g.DrawString($"n = {_itemCount}; O(n log n) = {_estComplex}, 'k+n' auxiliary space", _fontSmall, Brushes.WhiteSmoke, 10, 110);


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

        private string Hs(int index)
        {
            var h = _heap[index];
            if (!h.Occupied) return "   xx   "; // not loaded
            var hex = h.Value.ToString("X2");
            if (h.Locked) return $"  [{hex}]  "; // locked
            return $"   {hex}   "; // mobile
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
            _right = 0;
            _mergeLeft = _mergeRight = _mergeMiddle = -1; // no merge point yet
            int lastWritten = -1;

            while (true)
            {
                // PREPHASE //
                _phase = "pre-phase: fill";
                
                // First, fill the entire heap from the incoming array
                int i;
                for (i = 0; i <= B4; i++)
                {
                    FillSlot(i, lastWritten);
                    yield return _steps++;
                }

                UnlockHeap();
                _phase = "pre-phase: order";
                // Now, enforce the heap property on our heap
                // TODO: Combine sort and insert to a proper heap.
                
                if (Heap_LeftIsLess(B1, M1)) { SwapHeap(B1, M1); }
                if (Heap_LeftIsLess(B2, M1)) { SwapHeap(B2, M1); }
                if (Heap_LeftIsLess(B3, M2)) { SwapHeap(B3, M2); }
                if (Heap_LeftIsLess(B4, M2)) { SwapHeap(B4, M2); }
                
                if (Heap_LeftIsLess(M1, A0)) {
                    SwapHeap(M1, A0);
                    if (Heap_LeftIsLess(B1, M1)) { SwapHeap(B1, M1); }
                    if (Heap_LeftIsLess(B2, M1)) { SwapHeap(B2, M1); }
                }
                
                if (Heap_LeftIsLess(M2, A0)) {
                    SwapHeap(M2, A0);
                    if (Heap_LeftIsLess(B3, M2)) { SwapHeap(B3, M2); }
                    if (Heap_LeftIsLess(B4, M2)) { SwapHeap(B4, M2); }
                }
                    
                yield return _steps++;
                // Double check that the sort has worked.
                // This is not part of the algorithm
                CheckHeapOrder();
                                                
                // MAIN PHASE //
                _phase = "main-phase";
                yield return _steps;
                
                // Now, pop from the heap, bubble up and feed data into the bottom of the heap.
                while (true)
                {
                    // heap up, bubbling items onto the main array
                    if (Occupied(A0))
                    {
                        PopTop(out lastWritten);
                        yield return _steps++;
                    }
                    else
                    {
                        break; // The heap is empty, or has only locked items left?
                    }

                    // we now have a gap at the top,
                    // and we want a gap at the bottom.
                    // but we must maintain the heap property

                    var lastFreeSlot = BubbleUp(A0, M1, M2);
                    yield return _steps++;
                    if (lastFreeSlot == M1) lastFreeSlot = BubbleUp(M1, B1, B2);
                    else if (lastFreeSlot == M2) lastFreeSlot = BubbleUp(M2, B3, B4);
                    else break; // couldn't bubble -- heap is exhausted?

                    yield return _steps++;

                    if (lastFreeSlot >= B1) // bottom row is not *all* locked
                    {
                        FillSlot(lastFreeSlot, lastWritten);
                        yield return _steps++;

                        // now re-apply the heap property
                        Order(lastFreeSlot, ParentOf(lastFreeSlot), A0);
                        yield return _steps++;
                    }

                    yield return _steps;
                }

                _phase = "post-tournament phase";
                yield return _steps;

                if (_mergeMiddle > 0) // we have a left set that needs merging with the set we just created
                {
                    _phase = "merge phase";
                    yield return _steps;

                    // 2 spans to merge: (0 .. _mergeMiddle) and (_mergeMiddle .. _left)
                    _mergeLeft = 0;
                    _mergeRight = _mergeMiddle + 1;
                    var end = Math.Min(_data.Length - 1, _left - 1);

                    if (_mergeRight >= _data.Length)
                    {
                        _mergeMiddle = _mergeLeft = _mergeRight = -1;
                        _phase = "end of data";
                        yield return _steps;
                        yield return _steps;
                        yield break;
                    }

                    // Read smallest from the two partial results, write across to mergeTemp
                    _mergeTarget = 0;
                    while (_mergeLeft <= _mergeMiddle && _mergeRight <= end)
                    {
                        if (Data_LeftIsLess(_mergeLeft, _mergeRight))
                        {
                            _mergeTemp[_mergeTarget++] = _data[_mergeLeft];
                            _data[_mergeLeft] = 0; // not required, just makes the visuals clearer
                            _mergeLeft++;
                            _copyCount++;
                        }
                        else
                        {
                            _mergeTemp[_mergeTarget++] = _data[_mergeRight];
                            _data[_mergeRight] = 0; // not required, just makes the visuals clearer
                            _mergeRight++;
                            _copyCount++;
                        }
                        yield return _steps++;
                    }
                    // clean up left-overs
                    while (_mergeLeft <= _mergeMiddle)
                    {
                        _mergeTemp[_mergeTarget++] = _data[_mergeLeft];
                        _data[_mergeLeft] = 0; // not required, just makes the visuals clearer
                        _mergeLeft++;
                        _copyCount++;
                        yield return _steps++;
                    }
                    while (_mergeRight <= end)
                    {
                        _mergeTemp[_mergeTarget++] = _data[_mergeRight];
                        _data[_mergeRight] = 0; // not required, just makes the visuals clearer
                        _mergeRight++;
                        _copyCount++;
                        yield return _steps++;
                    }
                    
                    // reset for next tournament
                    _mergeMiddle = _left - 1;
                    _mergeLeft = _mergeRight = -1;
                    lastWritten = -1;
                    
                    // Copy merge temp back
                    for (int j = 0; j <= end; j++)
                    {
                        _mergeTarget = j;
                        _data[j] = _mergeTemp[j];
                        _mergeTemp[j] = 0;
                        _copyCount++;
                        yield return _steps++;
                    }
                    
                    UnlockHeap();
                    yield return _steps++;
                }
                else // there is no other set. We're either finished or need to start a second set
                {
                    _mergeMiddle = _left - 1;
                    yield return _steps++;
                    lastWritten = -1;
                    UnlockHeap();
                    yield return _steps++;
                    // and loop again
                }
            }
        }

        private void CheckHeapOrder()
        {
            var err = new Exception($"Heap is out of order: {Hs(A0)} / {Hs(M1)} {Hs(M2)} / {Hs(B1)} {Hs(B2)} {Hs(B3)} {Hs(B4)}");
            static int Map(HeapSlot hs) => hs.Occupied ? hs.Value : int.MaxValue;
            int ValueAt(int idx) => Map(_heap[idx]);

            if (ValueAt(A0) > ValueAt(M1)) throw err;
            if (ValueAt(A0) > ValueAt(M2)) throw err;
            
            if (ValueAt(M1) > ValueAt(B1)) throw err;
            if (ValueAt(M1) > ValueAt(B2)) throw err;
            
            if (ValueAt(M2) > ValueAt(B3)) throw err;
            if (ValueAt(M2) > ValueAt(B4)) throw err;
        }

        private void UnlockHeap()
        {
            for (int i = 0; i <= B4; i++)
            {
                _heap[i].Locked = false;
            }
        }

        /// <summary>
        /// Return the slot 'above' this one
        /// </summary>
        private int ParentOf(int slot)
        {
            return (slot - 1) >> 1;
        }

        private int BubbleUp(int target, int src1, int src2)
        {
            // Check we can bubble at all
            if (_heap[target].Occupied) return -1;
            if (!IsMobile(src1) && !IsMobile(src2)) return -1;
            
            // Only one available:
            if (IsMobile(src1) && !IsMobile(src2)) { SwapHeap(src1, target); return src1; }
            if (IsMobile(src2) && !IsMobile(src1)) { SwapHeap(src2, target); return src2; }
            
            // Both available, pick smallest
            if (Heap_LeftIsLess(src1, src2)) { SwapHeap(src1, target); return src1; }
            SwapHeap(src2, target); return src2;
        }

        /// <summary>
        /// Rearrange slots 1,2,3 so that slot 3 has the lowest value and slot 1 the highest.
        /// No values are removed.
        /// Returns true if any swaps are made.
        /// </summary>
        private void Order(int high, int mid, int low)
        {
            if (IsUnlocked(high) && Heap_LeftIsLess(high, mid)) { SwapHeap(mid, high); }
            if (Heap_LeftIsLess(mid, low)) { SwapHeap(low, mid); }
        }

        private bool IsUnlocked(int slot)
        {
            return !_heap[slot].Locked;
        }

        private void SwapHeap(int slot1, int slot2)
        {
            _copyCount+=3;
            _swapCount++;
            var tmp = _heap[slot1];
            _heap[slot1] = _heap[slot2];
            _heap[slot2] = tmp;
        }

        private bool Occupied(int slot)
        {
            return _heap[slot].Occupied;
        }
        
        private bool IsMobile(int slot)
        {
            return _heap[slot].Occupied && (!_heap[slot].Locked);
        }

        private void PopTop(out int lastWritten)
        {
            lastWritten = _data[_left++] = _heap[A0].Value;
            _heap[A0] = new HeapSlot{Locked = false, Occupied = false, Value = 0};
        }

        private void FillSlot(int slot, int lastWrittenValue)
        {
            if (_right >= _data.Length) return; // exhausted input data
            if (_heap[slot].Occupied) return; // can't be filled
            var newValue = _data[_right];
            var shouldLock = CompareValue(newValue, lastWrittenValue); // lock this if we can't possibly put it in order in the current partition
            _heap[slot] = new HeapSlot {Locked = shouldLock, Occupied = true, Value = newValue};
            _data[_right] = 0; // not required, but makes visuals clearer
            _right++; // we absorbed another value, step forward
        }

        const int A0 = 0;
        const int M1 = 1;
        const int M2 = 2;
        const int B1 = 3;
        const int B2 = 4;
        const int B3 = 5;
        const int B4 = 6;
        
        private bool CompareValue(byte i, int j)
        {
            _compareCount++;
            return i < j;
        }
        
        /// <summary>
        /// Return true if the value at the left index is less than at the right
        /// </summary>
        private bool Heap_LeftIsLess(int left, int right)
        {
            static int Map(HeapSlot hs) => hs.Occupied ? hs.Value : int.MaxValue;
            
            _compareCount++;
            return Map(_heap[left]) < Map(_heap[right]);
        }
        
        /// <summary>
        /// Return true if the value at the left index is less than at the right
        /// </summary>
        private bool Data_LeftIsLess(int left, int right)
        {
            if (left == right) return false;
            _compareCount++;
            return _data[left] <= _data[right];
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