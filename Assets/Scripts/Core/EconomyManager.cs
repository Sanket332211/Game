using System;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Manages player credits, betting limits, and local state persistence.
    /// Emits strongly typed events when balance or bet thresholds change.
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        private const string PrefBalanceKey = "SlotGame_PlayerBalance";
        private const string PrefTotalWonKey = "SlotGame_TotalWon";

        [Header("Starting Credits")]
        [SerializeField] private int startingBalance = 1000;

        [Header("Bet Configuration")]
        [SerializeField] private int[] betTiers = new int[] { 10, 20, 50, 100, 250, 500 };
        [SerializeField] private int defaultBetIndex = 1;

        // Runtime state
        private int _currentBalance;
        private int _currentBetIndex;
        private int _lastWinAmount;
        private int _totalWon;

        // Events for UI decoupling
        public event Action<int> OnBalanceChanged;
        public event Action<int> OnBetChanged;
        public event Action<int> OnWinAwarded;

        public int Balance => _currentBalance;
        public int CurrentBet => betTiers[_currentBetIndex];
        public int LastWin => _lastWinAmount;
        public int TotalWon => _totalWon;
        public bool CanAffordCurrentBet => _currentBalance >= CurrentBet;

        private void Awake()
        {
            LoadEconomyData();
        }

        /// <summary>
        /// Loads saved player balance or initializes with default starting credits.
        /// </summary>
        public void LoadEconomyData()
        {
            _currentBalance = PlayerPrefs.GetInt(PrefBalanceKey, startingBalance);
            _totalWon = PlayerPrefs.GetInt(PrefTotalWonKey, 0);
            _currentBetIndex = Mathf.Clamp(defaultBetIndex, 0, betTiers.Length - 1);
            _lastWinAmount = 0;

            NotifyStateChanged();
        }

        /// <summary>
        /// Saves current balance and cumulative winnings to PlayerPrefs.
        /// </summary>
        public void SaveEconomyData()
        {
            PlayerPrefs.SetInt(PrefBalanceKey, _currentBalance);
            PlayerPrefs.SetInt(PrefTotalWonKey, _totalWon);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Attempts to deduct the active bet from the player balance.
        /// Returns false if funds are insufficient.
        /// </summary>
        public bool TryDeductBet()
        {
            if (!CanAffordCurrentBet)
            {
                Debug.LogWarning("[EconomyManager] Insufficient credits to place bet!");
                return false;
            }

            _currentBalance -= CurrentBet;
            SaveEconomyData();
            OnBalanceChanged?.Invoke(_currentBalance);
            return true;
        }

        /// <summary>
        /// Awards payout credits to the player's balance.
        /// </summary>
        public void AwardPayout(int amount)
        {
            if (amount <= 0) return;

            _lastWinAmount = amount;
            _currentBalance += amount;
            _totalWon += amount;

            SaveEconomyData();
            OnBalanceChanged?.Invoke(_currentBalance);
            OnWinAwarded?.Invoke(_lastWinAmount);
        }

        /// <summary>
        /// Steps bet to the next higher increment.
        /// </summary>
        public void IncreaseBet()
        {
            if (_currentBetIndex < betTiers.Length - 1)
            {
                _currentBetIndex++;
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        /// <summary>
        /// Steps bet to the next lower increment.
        /// </summary>
        public void DecreaseBet()
        {
            if (_currentBetIndex > 0)
            {
                _currentBetIndex--;
                OnBetChanged?.Invoke(CurrentBet);
            }
        }

        /// <summary>
        /// Sets the bet to the maximum available tier.
        /// </summary>
        public void SetMaxBet()
        {
            _currentBetIndex = betTiers.Length - 1;
            OnBetChanged?.Invoke(CurrentBet);
        }

        /// <summary>
        /// Resets player credits back to starting bankroll (helpful when bankrupt).
        /// </summary>
        public void ResetBankroll()
        {
            _currentBalance = startingBalance;
            _lastWinAmount = 0;
            SaveEconomyData();
            NotifyStateChanged();
        }

        private void NotifyStateChanged()
        {
            OnBalanceChanged?.Invoke(_currentBalance);
            OnBetChanged?.Invoke(CurrentBet);
            OnWinAwarded?.Invoke(_lastWinAmount);
        }

        private void OnApplicationQuit()
        {
            SaveEconomyData();
        }
    }
}
