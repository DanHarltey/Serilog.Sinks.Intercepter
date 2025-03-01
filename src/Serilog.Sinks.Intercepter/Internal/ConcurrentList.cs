using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Serilog.Events;

namespace Serilog.Sinks.Intercepter.Internal
{
    /// <summary>
    /// Represents a thread-safe first-in, first-out collection of objects.
    /// </summary>
    /// <typeparam name="T">Specifies the type of elements in the queue.</typeparam>
    /// <remarks>
    /// All public and protected members of <see cref="ConcurrentQueue{T}"/> are thread-safe and may be used
    /// concurrently from multiple threads.
    /// </remarks>
    internal class ConcurrentList //: IEnumerable<LogEvent>
    {
        // This implementation provides an unbounded, multi-producer multi-consumer queue
        // that supports the standard Enqueue/TryDequeue operations, as well as support for
        // snapshot enumeration (GetEnumerator, ToArray, CopyTo), peeking, and Count/IsEmpty.
        // It is composed of a linked list of bounded ring buffers, each of which has a head
        // and a tail index, isolated from each other to minimize false sharing.  As long as
        // the number of elements in the queue remains less than the size of the current
        // buffer (Segment), no additional allocations are required for enqueued items.  When
        // the number of items exceeds the size of the current segment, the current segment is
        // "frozen" to prevent further enqueues, and a new segment is linked from it and set
        // as the new tail segment for subsequent enqueues.  As old segments are consumed by
        // dequeues, the head reference is updated to point to the segment that dequeuers should
        // try next.  To support snapshot enumeration, segments also support the notion of
        // preserving for observation, whereby they avoid overwriting state as part of dequeues.
        // Any operation that requires a snapshot results in all current segments being
        // both frozen for enqueues and preserved for observation: any new enqueues will go
        // to new segments, and dequeuers will consume from the existing segments but without
        // overwriting the existing data.

        /// <summary>Initial length of the segments used in the queue.</summary>
        private const int InitialSegmentLength = 32;
        /// <summary>
        /// Maximum length of the segments used in the queue.  This is a somewhat arbitrary limit:
        /// larger means that as long as we don't exceed the size, we avoid allocating more segments,
        /// but if we do exceed it, then the segment becomes garbage.
        /// </summary>
        private const int MaxSegmentLength = 1024 * 1024;

        /// <summary>
        /// Lock used to protect cross-segment operations, including any updates to <see cref="_tail"/> or <see cref="_head"/>
        /// and any operations that need to get a consistent view of them.
        /// </summary>
        private readonly object _crossSegmentLock;
        /// <summary>The current tail segment.</summary>
        private volatile ConcurrentListSegment _tail;
        /// <summary>The current head segment.</summary>
        private volatile ConcurrentListSegment _head; // SOS's ThreadPool command depends on this name

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentQueue{T}"/> class.
        /// </summary>
        public ConcurrentList()
        {
            _crossSegmentLock = new object();
            _tail = _head = new ConcurrentListSegment(InitialSegmentLength);
        }

        //    /// <summary>Returns an enumerator that iterates through a collection.</summary>
        //    /// <returns>An <see cref="IEnumerator"/> that can be used to iterate through the collection.</returns>
        //    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<LogEvent>)this).GetEnumerator();

        //    bool TryAdd(LogEvent item)
        //    {
        //        Enqueue(item);
        //        return true;
        //    }

        //    /// <summary>Returns an enumerator that iterates through the <see cref="ConcurrentQueue{T}"/>.</summary>
        //    /// <returns>An enumerator for the contents of the <see
        //    /// cref="ConcurrentQueue{T}"/>.</returns>
        //    /// <remarks>
        //    /// The enumeration represents a moment-in-time snapshot of the contents
        //    /// of the queue.  It does not reflect any updates to the collection after
        //    /// <see cref="GetEnumerator"/> was called.  The enumerator is safe to use
        //    /// concurrently with reads from and writes to the queue.
        //    /// </remarks>
        //    public IEnumerator<LogEvent> GetEnumerator()
        //    {
        //        ////SnapForObservation(out ConcurrentQueueSegment<T> head, out int headHead, out ConcurrentQueueSegment<T> tail, out int tailTail);
        //        ////return Enumerate(head, headHead, tail, tailTail);
        //    }

        //    /// <summary>Gets the item stored in the <paramref name="i"/>th entry in <paramref name="segment"/>.</summary>
        //    private static T GetItemWhenAvailable(ConcurrentQueueSegment<T> segment, int i)
        //    {
        //        Debug.Assert(segment._preservedForObservation);

        //        // Get the expected value for the sequence number
        //        int expectedSequenceNumberAndMask = (i + 1) & segment._slotsMask;

        //        // If the expected sequence number is not yet written, we're still waiting for
        //        // an enqueuer to finish storing it.  Spin until it's there.
        //        if ((segment._slots[i].SequenceNumber & segment._slotsMask) != expectedSequenceNumberAndMask)
        //        {
        //            SpinWait spinner = default;
        //            while ((Volatile.Read(ref segment._slots[i].SequenceNumber) & segment._slotsMask) != expectedSequenceNumberAndMask)
        //            {
        //                spinner.SpinOnce();
        //            }
        //        }

