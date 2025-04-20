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
        
        // PlayerPrefs keys for mute states
        public const string PREFS_MASTER_MUTED = "SoundManager_MasterMuted";
        public const string PREFS_MUSIC_MUTED = "SoundManager_MusicMuted";
        public const string PREFS_VFX_MUTED = "SoundManager_VfxMuted";
        public const string PREFS_UI_MUTED = "SoundManager_UIMuted";
        
        // PlayerPrefs keys for pre-mute volume values
        public const string PREFS_MASTER_PREMUTE = "SoundManager_MasterPreMute";
        public const string PREFS_MUSIC_PREMUTE = "SoundManager_MusicPreMute";
        public const string PREFS_VFX_PREMUTE = "SoundManager_VfxPreMute";
        public const string PREFS_UI_PREMUTE = "SoundManager_UIPreMute";
        
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
        
        /// <summary>
        /// Gets the PlayerPrefs key for mute state of a specific channel
        /// </summary>
        public static string GetMuteStateKey(AudioChannelType channelType)
        {
            switch (channelType)
            {
                case AudioChannelType.Master: return PREFS_MASTER_MUTED;
                case AudioChannelType.Music: return PREFS_MUSIC_MUTED;
                case AudioChannelType.Vfx: return PREFS_VFX_MUTED;
                case AudioChannelType.UI: return PREFS_UI_MUTED;
                default: return PREFS_MASTER_MUTED;
            }
        }
        
        /// <summary>
        /// Gets the PlayerPrefs key for pre-mute volume of a specific channel
        /// </summary>
        public static string GetPreMuteVolumeKey(AudioChannelType channelType)
        {
            switch (channelType)
            {
                case AudioChannelType.Master: return PREFS_MASTER_PREMUTE;
                case AudioChannelType.Music: return PREFS_MUSIC_PREMUTE;
                case AudioChannelType.Vfx: return PREFS_VFX_PREMUTE;
                case AudioChannelType.UI: return PREFS_UI_PREMUTE;
                default: return PREFS_MASTER_PREMUTE;
            }
        }
    }
}