using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace RiskOfChaos.ModificationController.SkillSlots
{
    public readonly struct SkillSlotMask : IEquatable<SkillSlotMask>, IEnumerable<SkillSlot>
    {
        static readonly int[] _containedSlotCountLookup;

        static SkillSlotMask()
        {
            const int LOOKUP_SIZE = 1 << SkillSlotUtils.SkillSlotCount;
            _containedSlotCountLookup = new int[LOOKUP_SIZE];

            for (uint mask = 0; mask < LOOKUP_SIZE; mask++)
            {
                int containedSlotCount = 0;
                for (SkillSlot slot = 0; slot <= SkillSlotUtils.MaxSlot; slot++)
                {
                    if (SkillSlotUtils.GetSkillSlotBit(mask, slot))
                    {
                        containedSlotCount++;
                    }
                }

                _containedSlotCountLookup[mask] = containedSlotCount;
            }
        }

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
            Mask = mask & SkillSlotUtils.ValidSkillSlotMask;
        }

        public readonly bool Contains(SkillSlot skillSlot)
        {
            return SkillSlotUtils.GetSkillSlotBit(Mask, skillSlot);
        }

        public override readonly string ToString()
        {
            if ((Mask & SkillSlotUtils.ValidSkillSlotMask) == 0)
                return string.Empty;

            StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();

            for (SkillSlot slot = 0; slot <= SkillSlotUtils.MaxSlot; slot++)
            {
                if (Contains(slot))
                {
                    if (stringBuilder.Length > 0)
                    {
                        stringBuilder.Append(" | ");
                    }

                    stringBuilder.Append(slot.ToString("G"));
                }
            }

            string result = stringBuilder.ToString();
            stringBuilder = HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);

            return result;
        }

        public override readonly int GetHashCode()
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

        public static bool operator ==(SkillSlotMask lhs, SkillSlotMask rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(SkillSlotMask lhs, SkillSlotMask rhs)
        {
            return !(lhs == rhs);
        }

        public static SkillSlotMask operator |(SkillSlotMask lhs, SkillSlotMask rhs)
        {
            return new SkillSlotMask(lhs.Mask | rhs.Mask);
        }

        public static SkillSlotMask operator &(SkillSlotMask lhs, SkillSlotMask rhs)
        {
            return new SkillSlotMask(lhs.Mask & rhs.Mask);
        }

        public static SkillSlotMask operator ^(SkillSlotMask lhs, SkillSlotMask rhs)
        {
            return new SkillSlotMask(lhs.Mask ^ rhs.Mask);
        }

        public static SkillSlotMask operator ~(SkillSlotMask slotMask)
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

            public Enumerator(SkillSlotMask mask)
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
