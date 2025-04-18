using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Handles interactions with the AudioMixer, including volume control and group management
    /// </summary>
    public class AudioMixerController
    {
        private const string MasterVolumeParam = "MasterVolume";
        private const string MusicVolumeParam = "MusicVolume";
        private const string VfxVolumeParam = "VfxVolume";
        private const string UIVolumeParam = "UIVolume";
        
        private const string MasterGroupName = "Master";
        private const string MusicGroupName = "Music";
        private const string VfxGroupName = "Vfx";
        private const string UIGroupName = "UI";
        
        private readonly AudioMixer _audioMixer;
        private readonly Dictionary<AudioChannelType, AudioMixerGroup> _mixerGroups;
        private readonly Dictionary<AudioChannelType, float> _volumeCache = new Dictionary<AudioChannelType, float>();
        
        /// <summary>
        /// Creates a new AudioMixerController
        /// </summary>
        /// <param name="audioMixer">The AudioMixer to control</param>
        public AudioMixerController(AudioMixer audioMixer)
        {
            _audioMixer = audioMixer;
            _mixerGroups = new Dictionary<AudioChannelType, AudioMixerGroup>();
            
            // Initialize the mixer groups dictionary
            InitializeMixerGroups();
            
            // Initialize volume cache with default values
            foreach (AudioChannelType channelType in Enum.GetValues(typeof(AudioChannelType)))
            {
                _volumeCache[channelType] = GetVolumeFromMixer(channelType);
            }
        }
        
        /// <summary>
        /// Populates the mixer groups dictionary
        /// </summary>
        private void InitializeMixerGroups()
        {
            AudioMixerGroup[] allGroups = _audioMixer.FindMatchingGroups(string.Empty);
            
            foreach (AudioMixerGroup group in allGroups)
            {
                switch (group.name)
                {
                    case MasterGroupName:
                        _mixerGroups[AudioChannelType.Master] = group;
                        break;
                    case MusicGroupName:
                        _mixerGroups[AudioChannelType.Music] = group;
                        break;
                    case VfxGroupName:
                        _mixerGroups[AudioChannelType.Vfx] = group;
                        break;
                    case UIGroupName:
                        _mixerGroups[AudioChannelType.UI] = group;
                        break;
                }
            }
        }
        
        /// <summary>
        /// Gets the AudioMixerGroup for a specific channel type
        /// </summary>
        /// <param name="channelType">The channel type to get</param>
        /// <returns>The AudioMixerGroup if found, null otherwise</returns>
        public AudioMixerGroup GetGroupByChannel(AudioChannelType channelType)
        {
            if (_mixerGroups.TryGetValue(channelType, out AudioMixerGroup group))
            {
                return group;
            }
            
            Debug.LogWarning($"AudioMixerGroup not found for channel {channelType}. Using Master group instead.");
            return _mixerGroups.TryGetValue(AudioChannelType.Master, out AudioMixerGroup masterGroup) ? 
                masterGroup : null;
        }
        
        /// <summary>
        /// Sets the volume for a specific channel
        /// </summary>
        /// <param name="channelType">The channel to set volume for</param>
        /// <param name="volume">Volume from 0 (mute) to 1 (full volume)</param>
        public void SetVolume(AudioChannelType channelType, float volume)
        {
            // Clamp volume between 0 and 1
            volume = Mathf.Clamp01(volume);
            
            // Cache the linear volume
            _volumeCache[channelType] = volume;
            
            // Convert linear volume (0-1) to logarithmic (-80db to 0db)
            // Avoid -infinity when volume is 0
            float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            
            string paramName = GetVolumeParamName(channelType);
            _audioMixer.SetFloat(paramName, dbVolume);
            
            // Save to player prefs for persistence
            PlayerPrefs.SetFloat($"SoundManager_{paramName}", volume);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Gets the current volume for a specific channel
        /// </summary>
        /// <param name="channelType">The channel to get volume for</param>
        /// <returns>Volume from 0 (mute) to 1 (full volume)</returns>
        public float GetVolume(AudioChannelType channelType)
        {
            if (_volumeCache.TryGetValue(channelType, out float cachedVolume))
            {
                return cachedVolume;
            }
            
            // If not cached, get directly from mixer
            return GetVolumeFromMixer(channelType);
        }
        
        /// <summary>
        /// Gets the current volume from the mixer directly
        /// </summary>
        /// <param name="channelType">The channel to get volume for</param>
        /// <returns>Volume from 0 (mute) to 1 (full volume)</returns>
        private float GetVolumeFromMixer(AudioChannelType channelType)
        {
            string paramName = GetVolumeParamName(channelType);
            
            // Try to get from PlayerPrefs first
            if (PlayerPrefs.HasKey($"SoundManager_{paramName}"))
            {
                float volume = PlayerPrefs.GetFloat($"SoundManager_{paramName}", 1f);
                
                // Also make sure the mixer has this value
                float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
                _audioMixer.SetFloat(paramName, dbVolume);
                
                return volume;
            }
            
            // Fall back to querying the mixer directly
            if (_audioMixer.GetFloat(paramName, out float dbValue))
            {
                // Convert from dB to linear
                return dbValue <= -79.9f ? 0f : Mathf.Pow(10, dbValue / 20);
            }
            
            // Default
            return 1f;
        }
        
        /// <summary>
        /// Toggles mute state for a channel
        /// </summary>
        /// <param name="channelType">Channel to toggle</param>
        /// <returns>True if now muted, false if unmuted</returns>
        public bool ToggleMute(AudioChannelType channelType)
        {
            float currentVolume = GetVolume(channelType);
            
            // If already muted (or very low), restore to 1.0
            if (currentVolume < 0.01f)
            {
                SetVolume(channelType, 1f);
                return false;
            }
            // Otherwise mute
            else
            {
                SetVolume(channelType, 0f);
                return true;
            }
        }
        
        /// <summary>
        /// Gets the parameter name used in the AudioMixer for volume control
        /// </summary>
        private string GetVolumeParamName(AudioChannelType channelType)
        {
            switch (channelType)
            {
                case AudioChannelType.Master:
                    return MasterVolumeParam;
                case AudioChannelType.Music:
                    return MusicVolumeParam;
                case AudioChannelType.Vfx:
                    return VfxVolumeParam;
                case AudioChannelType.UI:
                    return UIVolumeParam;
                default:
                    return MasterVolumeParam;
            }
        }
    }
}