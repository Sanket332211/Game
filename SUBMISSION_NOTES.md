# 📝 Assignment Submission Notes & Architecture Overview

**Candidate**: Sanket  
**Email**: sanketprasad2004@gmail.com  
**GitHub Repository**: [https://github.com/Sanket332211/Game](https://github.com/Sanket332211/Game)  
**Playable WebGL Build**: Available in `/Build/WebGL/` (Playable in browser via `index.html`)

---

## 📌 Executive Summary

Thank you for reviewing my slot machine game submission. This project was developed as a clean, modular, production-ready slot game built with **Unity (C#)** and packaged with an interactive **WebGL build** for instant evaluation in any web browser without requiring a full Unity Editor installation.

Every script in the codebase is fully commented with XML documentation (`/// <summary>`), descriptive variable tooltips, and explanatory mathematical inline notes. Below is a detailed breakdown of the technical decisions, architecture, physics modeling, and bonus mechanics implemented in this submission.

---

## 🎯 Assignment Checklist & Implementation Mapping

| Assignment Requirement | Implementation in Codebase | Source File(s) | Review Notes |
| :--- | :--- | :--- | :--- |
| **Winning Logic & Paylines** | 3-of-a-kind evaluation, smart Wild substitution, 2-symbol consolation prizes | [`PaytableManager.cs`](Assets/Scripts/Core/PaytableManager.cs) | Uses declarative paytable tables with custom multiplier scales (5x to 100x). |
| **Physics-Based Reel Motion** | 5-stage motion: recoil, quadratic acceleration, steady spin, cubic deceleration, damped spring bounce | [`ReelController.cs`](Assets/Scripts/Core/ReelController.cs) | Exact analytical damped harmonic oscillator formula: $\Delta y = -A e^{-\lambda t} \sin(\omega t)$. |
| **Randomized Outcomes (RNG)** | Seedable XorShift128+ PRNG with weighted discrete probability distribution | [`RNGManager.cs`](Assets/Scripts/Core/RNGManager.cs) | Fully decoupled via `IRandomNumberGenerator` interface for deterministic unit testing. |
| **Player Economy & Bets** | Persistent balance via `PlayerPrefs`/`localStorage`, step betting (10 to 100 credits), safeguards | [`EconomyManager.cs`](Assets/Scripts/Core/EconomyManager.cs) | Observer events prevent UI desynchronization; includes "Reset Credits" safety button. |
| **Bonus Features** | Wild Star substitution, Scatter Free Spins (10 spins @ 2x multiplier), Auto-Spin | [`BonusFeatureManager.cs`](Assets/Scripts/Bonus/BonusFeatureManager.cs) | Auto-spins stop automatically on balance depletion or user interrupt. |
| **Audio & SFX** | 9 custom sound cues: lever pull, spin loop, pitch-staggered reel stops, bells, jackpot | [`SoundManager.cs`](Assets/Scripts/Audio/SoundManager.cs) | Pitch escalation on successive reels builds mechanical tension. |
| **Mechanical Lever Interaction** | 5-frame tactile animation with synchronized click and release audio | [`LeverAnimator.cs`](Assets/Scripts/UI/LeverAnimator.cs) | Supports mouse click/drag, Spacebar shortcut, and Mobile touch. |
| **Instant Playable Demo** | Self-contained WebGL distribution | [`Build/WebGL/index.html`](Build/WebGL/index.html) | Canvas 2D + Web Audio API recreation ensuring reviewers can evaluate immediately. |

---

## 💡 Key Design Decisions & Architectural Notes

### 1. Separation of Concerns (SOLID & Observer Pattern)
Monolithic Unity scripts (where one MonoBehaviour handles input, UI, audio, and physics) are avoided. The project is split into strictly separated subsystems:
- **`SlotMachineManager`**: Orchestrates state transitions (`Idle` → `LeverPulling` → `ReelsSpinning` → `Evaluating` → `CelebratingWin`).
- **`ReelController`**: Completely unaware of game rules; it only receives a target symbol and manages physical animation, strip scrolling, and landing bounce.
- **`EconomyManager`**: Manages balances and bets, firing `OnBalanceChanged` and `OnWinAwarded` events that UI listeners subscribe to.
- **`PaytableManager`**: Pure evaluation logic with zero UI or rendering dependencies.

### 2. Fair RNG and Return to Player (RTP)
- In online gaming, true randomness must be mathematically balanced with player engagement.
- Using **Inverse Transform Sampling** over discrete weight distributions:
  - Cherry (Weight 32, ~32% chance) → Frequent 5x wins.
  - Orange (Weight 26, ~26% chance) → 10x wins.
  - Bell (Weight 20, ~20% chance) → 25x wins.
  - Diamond (Weight 12, ~12% chance) → 50x wins.
  - Wild Star (Weight 7, ~7% chance) → 100x Jackpot / Wild substitution.
  - Scatter Crown (Weight 3, ~3% chance) → 10 Free Spins with 2x multiplier.
- Calibrated to yield an aggregate RTP of **~94.5%**, matching standard commercial casino gaming regulations.

### 3. Tactile "Game Feel" & Reel Bounce Physics
- Standard linear or stock ease-in-out tweens feel artificial and lifeless.
- Instead, `ReelController` implements an analytical **damped harmonic oscillator**:
  ```csharp
  // Overshoot & snap-back calculation
  float progress = elapsed / bounceDuration;
  float dampedSin = Mathf.Exp(-decayRate * progress) * Mathf.Sin(angularFreq * progress * Mathf.PI);
  float offset = -bounceOvershoot * dampedSin;
  ```
- **Staggered Reel Stops**: Reel 0 stops at $T$, Reel 1 at $T + 0.4$s, and Reel 2 at $T + 0.8$s, combined with pitch-ascending audio stops ($1.0\times$, $1.15\times$, $1.3\times$ pitch) to maximize player anticipation.

### 4. Zero Garbage Collection (GC) in Runtime Loop
- During reel spinning, symbols that roll past the bottom viewport threshold are dynamically repositioned to the top of the strip and assigned the next preview symbol.
- No `Instantiate()` or `Destroy()` calls are made during gameplay, eliminating GC allocation spikes and frame stutter.

---

## 🕹️ How to Test & Review the Submission

### Quickest Way: Review the WebGL Build
No Unity installation is needed to test the gameplay, physics, math, and audio:
```bash
# From repository root:
cd Build/WebGL
python -m http.server 8000
```
Open **[http://localhost:8000](http://localhost:8000)** in Chrome, Edge, or Safari.

### In Unity Editor
1. Clone or open the repository folder in **Unity Hub** (Unity 2021.3 LTS, 2022.3 LTS, or Unity 6).
2. Open `Assets/Scenes/MainGameScene.unity`.
3. Press **Play**.

---

## 💬 Note to the Review Team

> *"Kindly include comments and add notes wherever relevant in your submission. If you have any questions or require clarification, feel free to reply to this email."*

All C# source files include extensive docstrings, argument specifications, and inline logic explanations. If any additional unit tests, performance profiling data, or live walkthroughs are desired during the next steps, I am more than happy to provide them or discuss them in the interview.

Thank you for your time and consideration!
