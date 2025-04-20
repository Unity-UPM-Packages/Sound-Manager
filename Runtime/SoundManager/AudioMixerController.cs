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
        private readonly AudioMixer _audioMixer;
        private readonly Dictionary<AudioChannelType, AudioMixerGroup> _mixerGroups;
        private readonly Dictionary<AudioChannelType, float> _volumeCache = new Dictionary<AudioChannelType, float>();
        private readonly Dictionary<AudioChannelType, float> _preMuteVolumes = new Dictionary<AudioChannelType, float>();
        
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
                
                // Restore mute states and pre-mute volumes from PlayerPrefs
                RestoreMuteState(channelType);
            }
        }
        
        /// <summary>
        /// Restores mute state and pre-mute volume from PlayerPrefs
        /// </summary>
        /// <param name="channelType">The channel to restore</param>
        private void RestoreMuteState(AudioChannelType channelType)
        {
            string muteStateKey = AudioConstants.GetMuteStateKey(channelType);
            string preMuteKey = AudioConstants.GetPreMuteVolumeKey(channelType);
            
            // Check if channel was muted
            if (PlayerPrefs.HasKey(muteStateKey) && PlayerPrefs.GetInt(muteStateKey) == 1)
            {
                // Channel was muted, check if we have saved pre-mute volume
                if (PlayerPrefs.HasKey(preMuteKey))
                {
                    float preMuteVolume = PlayerPrefs.GetFloat(preMuteKey);
                    
                    // Store in memory for later use when unmuting
                    _preMuteVolumes[channelType] = preMuteVolume;
                    
                    // Make sure the channel is actually muted (volume set to 0)
                    // We use base SetVolume to avoid recursive setting
                    float currentVol = GetVolume(channelType);
                    if (currentVol >= 0.01f)
                    {
                        SetVolume(channelType, 0f);
                    }
                }
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
                    case AudioConstants.GROUP_MASTER:
                        _mixerGroups[AudioChannelType.Master] = group;
                        break;
                    case AudioConstants.GROUP_MUSIC:
                        _mixerGroups[AudioChannelType.Music] = group;
                        break;
                    case AudioConstants.GROUP_VFX:
                        _mixerGroups[AudioChannelType.Vfx] = group;
                        break;
                    case AudioConstants.GROUP_UI:
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
            
            // Get current volume before making changes
            float currentVolume = GetVolume(channelType);
            
            // Handle special cases for mute/unmute through direct volume changes
            if (currentVolume < 0.01f && volume >= 0.01f)
            {
                // Case: Unmuting through direct volume change
                // Remove any stored pre-mute volume since we're explicitly setting a new value
                _preMuteVolumes.Remove(channelType);
                
                // Also remove from PlayerPrefs
                PlayerPrefs.DeleteKey(AudioConstants.GetPreMuteVolumeKey(channelType));
                PlayerPrefs.SetInt(AudioConstants.GetMuteStateKey(channelType), 0);
            }
            else if (currentVolume >= 0.01f && volume < 0.01f)
            {
                // Case: Muting through direct volume change
                // Store current volume before muting (if not already stored)
                if (!_preMuteVolumes.ContainsKey(channelType))
                {
                    _preMuteVolumes[channelType] = currentVolume;
                    
                    // Also store in PlayerPrefs
                    PlayerPrefs.SetFloat(AudioConstants.GetPreMuteVolumeKey(channelType), currentVolume);
                    PlayerPrefs.SetInt(AudioConstants.GetMuteStateKey(channelType), 1);
                }
            }
            else if (volume >= 0.01f)
            {
                // Case: Changing volume while unmuted
                // Update pre-mute volume if there's one stored (for consistency)
                if (_preMuteVolumes.ContainsKey(channelType))
                {
                    _preMuteVolumes[channelType] = volume;
                    
                    // Also update in PlayerPrefs
                    PlayerPrefs.SetFloat(AudioConstants.GetPreMuteVolumeKey(channelType), volume);
                }
            }
            
            // Cache the linear volume
            _volumeCache[channelType] = volume;
            
            // Convert linear volume (0-1) to logarithmic (-80db to 0db)
            // Avoid -infinity when volume is 0
            float dbVolume = volume > 0.0001f ? Mathf.Log10(volume) * 20 : -80f;
            
            string paramName = AudioConstants.GetMixerParam(channelType);
            _audioMixer.SetFloat(paramName, dbVolume);
            
            // Save to player prefs for persistence
            PlayerPrefs.SetFloat(AudioConstants.GetPrefsKey(channelType), volume);
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
            string paramName = AudioConstants.GetMixerParam(channelType);
            string prefsKey = AudioConstants.GetPrefsKey(channelType);
            
            // Try to get from PlayerPrefs first
            if (PlayerPrefs.HasKey(prefsKey))
            {
                float volume = PlayerPrefs.GetFloat(prefsKey, 1f);
                
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
            
            // If already muted (volume near zero), restore to previous volume
            if (currentVolume < 0.01f)
            {
                // Get the volume value before mute, default to 1 if not found
                float previousVolume = 1f;
                
                // First check the dictionary in memory
                if (_preMuteVolumes.TryGetValue(channelType, out float savedVolume))
                {
                    previousVolume = savedVolume;
                    // Remove the saved value after using it
                    _preMuteVolumes.Remove(channelType);
                }
                // If not in memory, check PlayerPrefs
                else
                {
                    string preMuteKey = AudioConstants.GetPreMuteVolumeKey(channelType);
                    if (PlayerPrefs.HasKey(preMuteKey))
                    {
                        previousVolume = PlayerPrefs.GetFloat(preMuteKey);
                    }
                }
                
                // Ensure volume is valid (not too small)
                previousVolume = Mathf.Max(0.01f, previousVolume);
                
                // Apply the previous volume
                SetVolume(channelType, previousVolume);
                
                // Update persistence - not muted anymore
                PlayerPrefs.SetInt(AudioConstants.GetMuteStateKey(channelType), 0);
                PlayerPrefs.DeleteKey(AudioConstants.GetPreMuteVolumeKey(channelType));
                PlayerPrefs.Save();
                
                return false; // Unmuted
            }
            // If not muted, save current volume and set to 0
            else
            {
                // Save current volume value before muting
                _preMuteVolumes[channelType] = currentVolume;
                
                // Also save to PlayerPrefs for persistence
                PlayerPrefs.SetFloat(AudioConstants.GetPreMuteVolumeKey(channelType), currentVolume);
                PlayerPrefs.SetInt(AudioConstants.GetMuteStateKey(channelType), 1);
                PlayerPrefs.Save();
                
                // Set volume to 0 to mute
                SetVolume(channelType, 0f);
                return true; // Muted
            }
        }
    }
}