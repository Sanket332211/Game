using System;
using UnityEngine;

namespace SlotGame.Bonus
{
    /// <summary>
    /// Manages the Free Spins bonus mode, win multipliers, and bonus triggers.
    /// </summary>
    public class BonusFeatureManager : MonoBehaviour
    {
        [Header("Bonus Tuning")]
        [Tooltip("Win multiplier applied to all wins during Free Spins mode.")]
        [SerializeField] private int freeSpinsMultiplier = 2;

        private int _remainingFreeSpins = 0;
        private int _totalBonusWinnings = 0;
        private bool _isBonusActive = false;

        public event Action<int, int> OnFreeSpinsStarted; // (totalSpins, multiplier)
        public event Action<int> OnFreeSpinStep;           // (remainingSpins)
        public event Action<int> OnFreeSpinsCompleted;     // (totalBonusWinnings)

        public bool IsBonusActive => _isBonusActive;
        public int RemainingFreeSpins => _remainingFreeSpins;
        public int ActiveMultiplier => _isBonusActive ? freeSpinsMultiplier : 1;
        public int TotalBonusWinnings => _totalBonusWinnings;

        /// <summary>
        /// Activates Free Spins mode when 3 Scatters land.
        /// </summary>
        public void TriggerFreeSpins(int spinsCount)
        {
            _remainingFreeSpins += spinsCount;
            _totalBonusWinnings = 0;
            _isBonusActive = true;

            Debug.Log($"[BonusFeatureManager] Free Spins Activated! {_remainingFreeSpins} spins with {freeSpinsMultiplier}x multiplier.");
            OnFreeSpinsStarted?.Invoke(_remainingFreeSpins, freeSpinsMultiplier);
        }

        /// <summary>
        /// Registers a completed free spin and deducts one spin from counter.
        /// </summary>
        public void RecordSpinOutcome(int payout)
        {
            if (!_isBonusActive) return;

            _totalBonusWinnings += payout;
            _remainingFreeSpins--;

            OnFreeSpinStep?.Invoke(_remainingFreeSpins);

            if (_remainingFreeSpins <= 0)
            {
                _remainingFreeSpins = 0;
                _isBonusActive = false;
                Debug.Log($"[BonusFeatureManager] Free Spins Finished! Total Won: {_totalBonusWinnings}");
                OnFreeSpinsCompleted?.Invoke(_totalBonusWinnings);
            }
        }

        /// <summary>
        /// Force ends bonus mode (e.g. on player exit or reset).
        /// </summary>
        public void ResetBonus()
        {
            _remainingFreeSpins = 0;
            _totalBonusWinnings = 0;
            _isBonusActive = false;
        }
    }
}
