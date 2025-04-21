using System.Collections;
using UnityEngine.UI;
using UnityEngine;

namespace com.thelegends.sound.manager
{
    public class UISoundManagerController : MonoBehaviour
    {
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider vfxVolumeSlider;
        [SerializeField] private Slider uiVolumeSlider;
        [SerializeField] private Toggle masterToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Toggle uiToggle;

        private void OnEnable()
        {
            StartCoroutine(InitializeAudioControls());
        }

        private void OnDisable()
        {
            // Remove listeners to prevent memory leaks
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            vfxVolumeSlider.onValueChanged.RemoveListener(OnVfxVolumeChanged);
            uiVolumeSlider.onValueChanged.RemoveListener(OnUIVolumeChanged);

            masterToggle.onValueChanged.RemoveAllListeners();
            musicToggle.onValueChanged.RemoveAllListeners();
            sfxToggle.onValueChanged.RemoveAllListeners();
            uiToggle.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// Initializes the audio control UI elements after a frame delay
        /// to ensure SoundManager is fully initialized
        /// </summary>
        private IEnumerator InitializeAudioControls()
        {
            yield return new WaitForEndOfFrame();

            // Initialize slider values from SoundManager
            masterVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Master);
            musicVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Music);
            vfxVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Vfx);
            uiVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.UI);

            // Add listeners for sliders
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            vfxVolumeSlider.onValueChanged.AddListener(OnVfxVolumeChanged);
            uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);

            // Initialize toggle states based on current volume levels
            // REVERSED LOGIC: ON = muted (volume near 0), OFF = unmuted (has volume)
            masterToggle.isOn = masterVolumeSlider.value < 0.01f;
            musicToggle.isOn = musicVolumeSlider.value < 0.01f;
            sfxToggle.isOn = vfxVolumeSlider.value < 0.01f;
            uiToggle.isOn = uiVolumeSlider.value < 0.01f;

            // Add listeners for toggles after initializing their values
            masterToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.Master, isOn));
            musicToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.Music, isOn));
            sfxToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.Vfx, isOn));
            uiToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.UI, isOn));
        }

        /// <summary>
        /// Handles changes in audio mute toggle states
        /// </summary>
        /// <param name="channelType">The audio channel affected</param>
        /// <param name="isOn">Toggle state (ON = muted, OFF = unmuted)</param>
        private void HandleToggleChange(AudioChannelType channelType, bool isOn)
        {
            if (isOn)
            {
                // Toggle is ON = MUTE (turn off audio)
                // If not already muted, use ToggleMute to store current volume before muting
                if (SoundManager.Instance.GetVolume(channelType) >= 0.01f)
                {
                    SoundManager.Instance.ToggleMute(channelType);
                }
                // If already muted, do nothing
            }
            else
            {
                // Toggle is OFF = UNMUTE (turn on audio)
                // If already muted, use ToggleMute to restore previous volume
                if (SoundManager.Instance.GetVolume(channelType) < 0.01f)
                {
                    SoundManager.Instance.ToggleMute(channelType);
                }
                // If already unmuted but value is very low, set to a reasonable default
                else if (SoundManager.Instance.GetVolume(channelType) < 0.1f)
                {
                    SoundManager.Instance.SetVolume(channelType, 0.5f);
                }
                // Otherwise, keep the current volume
            }

            // Update the slider to reflect the new volume without triggering the onValueChanged event
            UpdateSliderWithoutNotify(channelType, SoundManager.Instance.GetVolume(channelType));

            // Also update the toggle state to match the actual mute state (reversed logic)
            // Note that we reverse the logic here to match our UI convention
            UpdateToggleWithoutNotify(channelType, SoundManager.Instance.GetVolume(channelType) < 0.01f);
        }

        /// <summary>
        /// Updates the slider value without triggering the onValueChanged event
        /// </summary>
        /// <param name="channelType">Audio channel whose slider to update</param>
        /// <param name="value">New slider value</param>
        private void UpdateSliderWithoutNotify(AudioChannelType channelType, float value)
        {
            switch (channelType)
            {
                case AudioChannelType.Master:
                    masterVolumeSlider.SetValueWithoutNotify(value);
                    break;
                case AudioChannelType.Music:
                    musicVolumeSlider.SetValueWithoutNotify(value);
                    break;
                case AudioChannelType.Vfx:
                    vfxVolumeSlider.SetValueWithoutNotify(value);
                    break;
                case AudioChannelType.UI:
                    uiVolumeSlider.SetValueWithoutNotify(value);
                    break;
            }
        }

        /// <summary>
        /// Updates the toggle state without triggering the onValueChanged event
        /// </summary>
        /// <param name="channelType">Audio channel whose toggle to update</param>
        /// <param name="isOn">New toggle state</param>
        private void UpdateToggleWithoutNotify(AudioChannelType channelType, bool isOn)
        {
            switch (channelType)
            {
                case AudioChannelType.Master:
                    masterToggle.SetIsOnWithoutNotify(isOn);
                    break;
                case AudioChannelType.Music:
                    musicToggle.SetIsOnWithoutNotify(isOn);
                    break;
                case AudioChannelType.Vfx:
                    sfxToggle.SetIsOnWithoutNotify(isOn);
                    break;
                case AudioChannelType.UI:
                    uiToggle.SetIsOnWithoutNotify(isOn);
                    break;
            }
        }

        /// <summary>
        /// Handles master volume slider value changes
        /// </summary>
        /// <param name="value">New volume value</param>
        private void OnMasterVolumeChanged(float value)
        {
            SoundManager.Instance.SetVolume(AudioChannelType.Master, value);
            UpdateToggleWithoutNotify(AudioChannelType.Master, value < 0.01f);
        }

        /// <summary>
        /// Handles music volume slider value changes
        /// </summary>
        /// <param name="value">New volume value</param>
        private void OnMusicVolumeChanged(float value)
        {
            SoundManager.Instance.SetVolume(AudioChannelType.Music, value);
            UpdateToggleWithoutNotify(AudioChannelType.Music, value < 0.01f);
        }

        /// <summary>
        /// Handles sound effects volume slider value changes
        /// </summary>
        /// <param name="value">New volume value</param>
        private void OnVfxVolumeChanged(float value)
        {
            SoundManager.Instance.SetVolume(AudioChannelType.Vfx, value);
            UpdateToggleWithoutNotify(AudioChannelType.Vfx, value < 0.01f);
        }

        /// <summary>
        /// Handles UI sounds volume slider value changes
        /// </summary>
        /// <param name="value">New volume value</param>
        private void OnUIVolumeChanged(float value)
        {
            SoundManager.Instance.SetVolume(AudioChannelType.UI, value);
            UpdateToggleWithoutNotify(AudioChannelType.UI, value < 0.01f);
        }
    }
}
