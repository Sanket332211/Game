using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Encapsulates the complete result of a evaluated spin cycle.
    /// </summary>
    public struct SpinResult
    {
        public bool IsWin;
        public int TotalPayout;
        public int WinMultiplier;
        public SymbolType WinningSymbol;
        public bool IsJackpot;
        public bool IsConsolationWin;
        public bool TriggeredFreeSpins;
        public int FreeSpinsAwarded;
        public string WinTitle;
        public string WinDescription;
    }

    /// <summary>
    /// Handles paytable logic, win pattern verification, Wild substitutions,
    /// and payout distribution calculations according to game design standards.
    /// </summary>
    public class PaytableManager : MonoBehaviour
    {
        [Header("Jackpot Multipliers")]
        [Tooltip("Payout multiplier for hitting 3 Wild symbols (Top Jackpot).")]
        [SerializeField] private int tripleWildMultiplier = 100;

        [Header("Free Spins Bonus")]
        [Tooltip("Number of Free Spins awarded upon landing 3 Scatter symbols.")]
        [SerializeField] private int freeSpinsCount = 10;

        /// <summary>
        /// Evaluates the outcome row across all slot reels and determines the winning payout.
        /// </summary>
        /// <param name="reelsOutcome">The symbols landed on the center payline.</param>
        /// <param name="betAmount">Current active bet per spin.</param>
        /// <param name="globalMultiplier">Any active bonus multiplier (e.g. 2x during Free Spins).</param>
        /// <returns>SpinResult containing win status, multipliers, and awards.</returns>
        public SpinResult EvaluateOutcome(SymbolData[] reelsOutcome, int betAmount, int globalMultiplier = 1)
        {
            var result = new SpinResult
            {
                IsWin = false,
                TotalPayout = 0,
                WinMultiplier = 0,
                WinningSymbol = SymbolType.Cherry,
                IsJackpot = false,
                IsConsolationWin = false,
                TriggeredFreeSpins = false,
                FreeSpinsAwarded = 0,
                WinTitle = "NO WIN",
                WinDescription = "Better luck next spin!"
            };

            if (reelsOutcome == null || reelsOutcome.Length < 3)
            {
                Debug.LogWarning("[PaytableManager] Invalid reels outcome passed for evaluation!");
                return result;
            }

            int reelCount = reelsOutcome.Length;

            // 1. Check for Scatter symbols (Bonus trigger)
            int scatterCount = 0;
            for (int i = 0; i < reelCount; i++)
            {
                if (reelsOutcome[i].IsScatter)
                    scatterCount++;
            }

            if (scatterCount >= 3)
            {
                result.IsWin = true;
                result.TriggeredFreeSpins = true;
                result.FreeSpinsAwarded = freeSpinsCount;
                result.WinMultiplier = 5; // Scatter initial award payout
                result.TotalPayout = betAmount * result.WinMultiplier * globalMultiplier;
                result.WinningSymbol = SymbolType.Scatter;
                result.WinTitle = "FREE SPINS UNLOCKED!";
                result.WinDescription = $"3 SCATTERS! Won {freeSpinsCount} Free Spins with 2x Multiplier!";
                return result;
            }

            // 2. Check for Triple Wild Jackpot (3 Wilds)
            bool allWilds = true;
            for (int i = 0; i < reelCount; i++)
            {
                if (!reelsOutcome[i].IsWild)
                {
                    allWilds = false;
                    break;
                }
            }

            if (allWilds)
            {
                result.IsWin = true;
                result.IsJackpot = true;
                result.WinMultiplier = tripleWildMultiplier;
                result.TotalPayout = betAmount * result.WinMultiplier * globalMultiplier;
                result.WinningSymbol = SymbolType.Wild;
                result.WinTitle = "MEGA JACKPOT!";
                result.WinDescription = $"TRIPLE WILDS! Phenomenal Win of {result.TotalPayout:N0} Credits ({tripleWildMultiplier}x)!";
                return result;
            }

            // 3. Core Win Evaluation: Check for 3-of-a-kind (with Wild Substitution)
            // Wild substitutes for any non-scatter paying symbol.
            SymbolData targetSymbol = null;
            for (int i = 0; i < reelCount; i++)
            {
                if (!reelsOutcome[i].IsWild && !reelsOutcome[i].IsScatter)
                {
                    targetSymbol = reelsOutcome[i];
                    break;
                }
            }

            // If all non-wild symbols are identical, we have a complete 3-of-a-kind match!
            bool isFullMatch = true;
            if (targetSymbol != null)
            {
                for (int i = 0; i < reelCount; i++)
                {
                    bool matchesTarget = (reelsOutcome[i].Type == targetSymbol.Type);
                    bool isWild = reelsOutcome[i].IsWild;

                    if (!matchesTarget && !isWild)
                    {
                        isFullMatch = false;
                        break;
                    }
                }
            }
            else
            {
                isFullMatch = false;
            }

            if (isFullMatch && targetSymbol != null)
            {
                result.IsWin = true;
                result.WinningSymbol = targetSymbol.Type;
                result.WinMultiplier = targetSymbol.Match3Multiplier;
                result.TotalPayout = betAmount * result.WinMultiplier * globalMultiplier;

                if (result.WinMultiplier >= 50)
                {
                    result.IsJackpot = true;
                    result.WinTitle = "BIG WIN!";
                }
                else
                {
                    result.WinTitle = "WINNER!";
                }

                result.WinDescription = $"3x {targetSymbol.DisplayName}! Payout: {result.TotalPayout:N0} Credits ({result.WinMultiplier}x)!";
                return result;
            }

            // 4. Consolation Feature: 2-of-a-kind match (Reel 1 & Reel 2 match)
            // Provides positive feedback and frequent low-tier rewards.
            if (reelsOutcome[0].Type == reelsOutcome[1].Type && !reelsOutcome[0].IsScatter)
            {
                var matched = reelsOutcome[0];
                if (matched.Match2Multiplier > 0)
                {
                    result.IsWin = true;
                    result.IsConsolationWin = true;
                    result.WinningSymbol = matched.Type;
                    result.WinMultiplier = matched.Match2Multiplier;
                    result.TotalPayout = betAmount * result.WinMultiplier * globalMultiplier;
                    result.WinTitle = "NICE HIT!";
                    result.WinDescription = $"2x {matched.DisplayName}! Consolation Win: {result.TotalPayout:N0} Credits!";
                    return result;
                }
            }

            return result;
        }
    }
}

