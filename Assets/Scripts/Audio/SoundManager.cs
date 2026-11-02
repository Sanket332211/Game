using UnityEngine;

namespace SlotGame.Audio
{
    /// <summary>
    /// Manages sound effects, audio mixing, and pitch escalation for reel stops.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource spinLoopSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip spinLoopClip;
        [SerializeField] private AudioClip[] reelStopClips;
        [SerializeField] private AudioClip winBellClip;
        [SerializeField] private AudioClip bigWinClip;
        [SerializeField] private AudioClip buttonClickClip;
        [SerializeField] private AudioClip leverPullClip;
        [SerializeField] private AudioClip coinsCascadeClip;

        private bool _isMuted = false;

        public bool IsMuted => _isMuted;

        private void Awake()
        {
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (spinLoopSource == null)
            {
                spinLoopSource = gameObject.AddComponent<AudioSource>();
                spinLoopSource.loop = true;
                spinLoopSource.playOnAwake = false;
            }
        }

        public void PlaySpinLoop()
        {
            if (_isMuted || spinLoopClip == null) return;
            spinLoopSource.clip = spinLoopClip;
            spinLoopSource.volume = 0.6f;
            spinLoopSource.Play();
        }

        public void StopSpinLoop()
        {
            if (spinLoopSource.isPlaying)
            {
                spinLoopSource.Stop();
            }
        }

        public void PlayReelStop(int reelIndex)
        {
            if (_isMuted) return;

            // Use indexed stop sound or vary pitch for dramatic escalation
            AudioClip clip = (reelStopClips != null && reelIndex < reelStopClips.Length && reelStopClips[reelIndex] != null)
                ? reelStopClips[reelIndex]
                : (reelStopClips != null && reelStopClips.Length > 0 ? reelStopClips[0] : null);

            if (clip != null)
            {
                // Slightly higher pitch for subsequent reels (1.0 -> 1.08 -> 1.16)
                sfxSource.pitch = 1.0f + (reelIndex * 0.08f);
                sfxSource.PlayOneShot(clip, 0.8f);
                sfxSource.pitch = 1.0f;
            }
        }

        public void PlayWinBell()
        {
            if (_isMuted || winBellClip == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(winBellClip, 0.9f);
        }

        public void PlayBigWin()
        {
            if (_isMuted || bigWinClip == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(bigWinClip, 1.0f);
            if (coinsCascadeClip != null)
            {
                sfxSource.PlayOneShot(coinsCascadeClip, 0.7f);
            }
        }

        public void PlayButtonClick()
        {
            if (_isMuted || buttonClickClip == null) return;
            sfxSource.pitch = Random.Range(0.95f, 1.05f);
            sfxSource.PlayOneShot(buttonClickClip, 0.7f);
            sfxSource.pitch = 1.0f;
        }

        public void PlayLeverPull()
        {
            if (_isMuted || leverPullClip == null) return;
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(leverPullClip, 0.85f);
        }

        public void ToggleMute()
        {
            _isMuted = !_isMuted;
            sfxSource.mute = _isMuted;
            spinLoopSource.mute = _isMuted;
        }
    }
}
