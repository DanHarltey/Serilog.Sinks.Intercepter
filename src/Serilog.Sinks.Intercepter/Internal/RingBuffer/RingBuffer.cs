using Serilog;
using Serilog.Events;
using Serilog.Sinks;
using Serilog.Sinks.Intercepter;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Internal.RingBuffer
{
    internal sealed class RingBuffer
    {
        /// <summary>Padded head and tail indices, to avoid false sharing between producers and consumers.</summary>
        [DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
        [StructLayout(LayoutKind.Explicit, Size = 2 * 128)] // padding before/between/after fields
        internal struct PaddedHeadAndTail
        {
            [FieldOffset(1 * 128)]
            public ulong Index;
            ////[FieldOffset(2 * Internal.PaddingHelpers.CACHE_LINE_SIZE)] 
            ////public int Tail;
        }
        /*
         * max is 9,223,372,036,854,775,807
         *      /             4,000,000,000
         *                       2,305,843,009
         *                       2305843009 seconds
         *                       73 years
         *                       
         *                       
         * nine quintillion, two hundred and twenty-three quadrillion, three hundred and seventy-two trillion, thirty-six billion, eight hundred and fifty-four million
         *         // The current barrier phase
        // We don't need to worry about overflow, the max value is 2^63-1; If it starts from 0 at a
        // rate of 4 billion increments per second, it will takes about 64 years to overflow.
         */
        private const ulong COMPLETE_ADDING_MASK = unchecked(0x8000000000000000);
        private const ulong INDEX_MASK = unchecked(0x7fffffffffffffff);

        private PaddedHeadAndTail _index;
        private ulong _completedIndex;
        private readonly int _capacity;
        private readonly Slot[] _slots;
        private readonly int _rowShift;
        private readonly int _indexMask;

        public bool CompletedAdding => IsComplete(Volatile.Read(ref _index.Index));

        internal int Capacity => _slots.Length;

        //internal ulong Index => Volatile.Read(ref _completedIndex) & INDEX_MASK;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public RingBuffer(int capacity)
        {
            if (capacity <= 0 || capacity > 0x40000000)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be above 0 and below 1,073,741,8245");
            }

            _capacity = capacity;

            var pow2Mask = capacity - 1;
            var leadingZeros = BitOperations.LeadingZeroCount((uint)pow2Mask);

            if ((capacity & pow2Mask) != 0)
            {
                // not POW of 2, round up to nearest pow of 2
                capacity = (int)(0x1_0000_0000ul >> leadingZeros);
                pow2Mask = capacity - 1;
            }

            _slots = new Slot[capacity];
            _indexMask = pow2Mask;
            _rowShift = 32 - leadingZeros;
        }

        public bool CompleteAdding()
        {
            var currentIndex = Volatile.Read(ref _index.Index);

            while (!IsComplete(currentIndex))
            {
                var actualIndex = Interlocked.CompareExchange(
                    ref _index.Index,
                    COMPLETE_ADDING_MASK,
                    currentIndex);

                if (actualIndex == currentIndex)
                {
                    _completedIndex = actualIndex;
                    return true;
                }

                currentIndex = actualIndex;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryAdd(LogEvent logEvent)
        {
            var hasAdded = false;

            // lock xadd 
            var localIndex = Interlocked.Increment(ref _index.Index) - 1;

            if (!IsComplete(localIndex))
            {
                var slotIndex = ((int)localIndex) & _indexMask;
                var expectedVerison = localIndex >> _rowShift;

#if DEBUG
                PauseOnSlotWrite();
#endif
                // using index here as oppossed to Unsafe, to allow JIT to use CORINFO_HELP_ASSIGN_REF instead of slower CORINFO_HELP_CHECKED_ASSIGN_REF
                ref var slot = ref _slots[slotIndex];

                if (Volatile.Read(ref slot.Verison) != expectedVerison)
                {
                    // rare case, do not inline for speed
                    WaitForSlot(ref slot, expectedVerison);
                }

                slot.LogEvent = logEvent;
                Volatile.Write(ref slot.Verison, slot.Verison + 1);
                hasAdded = true;
            }

            return hasAdded;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WaitForSlot(ref Slot slot, ulong index)
        {
            var spinWait = new SpinWait();
            do
            {
                spinWait.SpinOnce();
            }
            while (Volatile.Read(ref slot.Verison) != index);
        }

        public IEnumerable<LogEvent> GetEnumerable()
        {
            return new RingBufferEnumerable(this);
        }

        private struct Slot
        {
            public ulong Verison;
            public LogEvent? LogEvent;

            public Slot()
            {
                new Exception();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsComplete(ulong index) =>
            // on x86 gets compiled to mov, test, jne
            (index & COMPLETE_ADDING_MASK) != 0;

#if DEBUG
        private bool _pauseOnSlotWrite = false;
        public void PauseOnSlotWrite(bool pause)
        {
            Volatile.Write(ref _pauseOnSlotWrite, pause);
            Interlocked.MemoryBarrier();
        }

        private void PauseOnSlotWrite()
        {
            if (Volatile.Read(ref _pauseOnSlotWrite))
            {
                PauseOnSlotWrite(false);
                Thread.Sleep(100);
            }
        }
#endif

        private class RingBufferEnumerable : IEnumerable<LogEvent>//  IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer _buffer;

            public RingBufferEnumerable(RingBuffer buffer)
            {
                _buffer = buffer;
            }

            //public int Count => 8;// (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBufferEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBufferEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer _ringBuffer;
            private int _index = -1;

            public RingBufferEnumerator(RingBuffer ringBuffer)
            {
                _ringBuffer = ringBuffer;
                var currentIndex = (int)(_ringBuffer._completedIndex);
                if (currentIndex < ringBuffer._capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = currentIndex - ringBuffer._capacity - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var slotIndex = _index & _ringBuffer._indexMask;

                    ref var slot = ref _ringBuffer._slots[slotIndex];
                    return slot.LogEvent;
                }
            }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++_index;

                var currentIndex = _ringBuffer._completedIndex & unchecked(COMPLETE_ADDING_MASK - 1);

                if (_index < (int)currentIndex)
                {
                    return true;
                }
                return false;
            }

            public void Reset()
            {
            }
        }

    }
}
