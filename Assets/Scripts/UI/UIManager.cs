using UnityEngine;
using UnityEngine.UI;

namespace SlotGame.UI
{
    /// <summary>
    /// Central UI presenter coordinating HUD readouts, button interactability,
    /// modal triggers, and audio toggles.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD Readouts")]
        [SerializeField] private Text balanceText;
        [SerializeField] private Text betText;
        [SerializeField] private Text winText;
        [SerializeField] private Text statusMessageText;

        [Header("Control Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button autoSpinButton;
        [SerializeField] private Text autoSpinButtonText;
        [SerializeField] private Button increaseBetButton;
        [SerializeField] private Button decreaseBetButton;
        [SerializeField] private Button maxBetButton;
        [SerializeField] private Button paytableButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private Text muteButtonText;

        [Header("Free Spins Indicator")]
        [SerializeField] private GameObject freeSpinsBanner;
        [SerializeField] private Text freeSpinsCounterText;

        [Header("Sub-View Controllers")]
        [SerializeField] private LeverAnimator leverAnimator;
        [SerializeField] private PaytableModal paytableModal;
        [SerializeField] private WinPopupController winPopup;

        public LeverAnimator Lever => leverAnimator;
        public PaytableModal Paytable => paytableModal;
        public WinPopupController WinPopup => winPopup;

        public void UpdateBalance(int balance)
        {
            if (balanceText != null) balanceText.text = $"{balance:N0}";
        }

        public void UpdateBet(int bet)
        {
            if (betText != null) betText.text = $"{bet:N0}";
        }

        public void UpdateWin(int win)
        {
            if (winText != null)
            {
                winText.text = win > 0 ? $"{win:N0}" : "0";
            }
        }

        public void SetStatusMessage(string message)
        {
            if (statusMessageText != null) statusMessageText.text = message;
        }

        public void SetSpinInteractable(bool interactable)
        {
            if (spinButton != null) spinButton.interactable = interactable;
            if (increaseBetButton != null) increaseBetButton.interactable = interactable;
            if (decreaseBetButton != null) decreaseBetButton.interactable = interactable;
            if (maxBetButton != null) maxBetButton.interactable = interactable;
        }

        public void UpdateAutoSpinState(bool isActive, int remainingSpins)
        {
            if (autoSpinButtonText != null)
            {
                autoSpinButtonText.text = isActive ? $"AUTO ({remainingSpins})" : "AUTO";
            }
        }

        public void SetFreeSpinsDisplay(bool isActive, int remainingSpins, int multiplier = 1)
        {
            if (freeSpinsBanner != null) freeSpinsBanner.SetActive(isActive);
            if (freeSpinsCounterText != null)
            {
                freeSpinsCounterText.text = isActive ? $"FREE SPINS: {remainingSpins} ({multiplier}x WIN!)" : "";
            }
        }

        public void UpdateMuteDisplay(bool isMuted)
        {
            if (muteButtonText != null)
            {
                muteButtonText.text = isMuted ? "UNMUTE" : "MUTE";
            }
        }
    }
}
