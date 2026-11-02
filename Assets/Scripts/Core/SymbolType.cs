namespace SlotGame.Core
{
    /// <summary>
    /// Enumerates all symbol types available in the slot machine.
    /// Includes classic fruit/casino symbols and special bonus symbols.
    /// </summary>
    public enum SymbolType
    {
        Cherry = 0,             // Classic low-tier symbol (slot-symbol1)
        Orange = 1,             // Medium-low tier symbol (slot-symbol2)
        Bell = 2,               // Medium-high tier symbol (slot-symbol3)
        Diamond = 3,            // High-tier premium symbol (slot-symbol4)
        Wild = 4,               // Bonus: Substitutes for any regular paying symbol
        Scatter = 5             // Bonus: Triggers Free Spins feature
    }
}
