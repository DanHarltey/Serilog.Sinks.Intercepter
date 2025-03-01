using Serilog.Events;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Serilog.Sinks.Intercepter.Internal
{/// <summary>
 /// Provides a multi-producer, multi-consumer thread-safe bounded segment.  When the queue is full,
 /// enqueues fail and return false.  When the queue is empty, dequeues fail and return null.
 /// These segments are linked together to form the unbounded <see cref="ConcurrentQueue{T}"/>.
 /// </summary>
    internal sealed class ConcurrentListSegment
    {
        // Segment design is inspired by the algorithm outlined at:
        // http://www.1024cores.net/home/lock-free-algorithms/queues/bounded-mpmc-queue

        /// <summary>The array of items in this queue.  Each slot contains the item in that slot and its "sequence number".</summary>
        internal readonly LogEvent[] _slots; // SOS's ThreadPool command depends on this name
        /// <summary>Mask for quickly accessing a position within the queue's array.</summary>
        internal readonly int _slotsMask;
        /// <summary>The head and tail positions, with padding to help avoid false sharing contention.</summary>
        /// <remarks>Dequeuing happens from the head, enqueuing happens at the tail.</remarks>
        internal PaddedHeadAndTail _headAndTail; // mutable struct: do not make this readonly

        ///// <summary>Indicates whether the segment has been marked such that dequeues don't overwrite the removed data.</summary>
        //internal bool _preservedForObservation;
        ///// <summary>Indicates whether the segment has been marked such that no additional items may be enqueued.</summary>
        //internal bool _frozenForEnqueues;
#pragma warning disable 0649 // some builds don't assign to this field
        /// <summary>The segment following this one in the queue, or null if this segment is the last in the queue.</summary>
        internal ConcurrentListSegment? _nextSegment; // SOS's ThreadPool command depends on this name
#pragma warning restore 0649

        /// <summary>Creates the segment.</summary>
        /// <param name="boundedLength">
        /// The maximum number of elements the segment can contain.  Must be a power of 2.
        /// </param>
        internal ConcurrentListSegment(int boundedLength)
        {
            // Validate the length
            Debug.Assert(boundedLength >= 2, $"Must be >= 2, got {boundedLength}");
            Debug.Assert((boundedLength & (boundedLength - 1)) == 0, $"Must be a power of 2, got {boundedLength}");

            // Initialize the slots and the mask.  The mask is used as a way of quickly doing "% _slots.Length",
            // instead letting us do "& _slotsMask".
            _slots = new LogEvent[boundedLength];
            _slotsMask = boundedLength - 1;
        }

        public bool CompleteAdding => (Volatile.Read(ref _headAndTail.Tail) & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK;
        /// <summary>Gets the number of elements this segment can store.</summary>
        internal int Capacity => _slots.Length;

        /// <summary>Gets the "freeze offset" for this segment.</summary>
        //internal int FreezeOffset => 0x8000;
        private const int COMPLETE_ADDING_MASK = unchecked((int)0x80000000);

        ////private const int NEW_SEGMENT_MASK = unchecked((int)0x40000000);


        ////internal bool TryEnsureFrozenForEnqueues() // must only be called while queue's segment lock is held
        ////{
        ////    var currentTail = Volatile.Read(ref _headAndTail.Tail);

        ////    var inverseMask = 0;
        ////    inverseMask ^= _slots.Length;

        ////    if (inverseMask == NEW_SEGMENT_MASK)
        ////    {
        ////        throw new InvalidOperationException();
        ////    }

        ////    if (inverseMask != 0)
        ////    {
        ////        return false;
        ////    }

        ////    Interlocked.Add(ref _headAndTail.Tail, NEW_SEGMENT_MASK);
        ////    return true;
        ////    ////var currentTail = Volatile.Read(ref _headAndTail.Tail);

        ////    ////// Loop in case of contention...
        ////    ////while (true)
        ////    ////{
        ////    ////    var inverseMask = 0;
        ////    ////    inverseMask ^= _slots.Length;

        ////    ////    if (inverseMask == NEW_SEGMENT_MASK)
        ////    ////    {
        ////    ////        throw new InvalidOperationException();
        ////    ////    }

        ////    ////    if (inverseMask != 0)
        ////    ////    {
        ////    ////        return false;
        ////    ////    }

        ////    ////    var actualTail = Interlocked.CompareExchange(ref _headAndTail.Tail, currentTail | NEW_SEGMENT_MASK, currentTail);

        ////    ////    if (actualTail == currentTail)
        ////    ////    {
        ////    ////        return true;
        ////    ////    }

        ////    ////    currentTail = actualTail;
        ////    ////}
        ////}

        /// <summary>
        /// Ensures that the segment will not accept any subsequent enqueues that aren't already underway.
        /// </summary>
        /// <remarks>
        /// When we mark a segment as being frozen for additional enqueues,
        /// we set the <see cref="_frozenForEnqueues"/> bool, but that's mostly
        /// as a small helper to avoid marking it twice.  The real marking comes
        /// by modifying the Tail for the segment, increasing it by this
        /// <see cref="FreezeOffset"/>.  This effectively knocks it off the
        /// sequence expected by future enqueuers, such that any additional enqueuer
        /// will be unable to enqueue due to it not lining up with the expected
        /// sequence numbers.  This value is chosen specially so that Tail will grow
        /// to a value that maps to the same slot but that won't be confused with
        /// any other enqueue/dequeue sequence number.
        /// </remarks>
        internal void TryCompleteAdding() // must only be called while queue's segment lock is held
        {
            var currentTail = _headAndTail.Tail;

            if((currentTail & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK)
            {
                return;
            }

            Interlocked.Add(ref _headAndTail.Tail, COMPLETE_ADDING_MASK);

            ////

            ////// Loop in case of contention...
            ////while (true)
            ////{
            ////    var inverseMask = 0;
            ////    inverseMask ^= _slots.Length;

            ////    if (inverseMask == COMPLETE_ADDING_MASK)
            ////    {
            ////        return true;
            ////    }

            ////    if (inverseMask != 0)
            ////    {
            ////        return false;
            ////    }

            ////    var actualTail = Interlocked.CompareExchange(ref _headAndTail.Tail, currentTail| COMPLETE_ADDING_MASK, currentTail);

            ////    if (actualTail == currentTail)
            ////    {
            ////        return true;
            ////    }

            ////    currentTail = actualTail;
            ////}
        }

        /// <summary>
        /// Attempts to enqueue the item.  If successful, the item will be stored
        /// in the queue and true will be returned; otherwise, the item won't be stored, and false
        /// will be returned.
        /// </summary>
        public Result TryEnqueue(LogEvent item)
        {
            var slots = _slots;

            var currentTail = Volatile.Read(ref _headAndTail.Tail);

            // Loop in case of contention...
            while (true)
            {
                if ((currentTail & COMPLETE_ADDING_MASK) == COMPLETE_ADDING_MASK)
                {
                    return Result.Locked;
                }

                var slotsIndex = currentTail + 1;

                if (slotsIndex >= slots.Length)
                {
                    return Result.Full;
                }

                var actualTail = Interlocked.CompareExchange(ref _headAndTail.Tail, slotsIndex, currentTail);

                if (actualTail == currentTail)
                {  
                    // delayed cache write
                    Volatile.Write(ref slots[slotsIndex], item);
                    return Result.Success;
                }

                currentTail = actualTail;
            }
        }
    }

    /////// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
    ////[DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
    ////[StructLayout(LayoutKind.Explicit, Size = 3 * Internal.PaddingHelpers.CACHE_LINE_SIZE)] // padding before/between/after fields
    internal struct PaddedHeadAndTail
    {
        ////[FieldOffset(1 * Internal.PaddingHelpers.CACHE_LINE_SIZE)]
        ////public int Head;
        ////[FieldOffset(2 * Internal.PaddingHelpers.CACHE_LINE_SIZE)] 
        public int Tail;
    }

    internal enum Result
    {
        Success,
        Full,
        Locked,
    }
}

