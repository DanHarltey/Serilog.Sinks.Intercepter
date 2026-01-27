using Serilog;
using Serilog.Events;
using Serilog.Sinks;
using Serilog.Sinks.Intercepter;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_10_Increment;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Internal.RingBuffer_10_Increment
{
    internal sealed class RingBuffer_10_Increment
    {
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
        private const ulong INDEX_MASK = unchecked(COMPLETE_ADDING_MASK - 1);

        private readonly Slot[] _slots;
        private ulong _index;

        private readonly int _capacity;
        private readonly int _indexMask;
        private readonly ulong _rowMask;
        private ulong _amountAdded;

        public bool CompletedAdding => IsComplete(Volatile.Read(ref _index));

        internal int Capacity => _slots.Length;

        internal ulong Index => Volatile.Read(ref _amountAdded) & INDEX_MASK;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public RingBuffer_10_Increment(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be above zero.");
            }

            _capacity = capacity;

            var roundedCapacity = BitOperations.RoundUpToPowerOf2((ulong)capacity);
            _slots = new Slot[roundedCapacity];
            _indexMask = _slots.Length - 1;
            _rowMask = (ulong)_slots.Length;// BitOperations.TrailingZeroCount(roundedCapacity);

            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].Verison = (ulong)i;
            }
        }

        public bool CompleteAdding()
        {
            var currentIndex = Volatile.Read(ref _index);

            var actualIndex = Interlocked.CompareExchange(
                ref _index,
                COMPLETE_ADDING_MASK,
                currentIndex);

            if (!IsComplete(currentIndex))
            {
                if (actualIndex == currentIndex)
                {
                    _amountAdded = actualIndex;
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
            var localIndex = Interlocked.Increment(ref _index) - 1;

            if (!IsComplete(localIndex))
            {

                AddToSlot(logEvent, localIndex);
                hasAdded = true;
            }

            return hasAdded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToSlot(LogEvent logEvent, ulong index)
        {
            var slotIndex = (int)index & _indexMask;
            //var previousVerison = index;// index >> _rowMask;
            ////var newVerison = index + _rowMask;

            // using index here as oppossed to Unsafe, to allow JIT to use CORINFO_HELP_ASSIGN_REF instead of slower CORINFO_HELP_CHECKED_ASSIGN_REF
            ref var slot = ref _slots[slotIndex];

            if (Volatile.Read(ref slot.Verison) != index)
            {
                SpinWait spinWait = default;
                do
                {
                    spinWait.SpinOnce();
                }
                while (Volatile.Read(ref slot.Verison) != index);
            }
            slot.LogEvent = logEvent;
            Volatile.Write(ref slot.Verison, slot.Verison + _rowMask);
        }

        public IReadOnlyCollection<LogEvent> GetEnumerable()
        {
            return new RingBuffer_10_IncrementEnumerable(this);
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
            // on x86 gets compiled to mov, test
            (index & COMPLETE_ADDING_MASK) != 0;

        private class RingBuffer_10_IncrementEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_10_Increment _buffer;

            public RingBuffer_10_IncrementEnumerable(RingBuffer_10_Increment buffer)
            {
                _buffer = buffer;
            }

            public int Count => 8;// (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_10_IncrementEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_10_IncrementEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_10_Increment _RingBuffer_10_Increment;
            private int _index = -1;

            public RingBuffer_10_IncrementEnumerator(RingBuffer_10_Increment RingBuffer_10_Increment)
            {
                _RingBuffer_10_Increment = RingBuffer_10_Increment;
                var currentIndex = (int)(_RingBuffer_10_Increment._amountAdded & unchecked(COMPLETE_ADDING_MASK - 1));
                if (currentIndex < RingBuffer_10_Increment.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = currentIndex - RingBuffer_10_Increment._slots.Length - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_10_Increment._slots.Length;

                    ref var slot = ref _RingBuffer_10_Increment._slots[index];
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

                var currentIndex = _RingBuffer_10_Increment._amountAdded & unchecked(COMPLETE_ADDING_MASK - 1);

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
