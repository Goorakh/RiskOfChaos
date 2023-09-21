using HG;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public abstract class VoteSelection<TVoteResult>
    {
        public struct VoteOption
        {
            public readonly int VoteIndex;
            public readonly TVoteResult Value;

            int _numVotes;

            public int NumVotes
            {
                get
                {
                    return _numVotes;
                }
                set
                {
                    if (_numVotes == value)
                        return;

                    _numVotes = value;
                }
            }

            public VoteOption(int voteIndex, TVoteResult value)
            {
                VoteIndex = voteIndex;
                Value = value;
                _numVotes = 0;
            }

            public override readonly string ToString()
            {
                return $"{VoteIndex + 1}: {Value}";
            }
        }

        VoteOption[] _options;

        int _numOptions;
        public int NumOptions
        {
            get => _numOptions;
            set
            {
                if (_numOptions == value)
                    return;

                _numOptions = value;

                if (_options != null)
                {
                    ArrayUtils.EnsureCapacity(ref _options, _numOptions);
                }
                else
                {
                    _options = new VoteOption[_numOptions];
                }
            }
        }

        public VoteWinnerSelectionMode WinnerSelectionMode = VoteWinnerSelectionMode.MostVotes;

        public int TotalVotes => Enumerable.Range(0, _numOptions).Sum(i => _options[i].NumVotes);

        public bool IsVoteActive { get; private set; }

        public VoteSelection(int numOptions)
        {
            if (numOptions <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numOptions), "Number of options must be greater than 0");
            }

            NumOptions = numOptions;
        }

        public bool TryGetVoteResult(out TVoteResult result)
        {
            VoteOption winningOption;
            switch (WinnerSelectionMode)
            {
                case VoteWinnerSelectionMode.MostVotes when tryGetWinner_MostVotes(out winningOption):
                case VoteWinnerSelectionMode.RandomProportional when tryGetWinner_Proportional(out winningOption):
                    EndVote();

                    result = winningOption.Value;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        bool tryGetWinner_MostVotes(out VoteOption result)
        {
            int currentGreatestVoteCount = -1;

            List<VoteOption> currentWinningVotes = new List<VoteOption>(_numOptions);

            for (int i = 0; i < _numOptions; i++)
            {
                VoteOption voteOption = _options[i];

                if (voteOption.NumVotes >= currentGreatestVoteCount)
                {
                    if (voteOption.NumVotes > currentGreatestVoteCount)
                    {
                        currentGreatestVoteCount = voteOption.NumVotes;
                        currentWinningVotes.Clear();
                    }

                    currentWinningVotes.Add(voteOption);
                }
            }

            if (currentWinningVotes.Count == 0)
            {
                result = default;
                return false;
            }
            else
            {
                result = RoR2Application.rng.NextElementUniform(currentWinningVotes);
                return true;
            }
        }

        bool tryGetWinner_Proportional(out VoteOption result)
        {
            if (_numOptions <= 0)
            {
                result = default;
                return false;
            }

            if (TotalVotes <= 0)
            {
                result = _options[RoR2Application.rng.RangeInt(0, NumOptions)];
                return true;
            }

            WeightedSelection<VoteOption> voteOptionSelection = new WeightedSelection<VoteOption>(_numOptions);
            for (int i = 0; i < _numOptions; i++)
            {
                VoteOption voteOption = _options[i];
                voteOptionSelection.AddChoice(voteOption, voteOption.NumVotes);
            }

            result = voteOptionSelection.Evaluate(RoR2Application.rng.nextNormalizedFloat);
            return true;
        }

        public void EndVote()
        {
            if (!IsVoteActive)
                return;

            resetState();
            IsVoteActive = false;
        }

        public void StartVote(TVoteResult[] newOptions)
        {
            if (newOptions is null)
                throw new ArgumentNullException(nameof(newOptions));

            if (newOptions.Length != _numOptions)
                throw new ArgumentException("Options array does not match vote option count", nameof(newOptions));

            EndVote();

            for (int i = 0; i < _numOptions; i++)
            {
                _options[i] = new VoteOption(i, newOptions[i]);
            }

            IsVoteActive = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValidOptionIndex(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < _numOptions;
        }

        protected void checkOptionIndex(int optionIndex)
        {
            if (!IsValidOptionIndex(optionIndex))
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex));
            }
        }

        protected ref VoteOption getOption(int optionIndex)
        {
            return ref _options[optionIndex];
        }

        public bool TryGetOption(int optionIndex, out VoteOption result)
        {
            if (!IsValidOptionIndex(optionIndex))
            {
                result = default;
                return false;
            }

            result = getOption(optionIndex);
            return true;
        }

        public TVoteResult[] GetVoteOptions()
        {
            int numOptions = NumOptions;
            TVoteResult[] options = new TVoteResult[numOptions];
            for (int i = 0; i < numOptions; i++)
            {
                options[i] = getOption(i).Value;
            }

            return options;
        }

        protected virtual void resetState()
        {
        }
    }
}
