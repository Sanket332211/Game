using System;
using UnityEngine;

namespace SlotGame.Core
{
    /// <summary>
    /// Configuration data for an individual slot machine symbol.
    /// Can be instantiated as data assets or configured via Inspector.
    /// </summary>
    [Serializable]
    public class SymbolData
    {
        [Header("Symbol Identity")]
        [Tooltip("Type identifier for this symbol.")]
        [SerializeField] private SymbolType type;

        [Tooltip("Display name shown in UI and paytable.")]
        [SerializeField] private string displayName;

        [Tooltip("Sprite rendered on the reel.")]
        [SerializeField] private Sprite icon;

        [Header("Payout Multipliers")]
        [Tooltip("Payout multiplier awarded when all reels match this symbol (3-of-a-kind).")]
        [SerializeField] private int match3Multiplier = 5;

        [Tooltip("Payout multiplier awarded when 2 matching symbols land (consolation win).")]
        [SerializeField] private int match2Multiplier = 1;

        [Header("RNG & Weighting")]
        [Tooltip("Relative selection weight for RNG. Higher weight means higher hit frequency.")]
        [Range(1, 100)]
        [SerializeField] private int rngWeight = 25;

        [Header("Bonus Properties")]
        [Tooltip("Indicates whether this symbol acts as a Wild (substitutes other paying symbols).")]
        [SerializeField] private bool isWild;

        [Tooltip("Indicates whether this symbol acts as a Scatter (triggers Free Spins bonus).")]
        [SerializeField] private bool isScatter;

        // Public Read-Only Properties
        public SymbolType Type => type;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int Match3Multiplier => match3Multiplier;
        public int Match2Multiplier => match2Multiplier;
        public int RngWeight => rngWeight;
        public bool IsWild => isWild;
        public bool IsScatter => isScatter;

        /// <summary>
        /// Constructor for programmatic creation or testing.
        /// </summary>
        public SymbolData(SymbolType type, string displayName, Sprite icon, int match3Mult, int match2Mult, int weight, bool isWild = false, bool isScatter = false)
        {
            this.type = type;
            this.displayName = displayName;
            this.icon = icon;
            this.match3Multiplier = match3Mult;
            this.match2Multiplier = match2Mult;
            this.rngWeight = weight;
            this.isWild = isWild;
            this.isScatter = isScatter;
        }
    }
}
