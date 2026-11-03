using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SlotGame.Audio;
using SlotGame.Bonus;
using SlotGame.UI;

namespace SlotGame.Core
{
    /// <summary>
    /// Master State Machine coordinating the complete slot machine lifecycle:
    /// Lever pull, staggered reel spinning, outcome evaluation, audio cues,
    /// economy updates, and bonus free spin automation.
    /// </summary>
    public class SlotMachineManager : MonoBehaviour
    {
        public enum MachineState
        {
            Idle,
            LeverPulling,
            ReelsSpinning,
            Evaluating,
            CelebratingWin
        }

        [Header("Symbol Catalog")]
        [Tooltip("List of all available symbols in the game with sprites and payouts.")]
        [SerializeField] private List<SymbolData> symbolCatalog = new List<SymbolData>();

        [Header("Reel Configuration")]
        [SerializeField] private ReelController[] reels;
        [SerializeField] private float baseSpinDuration = 1.2f;
        [SerializeField] private float reelStopStagger = 0.4f;

        [Header("Auto Spin Configuration")]
        [SerializeField] private int autoSpinBatchSize = 10;
        [SerializeField] private float autoSpinDelay = 1.0f;

        [Header("Subsystem References")]
        [SerializeField] private RNGManager rngManager;
        [SerializeField] private PaytableManager paytableManager;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private BonusFeatureManager bonusManager;
        [SerializeField] private SoundManager soundManager;
        [SerializeField] private UIManager uiManager;

        // Runtime State
        private MachineState _currentState = MachineState.Idle;
        private SymbolData[] _currentSpinOutcomes;
        private int _stoppedReelCount = 0;
        private bool _isAutoSpinActive = false;
        private int _remainingAutoSpins = 0;
        private Coroutine _autoSpinRoutine;

        public MachineState State => _currentState;
        public bool IsAutoSpinActive => _isAutoSpinActive;

        private void Start()
        {
            InitializeGame();
        }

        /// <summary>
        /// Boots up subsystems, registers symbols into the RNG, and binds UI events.
        /// </summary>
        public void InitializeGame()
        {
            // Fallback setup if not wired in Inspector
            if (rngManager == null) rngManager = GetComponentInChildren<RNGManager>();
            if (paytableManager == null) paytableManager = GetComponentInChildren<PaytableManager>();
            if (economyManager == null) economyManager = GetComponentInChildren<EconomyManager>();
            if (bonusManager == null) bonusManager = GetComponentInChildren<BonusFeatureManager>();
            if (soundManager == null) soundManager = GetComponentInChildren<SoundManager>();
            if (uiManager == null) uiManager = GetComponentInChildren<UIManager>();

            // Register symbols to RNG
            rngManager.RegisterSymbols(symbolCatalog);

            // Initialize each reel
            for (int i = 0; i < reels.Length; i++)
            {
                reels[i].Initialize(i, rngManager, symbolCatalog);
                reels[i].OnReelStopped += HandleReelStopped;
            }

            // Bind Economy events
            if (economyManager != null)
            {
                economyManager.OnBalanceChanged += uiManager.UpdateBalance;
                economyManager.OnBetChanged += uiManager.UpdateBet;
                economyManager.OnWinAwarded += uiManager.UpdateWin;
            }

            // Bind Bonus events
            if (bonusManager != null)
            {
                bonusManager.OnFreeSpinsStarted += (spins, mult) => uiManager.SetFreeSpinsDisplay(true, spins, mult);
                bonusManager.OnFreeSpinStep += (spins) => uiManager.SetFreeSpinsDisplay(true, spins, bonusManager.ActiveMultiplier);
                bonusManager.OnFreeSpinsCompleted += HandleFreeSpinsCompleted;
            }

            _currentState = MachineState.Idle;
            uiManager.SetStatusMessage("PULL LEVER OR PRESS SPIN TO PLAY!");
            uiManager.SetSpinInteractable(true);
        }

        /// <summary>
        /// Primary action trigger invoked by UI Spin Button, Spacebar, or Lever.
        /// </summary>
        public void RequestSpin()
        {
            if (_currentState != MachineState.Idle) return;

            // Check if user has sufficient balance or is currently in free spins
            bool isFreeSpin = bonusManager != null && bonusManager.IsBonusActive;
            if (!isFreeSpin)
            {
                if (!economyManager.TryDeductBet())
                {
                    uiManager.SetStatusMessage("INSUFFICIENT BALANCE! PLEASE RESET CREDITS.");
                    soundManager.PlayButtonClick();
                    StopAutoSpin();
                    return;
                }
            }

            StartCoroutine(ExecuteSpinSequence(isFreeSpin));
        }

