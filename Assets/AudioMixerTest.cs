using UnityEngine;
using UnityEngine.UI;
using com.thelegends.sound.manager;

public class AudioMixerTest : MonoBehaviour
{
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider vfxVolumeSlider;
    [SerializeField] private Slider uiVolumeSlider;
    
    private void Start()
    {
        // Khởi tạo giá trị slider từ SoundManager
        masterVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Master);
        musicVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Music);
        vfxVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.Vfx);
        uiVolumeSlider.value = SoundManager.Instance.GetVolume(AudioChannelType.UI);
        
        // Thêm listeners
        masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        vfxVolumeSlider.onValueChanged.AddListener(OnVfxVolumeChanged);
        uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
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
}