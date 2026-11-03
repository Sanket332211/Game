using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.Core
{
    /// <summary>
    /// Controls the physical motion, visual recycling, and bounce-back animation
    /// of an individual slot machine reel column.
    /// </summary>
    public class ReelController : MonoBehaviour
    {
        [Header("Reel Identity")]
        [SerializeField] private int reelIndex = 0;

        [Header("Symbol Layout Configuration")]
        [Tooltip("Height of each symbol cell in local units/pixels.")]
        [SerializeField] private float symbolHeight = 120f;

        [Tooltip("Total active symbol views managed by this reel (e.g. 5: 1 above, 3 visible, 1 below).")]
        [SerializeField] private int totalSymbolViews = 5;

        [Tooltip("UI Image components representing the vertical strip of symbols.")]
        [SerializeField] private Image[] symbolImages;

        [Header("Animation Tuning (Game Feel)")]
        [Tooltip("Anticipation recoil distance in pixels (slight upward movement before spin).")]
        [SerializeField] private float anticipationDistance = 25f;
        [SerializeField] private float anticipationDuration = 0.15f;

        [Tooltip("Maximum spinning speed in pixels per second.")]
        [SerializeField] private float maxSpinSpeed = 1600f;

        [Tooltip("Acceleration ramp duration in seconds.")]
        [SerializeField] private float accelerationTime = 0.25f;

        [Tooltip("Deceleration easing duration.")]
        [SerializeField] private float decelerationTime = 0.45f;

        [Tooltip("Elastic bounce overshoot amplitude in pixels upon landing.")]
        [SerializeField] private float bounceOvershoot = 22f;
        [SerializeField] private float bounceDuration = 0.25f;

        // Dependencies
        private RNGManager _rngManager;
        private RectTransform _rectTransform;

        // Internal State
        private bool _isSpinning = false;
        private SymbolData _targetPaylineSymbol;
        private List<SymbolData> _currentSymbolsOnStrip = new List<SymbolData>();
        private float[] _symbolYPositions;
        private Coroutine _spinCoroutine;

        // Events
        public event Action<int, SymbolData> OnReelStopped;

        public bool IsSpinning => _isSpinning;
        public int ReelIndex => reelIndex;
        public SymbolData CurrentPaylineSymbol => _targetPaylineSymbol;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            InitializePositions();
        }

        /// <summary>
        /// Initializes the symbol array positions and binds the RNG provider.
        /// </summary>
        public void Initialize(int index, RNGManager rng, List<SymbolData> initialSymbols)
        {
            this.reelIndex = index;
            this._rngManager = rng;

            if (symbolImages == null || symbolImages.Length == 0)
            {
                symbolImages = GetComponentsInChildren<Image>(true);
            }

            _currentSymbolsOnStrip.Clear();
            for (int i = 0; i < symbolImages.Length; i++)
            {
                SymbolData sym = (initialSymbols != null && i < initialSymbols.Count) 
                    ? initialSymbols[i] 
                    : _rngManager.GetRandomSymbol();

                _currentSymbolsOnStrip.Add(sym);
                UpdateSymbolVisual(i, sym);
            }

            // Middle symbol is the initial payline symbol
            int centerIdx = symbolImages.Length / 2;
            _targetPaylineSymbol = _currentSymbolsOnStrip[centerIdx];
        }

        private void InitializePositions()
        {
            int count = (symbolImages != null && symbolImages.Length > 0) ? symbolImages.Length : totalSymbolViews;
            _symbolYPositions = new float[count];

            int centerIndex = count / 2;
            for (int i = 0; i < count; i++)
            {
                // Align relative to center (0, 0)
                _symbolYPositions[i] = (centerIndex - i) * symbolHeight;
                if (symbolImages != null && i < symbolImages.Length)
                {
                    symbolImages[i].rectTransform.anchoredPosition = new Vector2(0f, _symbolYPositions[i]);
                }
            }
        }

        /// <summary>
        /// Begins spinning the reel. Uses coroutine for frame-rate independent physics animation.
        /// </summary>
        /// <param name="targetSymbol">Pre-determined symbol that will stop on the payline.</param>
        /// <param name="spinDuration">Minimum duration before deceleration begins.</param>
        public void StartSpin(SymbolData targetSymbol, float spinDuration)
        {
            if (_isSpinning) return;

            _targetPaylineSymbol = targetSymbol;
            if (_spinCoroutine != null) StopCoroutine(_spinCoroutine);
            _spinCoroutine = StartCoroutine(SpinRoutine(targetSymbol, spinDuration));
        }

        /// <summary>
        /// 5-Stage Physics Animation Coroutine:
        /// 1. Anticipation (Upward recoil)
        /// 2. Acceleration (Ramping down)
        /// 3. Steady Spin (High speed cycling)
        /// 4. Deceleration (Target alignment glide)
        /// 5. Damped Spring Bounce-Back (Snapping into place)
        /// </summary>
        private IEnumerator SpinRoutine(SymbolData targetSymbol, float minSpinDuration)
        {
            _isSpinning = true;
            int count = symbolImages.Length;
            int centerIndex = count / 2;
            float stripHeight = count * symbolHeight;

            // -----------------------------------------------------------------
            // STAGE 1: Anticipation (Upward recoil)
            // Creates physical spring tension before spinning downwards.
            // -----------------------------------------------------------------
            float elapsed = 0f;
            while (elapsed < anticipationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / anticipationDuration);
                // Ease-out quadratic nudge upward
                float offset = Mathf.Sin(t * Mathf.PI * 0.5f) * anticipationDistance;

                for (int i = 0; i < count; i++)
                {
                    symbolImages[i].rectTransform.anchoredPosition = new Vector2(0f, _symbolYPositions[i] + offset);
                }
                yield return null;
            }

            // -----------------------------------------------------------------
            // STAGE 2: Acceleration & STAGE 3: Steady High-Speed Spin
            // -----------------------------------------------------------------
            float currentSpeed = 0f;
            float spinTimer = 0f;

            while (spinTimer < minSpinDuration || currentSpeed < maxSpinSpeed * 0.9f)
            {
                float dt = Time.deltaTime;
                spinTimer += dt;

                // Smooth quadratic speed ramp
                if (currentSpeed < maxSpinSpeed)
                {
                    currentSpeed += (maxSpinSpeed / accelerationTime) * dt;
                    currentSpeed = Mathf.Min(currentSpeed, maxSpinSpeed);
                }

                // Move symbols downwards
                ShiftSymbolsDown(currentSpeed * dt);
                yield return null;
            }

            // -----------------------------------------------------------------
            // STAGE 4: Deceleration & Target Positioning
            // Smoothly glide until the target symbol lands on the center payline.
            // -----------------------------------------------------------------
            // Inject target symbol above the payline so it cycles into view
            _currentSymbolsOnStrip[0] = targetSymbol;
            UpdateSymbolVisual(0, targetSymbol);

            float decelTimer = 0f;
            float startSpeed = currentSpeed;

            while (decelTimer < decelerationTime)
            {
                float dt = Time.deltaTime;
                decelTimer += dt;
                float progress = decelTimer / decelerationTime;

                // Cubic ease-out deceleration
                currentSpeed = Mathf.Lerp(startSpeed, 80f, 1f - Mathf.Pow(1f - progress, 3));
                ShiftSymbolsDown(currentSpeed * dt);
                yield return null;
            }

            // Align symbols to nearest slot positions
            for (int i = 0; i < count; i++)
            {
                _currentSymbolsOnStrip[i] = (i == centerIndex) ? targetSymbol : _rngManager.GetRandomSymbol();
                UpdateSymbolVisual(i, _currentSymbolsOnStrip[i]);
            }

            // -----------------------------------------------------------------
            // STAGE 5: Damped Spring Bounce-Back
            // f(t) = exp(-decay * t) * cos(frequency * t)
            // -----------------------------------------------------------------
            elapsed = 0f;
            float decay = 12f;
            float frequency = 24f;

            while (elapsed < bounceDuration)
            {
                elapsed += Time.deltaTime;
                float damping = Mathf.Exp(-decay * elapsed);
                float oscillation = Mathf.Sin(frequency * elapsed);
                float bounceOffset = -bounceOvershoot * damping * oscillation;

                for (int i = 0; i < count; i++)
                {
                    symbolImages[i].rectTransform.anchoredPosition = new Vector2(0f, _symbolYPositions[i] + bounceOffset);
                }
                yield return null;
            }

            // Final exact position lock
            for (int i = 0; i < count; i++)
            {
                symbolImages[i].rectTransform.anchoredPosition = new Vector2(0f, _symbolYPositions[i]);
            }

            _isSpinning = false;
            OnReelStopped?.Invoke(reelIndex, targetSymbol);
        }

        /// <summary>
        /// Scrolls symbols downward and recycles symbols that cross the bottom threshold.
        /// </summary>
        private void ShiftSymbolsDown(float deltaY)
        {
            int count = symbolImages.Length;
            float topY = _symbolYPositions[0];
            float bottomThreshold = _symbolYPositions[count - 1] - (symbolHeight * 0.5f);

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = symbolImages[i].rectTransform.anchoredPosition;
                pos.y -= deltaY;

                // Wrap-around recycling
                if (pos.y < bottomThreshold)
                {
                    pos.y += count * symbolHeight;
                    // Pick new random symbol on recycle
                    var newSym = _rngManager.GetRandomSymbol();
                    _currentSymbolsOnStrip[i] = newSym;
                    UpdateSymbolVisual(i, newSym);
                }

                symbolImages[i].rectTransform.anchoredPosition = pos;
            }
        }

        private void UpdateSymbolVisual(int index, SymbolData symbol)
        {
            if (index < 0 || index >= symbolImages.Length || symbol == null) return;
            symbolImages[index].sprite = symbol.Icon;
            symbolImages[index].preserveAspect = true;
        }
    }
}

