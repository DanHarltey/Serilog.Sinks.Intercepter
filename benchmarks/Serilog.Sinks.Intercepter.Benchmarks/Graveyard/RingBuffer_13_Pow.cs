using Serilog;
using Serilog.Events;
using Serilog.Sinks;
using Serilog.Sinks.Intercepter;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_13_Pow;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Internal.RingBuffer_13_Pow
{
    internal sealed class RingBuffer_13_Pow
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
        private const ulong INDEX_MASK = unchecked(0x7fffffffffffffff);

        private readonly Slot[] _slots;
        private readonly ulong _slotsLength;

        private readonly int _indexMask;

        private ulong _index;

        private ulong _completedIndex;

        private readonly int _capacity;

        public bool CompletedAdding => IsComplete(Volatile.Read(ref _index));

        internal int Capacity => _slots.Length;

        //internal ulong Index => Volatile.Read(ref _completedIndex) & INDEX_MASK;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public RingBuffer_13_Pow(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Must be above zero.");
            }

            _capacity = capacity;
            var roundedCapacity = BitOperations.RoundUpToPowerOf2((uint)capacity);
            _slotsLength = roundedCapacity;

            _indexMask = (int)roundedCapacity - 1;

            var slots = new Slot[roundedCapacity];
            for (var i = 0; i < slots.Length; i++)
            {
                slots[i].Verison = (ulong)i;
            }
            _slots = slots;
        }

        public bool CompleteAdding()
        {
            var currentIndex = Volatile.Read(ref _index);

            while (!IsComplete(currentIndex))
            {
                var actualIndex = Interlocked.CompareExchange(
                    ref _index,
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
            var localIndex = Interlocked.Increment(ref _index) - 1;

            if (!IsComplete(localIndex))
            {
                var slotIndex = ((int)localIndex) & _indexMask;

                // using index here as oppossed to Unsafe, to allow JIT to use CORINFO_HELP_ASSIGN_REF instead of slower CORINFO_HELP_CHECKED_ASSIGN_REF
                ref var slot = ref _slots[slotIndex];

                if (Volatile.Read(ref slot.Verison) != localIndex)
                {
                    WaitForSlot(ref slot, localIndex);
                }

                slot.LogEvent = logEvent;
                Volatile.Write(ref slot.Verison, slot.Verison + _slotsLength);
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

        public IReadOnlyCollection<LogEvent> GetEnumerable()
        {
            return new RingBuffer_13_PowEnumerable(this);
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

        private class RingBuffer_13_PowEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_13_Pow _buffer;

            public RingBuffer_13_PowEnumerable(RingBuffer_13_Pow buffer)
            {
                _buffer = buffer;
            }

            public int Count => 8;// (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_13_PowEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_13_PowEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_13_Pow _RingBuffer_13_Pow;
            private int _index = -1;

            public RingBuffer_13_PowEnumerator(RingBuffer_13_Pow RingBuffer_13_Pow)
            {
                _RingBuffer_13_Pow = RingBuffer_13_Pow;
                var currentIndex = (int)(_RingBuffer_13_Pow._completedIndex & unchecked(COMPLETE_ADDING_MASK - 1));
                if (currentIndex < RingBuffer_13_Pow.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = currentIndex - RingBuffer_13_Pow._slots.Length - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_13_Pow._slots.Length;

                    ref var slot = ref _RingBuffer_13_Pow._slots[index];
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

                var currentIndex = _RingBuffer_13_Pow._completedIndex & unchecked(COMPLETE_ADDING_MASK - 1);

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
