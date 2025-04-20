using UnityEngine;
using UnityEngine.UI;
using com.thelegends.sound.manager;
using System.Collections;

public class Demo : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider vfxVolumeSlider;
    [SerializeField] private Slider uiVolumeSlider;
    [SerializeField] private Toggle masterToggle;
    [SerializeField] private Toggle musicToggle;
    [SerializeField] private Toggle sfxToggle;
    [SerializeField] private Toggle uiToggle;
    
    private void Start()
    {
       StartCoroutine(A());
    }

    private IEnumerator A() {
        yield return new WaitForEndOfFrame();
        // Khởi tạo giá trị slider từ SoundManager
        masterVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Master);
        musicVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Music);
        vfxVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Vfx);
        uiVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.UI);
        
        // Thêm listeners cho slider
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        vfxVolumeSlider.onValueChanged.AddListener(OnVfxVolumeChanged);
        uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);

        // Khởi tạo trạng thái toggle dựa trên âm lượng hiện tại
        // ĐẢO NGƯỢC LOGIC: ON = muted (âm lượng gần 0), OFF = unmuted (có âm lượng)
        masterToggle.isOn = masterVolumeSlider.value < 0.01f;
        musicToggle.isOn = musicVolumeSlider.value < 0.01f;
        sfxToggle.isOn = vfxVolumeSlider.value < 0.01f;
        uiToggle.isOn = uiVolumeSlider.value < 0.01f;

        // Thêm listeners cho toggle sau khi đã khởi tạo giá trị
        masterToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.Master, isOn));
        musicToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.Music, isOn));
        sfxToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.Vfx, isOn));
        uiToggle.onValueChanged.AddListener(isOn => HandleToggleChange(AudioChannelType.UI, isOn));
    }

    private void HandleToggleChange(AudioChannelType channelType, bool isOn)
    {
        if (isOn)
        {
            // Toggle is ON = MUTE (tắt âm)
            // If not already muted, use ToggleMute to store current volume before muting
            if (SoundManager.Instance.GetVolume(channelType) >= 0.01f)
            {
                SoundManager.Instance.ToggleMute(channelType);
            }
            // If already muted, do nothing
        }
        else
        {
            // Toggle is OFF = UNMUTE (bật âm)
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
    
    private void OnMasterVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(AudioChannelType.Master, value);
    }
    
    private void OnMusicVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(AudioChannelType.Music, value);
    }
    
    private void OnVfxVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(AudioChannelType.Vfx, value);
    }
    
    private void OnUIVolumeChanged(float value)
    {
        SoundManager.Instance.SetVolume(AudioChannelType.UI, value);
    }

    public void PlayMusic() {
        SoundManager.Instance.Play(SoundKeys.MUSIC_GAMEPLAYMUSIC, 1, true, 3);
    }

    public void PlayMainMenuMusic() {
        SoundManager.Instance.Play(SoundKeys.MUSIC_MAINMENU, 1, true, 3);
    }

    public void PlaySfx1() {
        SoundManager.Instance.PlayOneShot(SoundKeys.VFX_PUNCHSFX);
    }

    public void PlaySfx2() {
        SoundManager.Instance.PlayOneShot(SoundKeys.UI_BUTTON_CLICK);
    }

    public void PauseMUsic() {
        SoundManager.Instance.Pause(SoundKeys.MUSIC);  // Sử dụng hằng số MUSIC
    }
    public void ResumeMusic() {
        SoundManager.Instance.Resume(SoundKeys.MUSIC); // Sử dụng hằng số MUSIC
    }
    public void StopMusic() {
        SoundManager.Instance.Stop(SoundKeys.MUSIC);   // Sử dụng hằng số MUSIC
    }
}