using UnityEngine;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Constants used across the audio system
    /// </summary>
    public static class AudioConstants
    {
        // PlayerPrefs keys for volume settings
        public const string PREFS_MASTER_VOLUME = "SoundManager_MasterVolume";
        public const string PREFS_MUSIC_VOLUME = "SoundManager_MusicVolume";
        public const string PREFS_VFX_VOLUME = "SoundManager_VfxVolume";
        public const string PREFS_UI_VOLUME = "SoundManager_UIVolume";
        
        // AudioMixer parameter names
        public const string MIXER_MASTER_VOLUME = "MasterVolume";
        public const string MIXER_MUSIC_VOLUME = "MusicVolume";
        public const string MIXER_VFX_VOLUME = "VfxVolume";
        public const string MIXER_UI_VOLUME = "UIVolume";
        
        // AudioMixer group names
        public const string GROUP_MASTER = "Master";
        public const string GROUP_MUSIC = "Music";
        public const string GROUP_VFX = "Vfx";
        public const string GROUP_UI = "UI";
        
        /// <summary>
        /// Gets the PlayerPrefs key for a specific channel
        /// </summary>
        public static string GetPrefsKey(AudioChannelType channelType)
        {
            switch (channelType)
            {
                case AudioChannelType.Master: return PREFS_MASTER_VOLUME;
                case AudioChannelType.Music: return PREFS_MUSIC_VOLUME;
                case AudioChannelType.Vfx: return PREFS_VFX_VOLUME;
                case AudioChannelType.UI: return PREFS_UI_VOLUME;
                default: return PREFS_MASTER_VOLUME;
            }
        }
        
        /// <summary>
        /// Gets the mixer parameter name for a specific channel
        /// </summary>
        public static string GetMixerParam(AudioChannelType channelType)
        {
            switch (channelType)
            {
                case AudioChannelType.Master: return MIXER_MASTER_VOLUME;
                case AudioChannelType.Music: return MIXER_MUSIC_VOLUME;
                case AudioChannelType.Vfx: return MIXER_VFX_VOLUME; 
                case AudioChannelType.UI: return MIXER_UI_VOLUME;
                default: return MIXER_MASTER_VOLUME;
            }
        }
        
        /// <summary>
        /// Gets the mixer group name for a specific channel
        /// </summary>
        public static string GetGroupName(AudioChannelType channelType)
        {
            switch (channelType)
            {
                case AudioChannelType.Master: return GROUP_MASTER;
                case AudioChannelType.Music: return GROUP_MUSIC;
                case AudioChannelType.Vfx: return GROUP_VFX;
                case AudioChannelType.UI: return GROUP_UI;
                default: return GROUP_MASTER;
            }
        }
    }
}