        //        // Return the value from the slot.
        //        return segment._slots[i].Item!;
        //    }

        //    private static IEnumerator<T> Enumerate(ConcurrentQueueSegment<T> head, int headHead, ConcurrentQueueSegment<T> tail, int tailTail)
        //    {
        //        Debug.Assert(head._preservedForObservation);
        //        Debug.Assert(head._frozenForEnqueues);
        //        Debug.Assert(tail._preservedForObservation);
        //        Debug.Assert(tail._frozenForEnqueues);

        //        // Head segment.  We've already marked it as not accepting any more enqueues,
        //        // so its tail position is fixed, and we've already marked it as preserved for
        //        // enumeration (before we grabbed its head), so we can safely enumerate from
        //        // its head to its tail.
        //        int headTail = (head == tail ? tailTail : Volatile.Read(ref head._headAndTail.Tail)) - head.FreezeOffset;
        //        if (headHead < headTail)
        //        {
        //            headHead &= head._slotsMask;
        //            headTail &= head._slotsMask;

        //            if (headHead < headTail)
        //            {
        //                for (int i = headHead; i < headTail; i++) yield return GetItemWhenAvailable(head, i);
        //            }
        //            else
        //            {
        //                for (int i = headHead; i < head._slots.Length; i++) yield return GetItemWhenAvailable(head, i);
        //                for (int i = 0; i < headTail; i++) yield return GetItemWhenAvailable(head, i);
        //            }
        //        }

        //        // We've enumerated the head.  If the tail is the same, we're done.
        //        if (head != tail)
        //        {
        //            // Each segment between head and tail, not including head and tail.  Since there were
        //            // segments before these, for our purposes we consider it to start at the 0th element.
        //            for (ConcurrentQueueSegment<T> s = head._nextSegment!; s != tail; s = s._nextSegment!)
        //            {
        //                Debug.Assert(s._preservedForObservation, "Would have had to been preserved as a segment part of enumeration");
        //                Debug.Assert(s._frozenForEnqueues, "Would have had to be frozen for enqueues as it's intermediate");

        //                int sTail = s._headAndTail.Tail - s.FreezeOffset;
        //                for (int i = 0; i < sTail; i++)
        //                {
        //                    yield return GetItemWhenAvailable(s, i);
        //                }
        //            }

        //            // Enumerate the tail.  Since there were segments before this, we can just start at
        //            // its beginning, and iterate until the tail we already grabbed.
        //            tailTail -= tail.FreezeOffset;
        //            for (int i = 0; i < tailTail; i++)
        //            {
        //                yield return GetItemWhenAvailable(tail, i);
        //            }
        //        }
        //    }

        //    /// <summary>Adds an object to the end of the <see cref="ConcurrentQueue{T}"/>.</summary>
        //    /// <param name="item">
        //    /// The object to add to the end of the <see cref="ConcurrentQueue{T}"/>.
        //    /// The value can be a null reference (Nothing in Visual Basic) for reference types.
        //    /// </param>
        public bool TryEnqueue(LogEvent item)
        {
            var tail = _tail;

            var result = tail.TryEnqueue(item);

            if (result == Result.Success)
            {
                return true;
            }
            else if (result == Result.Locked)
            {
                return false;
            }
            else
            {
                // If we're unable to, we need to take a slow path that will
                // try to add a new tail segment.
                return TryEnqueueSlow(tail, item);
            }
        }

        /// <summary>Adds to the end of the queue, adding a new segment if necessary.</summary>
        private bool TryEnqueueSlow(ConcurrentListSegment  tail, LogEvent item)
        {
            while (true)
            {
                // If we were unsuccessful, take the lock so that we can compare and manipulate
                // the tail.  Assuming another enqueuer hasn't already added a new segment,
                // do so, then loop around to try enqueueing again.
                lock (_crossSegmentLock)
                {
                    if (tail == _tail)
                    {
                        // Make sure no one else can enqueue to this segment.
                        if(tail.CompleteAdding)
                        {
                            return false;
                        }

                        // We determine the new segment's length based on the old length.
                        // In general, we double the size of the segment, to make it less likely
                        // that we'll need to grow again.  However, if the tail segment is marked
                        // as preserved for observation, something caused us to avoid reusing this
                        // segment, and if that happens a lot and we grow, we'll end up allocating
                        // lots of wasted space.  As such, in such situations we reset back to the
                        // initial segment length; if these observations are happening frequently,
                        // this will help to avoid wasted memory, and if they're not, we'll
                        // relatively quickly grow again to a larger size.
                        int nextSize =  Math.Min(tail.Capacity * 2, MaxSegmentLength);
                        var newTail = new ConcurrentListSegment(nextSize);

                        // Hook up the new tail.
                        tail._nextSegment = newTail;
                        _tail = newTail;
                    }

                    tail = _tail;
                }

                // Try to append to the existing tail.
                var result = tail.TryEnqueue(item);

                if (result == Result.Success)
                {
                    return true;
                }
                else if (result == Result.Locked)
                {
                    return false;
                }
            }
        }
    }
}
