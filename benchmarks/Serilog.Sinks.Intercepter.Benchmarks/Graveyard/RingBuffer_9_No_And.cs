using Serilog;
using Serilog.Events;
using Serilog.Sinks;
using Serilog.Sinks;
using Serilog.Sinks.Intercepter;
using Serilog.Sinks.Intercepter;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;
using Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And;
using System;
using System;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Serilog.Sinks.Intercepter.Internal.RingBuffer_9_No_And
{
    internal sealed class RingBuffer_9_No_And
    {
        private const ulong COMPLETE_ADDING_MASK = unchecked(0x8000000000000000);
        private const ulong INDEX_MASK = unchecked(COMPLETE_ADDING_MASK - 1);

        private readonly Slot[] _slots;
        private ulong _index;

        private readonly int _capacity;
        private readonly int _indexMask;
        private readonly ulong _rowMask;

        public bool CompletedAdding => IsComplete(Volatile.Read(ref _index));

        internal int Capacity => _slots.Length;

        internal ulong Index => Volatile.Read(ref _index) & INDEX_MASK;

        public RingBuffer_9_No_And(int capacity)
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

            while (!IsComplete(currentIndex))
            {
                var actualIndex = Interlocked.CompareExchange(
                    ref _index,
                    currentIndex | COMPLETE_ADDING_MASK,
                    currentIndex);

                if (actualIndex == currentIndex)
                {
                    return true;
                }

                currentIndex = actualIndex;
            }

            return false;
        }

        public bool TryAdd(LogEvent logEvent)
        {
            var currentIndex = Volatile.Read(ref _index);

            // Loop in case of contention...
            while (!IsComplete(currentIndex))
            {
                var newIndex = currentIndex + 1;

                //// add overflow check
                //if(IsComplete(newIndex))
                //{
                //    throw new Exception();
                //}

                var actualIndex = Interlocked.CompareExchange(
                    ref _index,
                    newIndex,
                    currentIndex);

                if (currentIndex == actualIndex)
                {
                    AddToSlot(logEvent, actualIndex);
                    return true;
                }

                currentIndex = actualIndex;
            }

            return false;
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
                var spinWait = new SpinWait();
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
            return new RingBuffer_9_No_AndEnumerable(this);
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

        private class RingBuffer_9_No_AndEnumerable : IReadOnlyCollection<LogEvent>
        {
            private readonly RingBuffer_9_No_And _buffer;

            public RingBuffer_9_No_AndEnumerable(RingBuffer_9_No_And buffer)
            {
                _buffer = buffer;
            }

            public int Count => 8;// (_buffer._index & unchecked(COMPLETE_ADDING_MASK - 1)) < _buffer.Capacity ? _buffer._index & unchecked(COMPLETE_ADDING_MASK - 1) : _buffer.Capacity;

            public IEnumerator<LogEvent> GetEnumerator()
            {
                return new RingBuffer_9_No_AndEnumerator(_buffer);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class RingBuffer_9_No_AndEnumerator : IEnumerator<LogEvent>
        {
            private readonly RingBuffer_9_No_And _RingBuffer_9_No_And;
            private int _index = -1;

            public RingBuffer_9_No_AndEnumerator(RingBuffer_9_No_And RingBuffer_9_No_And)
            {
                _RingBuffer_9_No_And = RingBuffer_9_No_And;
                var currentIndex = (int)(_RingBuffer_9_No_And._index & unchecked(COMPLETE_ADDING_MASK - 1));
                if (currentIndex < RingBuffer_9_No_And.Capacity)
                {
                    _index = -1;
                }
                else
                {
                    _index = currentIndex - RingBuffer_9_No_And._slots.Length - 1;
                }
            }

            public LogEvent Current
            {
                get
                {
                    var index = _index % _RingBuffer_9_No_And._slots.Length;

                    ref var slot = ref _RingBuffer_9_No_And._slots[index];
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

                var currentIndex = _RingBuffer_9_No_And._index & unchecked(COMPLETE_ADDING_MASK - 1);

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
