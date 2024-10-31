using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.ModificationController.SkillSlots
{
    public readonly struct SkillSlotMask : IEquatable<SkillSlotMask>, IEnumerable<SkillSlot>
    {
        static readonly int[] _containedSlotCountLookup;

        static SkillSlotMask()
        {
            _containedSlotCountLookup = new int[1 << SkillSlotUtils.SkillSlotCount];

            for (int i = 0; i < _containedSlotCountLookup.Length; i++)
            {
                int containedSlotCount = 0;
                for (int slot = 0; slot < SkillSlotUtils.SkillSlotCount; slot++)
                {
                    if ((i & (1 << slot)) != 0)
                    {
                        containedSlotCount++;
                    }
                }

                _containedSlotCountLookup[i] = containedSlotCount;
            }
        }

        public const int MASK_SIZE = sizeof(uint) * 8;

        public readonly uint Mask;

        public readonly int ContainedSlotCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return _containedSlotCountLookup[Mask & SkillSlotUtils.ValidSkillSlotMask];
            }
        }

        public SkillSlotMask(uint mask)
        {
            Mask = mask;
        }

        public readonly bool Contains(SkillSlot skillSlot)
        {
            return SkillSlotUtils.GetSkillSlotBit(Mask, skillSlot);
        }

        public override int GetHashCode()
        {
            return Mask.GetHashCode();
        }

        public readonly override bool Equals(object obj)
        {
            return obj is SkillSlotMask other && Equals(other);
        }

        public readonly bool Equals(SkillSlotMask other)
        {
            return Mask == other.Mask;
        }

        public static SkillSlotMask operator |(in SkillSlotMask lhs, in SkillSlotMask rhs)
        {
            return new SkillSlotMask(lhs.Mask | rhs.Mask);
        }

        public static SkillSlotMask operator &(in SkillSlotMask lhs, in SkillSlotMask rhs)
        {
            return new SkillSlotMask(lhs.Mask & rhs.Mask);
        }

        public static SkillSlotMask operator ^(in SkillSlotMask lhs, in SkillSlotMask rhs)
        {
            return new SkillSlotMask(lhs.Mask ^ rhs.Mask);
        }

        public static SkillSlotMask operator ~(in SkillSlotMask slotMask)
        {
            return new SkillSlotMask(~slotMask.Mask);
        }

        public static implicit operator SkillSlotMask(SkillSlot slot)
        {
            return new SkillSlotMask(SkillSlotUtils.GetSlotBitMask(slot));
        }

        public static SkillSlotMask FromCollection(IEnumerable<SkillSlot> slots)
        {
            uint mask = 0;
            foreach (SkillSlot slot in slots)
            {
                mask |= SkillSlotUtils.GetSlotBitMask(slot);
            }

            return new SkillSlotMask(mask);
        }

        public IEnumerator<SkillSlot> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        struct Enumerator : IEnumerator<SkillSlot>
        {
            readonly SkillSlotMask _mask;

            int _position = -1;

            public Enumerator(in SkillSlotMask mask)
            {
                _mask = mask;
            }

            public SkillSlot Current { readonly get; private set; } = SkillSlot.None;

            readonly object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                do
                {
                    _position++;
                } while (_position < SkillSlotUtils.SkillSlotCount && !_mask.Contains((SkillSlot)_position));

                if (_position >= SkillSlotUtils.SkillSlotCount)
                {
                    return false;
                }

                Current = (SkillSlot)_position;
                return true;
            }

            public void Reset()
            {
                _position = -1;
                Current = SkillSlot.None;
            }

            readonly void IDisposable.Dispose()
            {
            }
        }
    }
}