        /// <summary>
        /// Coordinates the lever pull, spin sound, reel delays, and stop evaluation.
        /// </summary>
        private IEnumerator ExecuteSpinSequence(bool isFreeSpin)
        {
            _currentState = MachineState.LeverPulling;
            uiManager.SetSpinInteractable(false);
            uiManager.UpdateWin(0);

            if (isFreeSpin)
            {
                uiManager.SetStatusMessage($"FREE SPIN! ({bonusManager.RemainingFreeSpins} LEFT - {bonusManager.ActiveMultiplier}x MULTIPLIER)");
            }
            else
            {
                uiManager.SetStatusMessage("GOOD LUCK! REELS ARE ROLLING...");
            }

            // 1. Play Lever Animation and Sound
            soundManager.PlayLeverPull();
            bool leverReachedBottom = false;

            if (uiManager.Lever != null)
            {
                uiManager.Lever.PlayLeverPull(onBottomReached: () => leverReachedBottom = true);
                while (!leverReachedBottom) yield return null;
            }

            // 2. Determine Spin Outcomes via RNG
            _currentState = MachineState.ReelsSpinning;
            _stoppedReelCount = 0;
            _currentSpinOutcomes = rngManager.GenerateSpinOutcomes(reels.Length);

            // 3. Start audio loop
            soundManager.PlaySpinLoop();

            // 4. Trigger Reel Spins with staggered stop times for suspense
            for (int i = 0; i < reels.Length; i++)
            {
                float spinTime = baseSpinDuration + (i * reelStopStagger);
                reels[i].StartSpin(_currentSpinOutcomes[i], spinTime);
            }

            // Wait for all reels to stop
            while (_stoppedReelCount < reels.Length)
            {
                yield return null;
            }

            // 5. Evaluate Results
            soundManager.StopSpinLoop();
            _currentState = MachineState.Evaluating;
            yield return new WaitForSeconds(0.2f);

            EvaluateSpinOutcome(isFreeSpin);
        }

        private void HandleReelStopped(int reelIndex, SymbolData paylineSymbol)
        {
            _stoppedReelCount++;
            soundManager.PlayReelStop(reelIndex);
        }

        /// <summary>
        /// Evaluates matching symbols, triggers payouts, bonus rounds, and auto-spins.
        /// </summary>
        private void EvaluateSpinOutcome(bool isFreeSpin)
        {
            int multiplier = bonusManager != null ? bonusManager.ActiveMultiplier : 1;
            SpinResult result = paytableManager.EvaluateOutcome(_currentSpinOutcomes, economyManager.CurrentBet, multiplier);

            if (result.IsWin)
            {
                _currentState = MachineState.CelebratingWin;
                economyManager.AwardPayout(result.TotalPayout);
                uiManager.SetStatusMessage(result.WinTitle + " " + result.WinDescription);

                if (result.IsJackpot)
                {
                    soundManager.PlayBigWin();
                }
                else
                {
                    soundManager.PlayWinBell();
                }

                if (result.TriggeredFreeSpins && bonusManager != null)
                {
                    bonusManager.TriggerFreeSpins(result.FreeSpinsAwarded);
                }

                // Show visual celebration popup for notable wins
                if (result.TotalPayout >= economyManager.CurrentBet * 5 || result.TriggeredFreeSpins)
                {
                    uiManager.WinPopup.ShowWin(result.WinTitle, result.WinDescription, result.TotalPayout, result.IsJackpot, onDismiss: () =>
                    {
                        FinishSpinCycle(isFreeSpin, result.TotalPayout);
                    });
                    return;
                }
            }
            else
            {
                uiManager.SetStatusMessage(result.WinDescription);
            }

            FinishSpinCycle(isFreeSpin, result.TotalPayout);
        }

        private void FinishSpinCycle(bool wasFreeSpin, int payout)
        {
            if (wasFreeSpin && bonusManager != null)
            {
                bonusManager.RecordSpinOutcome(payout);
            }

            _currentState = MachineState.Idle;

            // Check if next auto spin or free spin should execute
            if (bonusManager != null && bonusManager.IsBonusActive)
            {
                StartCoroutine(TriggerNextDelayedSpin(autoSpinDelay));
            }
            else if (_isAutoSpinActive)
            {
                _remainingAutoSpins--;
                uiManager.UpdateAutoSpinState(true, _remainingAutoSpins);

                if (_remainingAutoSpins > 0 && economyManager.CanAffordCurrentBet)
                {
                    StartCoroutine(TriggerNextDelayedSpin(autoSpinDelay));
                }
                else
                {
                    StopAutoSpin();
                }
            }
            else
            {
                uiManager.SetSpinInteractable(true);
            }
        }

        private IEnumerator TriggerNextDelayedSpin(float delay)
        {
            yield return new WaitForSeconds(delay);
            RequestSpin();
        }

        public void ToggleAutoSpin()
        {
            soundManager.PlayButtonClick();

            if (_isAutoSpinActive)
            {
                StopAutoSpin();
            }
            else
            {
                _isAutoSpinActive = true;
                _remainingAutoSpins = autoSpinBatchSize;
                uiManager.UpdateAutoSpinState(true, _remainingAutoSpins);

                if (_currentState == MachineState.Idle)
                {
                    RequestSpin();
                }
            }
        }

        public void StopAutoSpin()
        {
            _isAutoSpinActive = false;
            _remainingAutoSpins = 0;
            uiManager.UpdateAutoSpinState(false, 0);
            if (_currentState == MachineState.Idle)
            {
                uiManager.SetSpinInteractable(true);
            }
        }

        private void HandleFreeSpinsCompleted(int totalBonusWin)
        {
            uiManager.SetFreeSpinsDisplay(false, 0);
            uiManager.WinPopup.ShowWin("FREE SPINS COMPLETED!", $"Congratulations! Total Bonus Won: {totalBonusWin:N0} Credits!", totalBonusWin, true, () =>
            {
                uiManager.SetSpinInteractable(true);
            });
        }

        // Keyboard hotkeys for testing convenience
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                RequestSpin();
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                ToggleAutoSpin();
            }
            else if (Input.GetKeyDown(KeyCode.P))
            {
                uiManager.Paytable.Show();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                soundManager.ToggleMute();
                uiManager.UpdateMuteDisplay(soundManager.IsMuted);
            }
        }
    }
}

