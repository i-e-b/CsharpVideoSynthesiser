# CsharpVideoSynthesiser

## What?

Programmatically output .mp4 files from C#.
This is used to generate a range of algorithm visualisations.

Result videos can be viewed at https://www.youtube.com/playlist?list=PLsb_7wpuMFt69ColcujH6LkhGg4984ZTk

## How?

Uses OpenCV and FFMpeg, with the libraries:

- https://www.nuget.org/packages/Emgu.CV -> https://github.com/emgucv/emgucv
- https://www.nuget.org/packages/FFMpegCore/  -> https://github.com/rosenbjerg/FFMpegCore

Generation of the videos is triggered by running its NUnit test

## Visualisations

The web has lots of visualisations -- especially of sorting algorithms,
but I am attempting to have a customised visualisation for each algorithm that
shows more of how it operates, so you will see markers, stack visualisations etc.

The visualisations can be a bit slower that others due to showing all the steps.

### Sorts

#### Bottom-up merge sort

https://www.youtube.com/watch?v=6F-RMaKMrks

This is my personal favorite.

This sort is relatively optimal for a compare based sort.

It requires an equal sized aux array, but keeps reads and writes separate and has close to sequential access,
which helps reduce CPU cache and memory bus conflicts, and uses cache lines relatively well.

#### Radix merge sort (MSD)

https://www.youtube.com/watch?v=4RII-rEc_qQ

Radix merge is somewhere between quick-sort and a merge sort.
This uses a separate source and destination buffer, which are swapped.
We keep a queue of 'done' and 'not-done' areas, partitioning the remaining spans as we go.

This means we avoid read/write conflicts on cache lines, but use `2n` auxiliary space.

Complexity is exactly `kn`, where `k` is the number of bits in the key, and `n` the number of items.

#### Radix in-place sort (MSD)

https://www.youtube.com/watch?v=tsVn5CT67T8

In place radix sort using most significant first, and a swapping system like quick sort.

This results in many more inspections than a buffer-copy strategy, but fewer writes.

#### Naïve iterative heap sort with min-heap

https://www.youtube.com/watch?v=QahrU49QjvM

In-place heap sort with 1024 random entries.

This sort is in the `O(n log n)` class, but is relatively slow compared to merge sort or the best case of quick sort.
It requires no substantial aux storage. The array is accessed in a very scattered way, which can cause cache and memory-line problems.

#### Access optimised heap sort

https://www.youtube.com/watch?v=H5xw4_GI0WA

In-place heap sort with 1024 random entries.

This sort is in the `O(n log n)` class, but is relatively slow compared to merge sort or the best case of quick sort.
It requires no substantial aux storage.

The array is accessed in a slightly less scattered way than the basic heap sort, and requires no swap phase at the end.

#### Simple recursive quick-sort

https://www.youtube.com/watch?v=hOWc9WAdhkc
https://www.youtube.com/watch?v=mUVmjoP0R4E

Recursive in-place quick sort using Hoare's scheme and best-of-3 partition with 1024 random entries.

This sort can be very quick, but easily degenerates to a very poor worst case.

It requires a stack of spans to be sorted, shown in orange.

#### Tournament sort

https://www.youtube.com/watch?v=pE6Axfw_LgE

This sort uses a heap-based sort window to create sorted sub-regions.
These regions are then merge-sorted together.

The example uses a very small 7 item window for visualisation.

Some visualisations show this sort working very quickly (or in very few steps).
I believe this is due to skipping the heap-sort stages. This algorithm does not 
seem to be particularly fast.



### Array rotation

#### 'Trinity' array rotation

https://www.youtube.com/watch?v=OXLjT_KsMR4

Rotate an array by an arbitrary amount in place.

This is a refinement of the older "3 reversals" method.

Works by reversing each side of the rotation point separately,
then reversing the entire array, but combines some of these swaps for a
significant reduction in the number of copies performed.

#### Array rotation by 3 reversals

https://www.youtube.com/watch?v=zWT8yAAEvr8

Rotate an array by an arbitrary amount in place.

Works by reversing each side of the rotation point separately,
then reversing the entire array.

This is simple and reasonably fast. It is outperformed by the
'trinity' rotation, which elides some of the inversions here.