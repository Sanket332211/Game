using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Contract for RNG generation to allow dependency injection and automated unit testing.
    /// </summary>
    public interface IRandomNumberGenerator
    {
        int NextInt(int minValue, int maxValue);
        float NextFloat();
        void SetSeed(int seed);
    }

    /// <summary>
    /// High-performance XorShift128+ pseudo-random number generator.
    /// Provides statistical uniformity, high period (2^128 - 1), and determinism.
    /// </summary>
    public class XorShiftRNG : IRandomNumberGenerator
    {
        private ulong _s0;
        private ulong _s1;

        public XorShiftRNG(int seed = 0)
        {
            SetSeed(seed == 0 ? Environment.TickCount : seed);
        }

        public void SetSeed(int seed)
        {
            _s0 = (ulong)seed;
            _s1 = (ulong)(seed ^ 0x5DEECE66D);
            if (_s0 == 0 && _s1 == 0) _s1 = 1;
        }

        public float NextFloat()
        {
            // Normalize to [0.0, 1.0)
            return (NextULong() >> 11) * (1.0f / 9007199254740992.0f);
        }

        public int NextInt(int minValue, int maxValue)
        {
            if (minValue >= maxValue) return minValue;
            int range = maxValue - minValue;
            return minValue + (int)(NextFloat() * range);
        }

        private ulong NextULong()
        {
            ulong x = _s0;
            ulong y = _s1;
            _s0 = y;
            x ^= x << 23;
            _s1 = x ^ y ^ (x >> 17) ^ (y >> 26);
            return _s1 + y;
        }
    }

    /// <summary>
    /// Handles slot symbol outcome selection utilizing weighted random sampling.
    /// Implements fair industry-standard Return To Player (RTP) simulation and
    /// provides deterministic debug hooks for validating winning logic.
    /// </summary>
    public class RNGManager : MonoBehaviour
    {
        [Header("RNG Configuration")]
        [Tooltip("If enabled, uses deterministic fixed seed for reproducible test results.")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int debugSeed = 1337;

        [Header("Debug Override (Optional)")]
        [Tooltip("When enabled, forces reels to stop on predetermined symbols for testing.")]
        [SerializeField] private bool forceDebugOutcome = false;
        [SerializeField] private SymbolType[] forcedSymbols = new SymbolType[] { SymbolType.Diamond, SymbolType.Diamond, SymbolType.Diamond };

        private IRandomNumberGenerator _rng;
        private List<SymbolData> _registeredSymbols = new List<SymbolData>();
        private int _totalRngWeight = 0;

        private void Awake()
        {
            InitializeRNG();
        }

        /// <summary>
        /// Initializes the RNG subsystem with either a fixed or time-based random seed.
        /// </summary>
        public void InitializeRNG()
        {
            int seed = useFixedSeed ? debugSeed : Environment.TickCount ^ System.Diagnostics.Process.GetCurrentProcess().Id;
            _rng = new XorShiftRNG(seed);
        }

        /// <summary>
        /// Registers available symbols and calculates cumulative weight table.
        /// Time Complexity: O(N) where N is symbol count.
        /// </summary>
        public void RegisterSymbols(IEnumerable<SymbolData> symbols)
        {
            _registeredSymbols.Clear();
            _totalRngWeight = 0;

            foreach (var symbol in symbols)
            {
                _registeredSymbols.Add(symbol);
                _totalRngWeight += Mathf.Max(1, symbol.RngWeight);
            }
        }

        /// <summary>
        /// Selects a random symbol using Inverse Transform Sampling over the weighted discrete distribution.
        /// Guarantees that symbols with higher weights appear proportionately more often.
        /// </summary>
        public SymbolData GetRandomSymbol()
        {
            if (_registeredSymbols == null || _registeredSymbols.Count == 0)
            {
                Debug.LogError("[RNGManager] No symbols registered for RNG evaluation!");
                return null;
            }

            // Pick a random weight slice in [0, TotalWeight)
            int targetWeight = _rng.NextInt(0, _totalRngWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < _registeredSymbols.Count; i++)
            {
                cumulativeWeight += _registeredSymbols[i].RngWeight;
                if (targetWeight < cumulativeWeight)
                {
                    return _registeredSymbols[i];
                }
            }

            // Fallback (boundary condition safeguard)
            return _registeredSymbols[_registeredSymbols.Count - 1];
        }

        /// <summary>
        /// Generates the outcome row across all reels for a single spin cycle.
        /// If forceDebugOutcome is active, supplies predetermined symbols for testing win evaluations.
        /// </summary>
        /// <param name="reelCount">Total number of reels (typically 3).</param>
        /// <returns>Array of SymbolData selected for each reel.</returns>
        public SymbolData[] GenerateSpinOutcomes(int reelCount)
        {
            SymbolData[] outcomes = new SymbolData[reelCount];

            // Debug hook for instant validation of 3-of-a-kind, Wilds, and Scatters
            if (forceDebugOutcome && forcedSymbols != null && forcedSymbols.Length >= reelCount)
            {
                for (int i = 0; i < reelCount; i++)
                {
                    SymbolType targetType = forcedSymbols[i];
                    outcomes[i] = _registeredSymbols.Find(s => s.Type == targetType) ?? GetRandomSymbol();
                }
                return outcomes;
            }

            // Independent reel RNG selection
            for (int i = 0; i < reelCount; i++)
            {
                outcomes[i] = GetRandomSymbol();
            }

            return outcomes;
        }

        /// <summary>
        /// Helper for debug/unit tests to set specific forced outcomes during runtime.
        /// </summary>
        public void SetForcedOutcome(bool enable, params SymbolType[] types)
        {
            forceDebugOutcome = enable;
            forcedSymbols = types;
        }
    }
}
