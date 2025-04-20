using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Handles loading audio clips through the Addressables system
    /// </summary>
    public class AudioAddressableLoader
    {
        private readonly Dictionary<string, AudioClip> _loadedClips = new Dictionary<string, AudioClip>();
        private readonly Dictionary<string, AsyncOperationHandle<AudioClip>> _loadingOperations = 
            new Dictionary<string, AsyncOperationHandle<AudioClip>>();
        private readonly Dictionary<string, SoundAddress> _soundAddressesByKey = 
            new Dictionary<string, SoundAddress>();
        
        /// <summary>
        /// Number of clips currently loaded in memory
        /// </summary>
        public int LoadedClipCount => _loadedClips.Count;
        
        /// <summary>
        /// Number of clips currently being loaded
        /// </summary>
        public int LoadingCount => _loadingOperations.Count;
        
        /// <summary>
        /// Creates a new AudioAddressableLoader
        /// </summary>
        /// <param name="soundAddresses">List of sound addresses to manage</param>
        public AudioAddressableLoader(List<SoundAddress> soundAddresses)
        {
            RegisterSoundAddresses(soundAddresses);
        }
        
        /// <summary>
        /// Registers sound addresses for lookup
        /// </summary>
        /// <param name="soundAddresses">List of sound addresses to register</param>
        public void RegisterSoundAddresses(List<SoundAddress> soundAddresses)
        {
            if (soundAddresses == null) return;
            
            foreach (var soundAddress in soundAddresses)
            {
                if (!string.IsNullOrEmpty(soundAddress.Key))
                {
                    _soundAddressesByKey[soundAddress.Key] = soundAddress;
                }
            }
        }

        /// <summary>
        /// Loads an audio clip by key
        /// </summary>
        /// <param name="key">The key of the sound to load</param>
        /// <param name="onLoaded">Callback when loading completes successfully</param>
        /// <param name="onFailed">Callback when loading fails</param>
        public void LoadClip(string key, Action<AudioClip> onLoaded, Action<Exception> onFailed = null)
        {
            // If already loaded, return the clip immediately
            if (_loadedClips.TryGetValue(key, out AudioClip clip))
            {
                onLoaded?.Invoke(clip);
                return;
            }

            // If already loading, wait for it to complete
            if (_loadingOperations.TryGetValue(key, out AsyncOperationHandle<AudioClip> loadingOp))
            {
                if (loadingOp.IsDone)
                {
                    if (loadingOp.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedClips[key] = loadingOp.Result;
                        onLoaded?.Invoke(loadingOp.Result);
                    }
                    else
                    {
                        onFailed?.Invoke(loadingOp.OperationException);
                    }
                }
                else
                {
                    loadingOp.Completed += (op) => {
                        if (op.Status == AsyncOperationStatus.Succeeded)
                        {
                            _loadedClips[key] = op.Result;
                            onLoaded?.Invoke(op.Result);
                        }
                        else
                        {
                            onFailed?.Invoke(op.OperationException);
                        }
                    };
                }
                return;
            }

            // Find the address for the key
            if (!_soundAddressesByKey.TryGetValue(key, out SoundAddress soundAddress))
            {
                onFailed?.Invoke(new KeyNotFoundException($"Sound address with key '{key}' not found."));
                return;
            }

            // Start loading
            try
            {
                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(soundAddress.Address);
                _loadingOperations[key] = handle;
                
                handle.Completed += (op) => {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedClips[key] = op.Result;
                        onLoaded?.Invoke(op.Result);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load audio clip for key: {key}, address: {soundAddress.Address}. Error: {op.OperationException?.Message}");
                        onFailed?.Invoke(op.OperationException);
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception when loading audio clip: {key}. Error: {ex.Message}");
                onFailed?.Invoke(ex);
            }
        }

        /// <summary>
        /// Preloads an audio clip but doesn't return it immediately
        /// </summary>
        /// <param name="key">The key of the sound to preload</param>
        public void PreloadClip(string key)
        {
            // Skip if already loaded or loading
            if (_loadedClips.ContainsKey(key) || _loadingOperations.ContainsKey(key))
                return;
                
            LoadClip(key, 
                clip => { Debug.Log($"Preloaded audio: {key}"); }, 
                error => { Debug.LogError($"Failed to preload audio: {key}. Error: {error.Message}"); });
        }

        /// <summary>
        /// Releases a loaded audio clip
        /// </summary>
        /// <param name="key">The key of the clip to release</param>
        public void ReleaseClip(string key)
        {
            if (_loadedClips.TryGetValue(key, out AudioClip _))
            {
                _loadedClips.Remove(key);
                
                if (_loadingOperations.TryGetValue(key, out AsyncOperationHandle<AudioClip> handle))
                {
                    Addressables.Release(handle);
                    _loadingOperations.Remove(key);
                }
            }
        }

        /// <summary>
        /// Releases all loaded audio clips
        /// </summary>
        public void ReleaseAllClips()
        {
            foreach (var key in new List<string>(_loadedClips.Keys))
            {
                ReleaseClip(key);
            }
        }

        /// <summary>
        /// Gets the channel type for a specific audio key
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>Channel type for the sound, or Master if not found</returns>
        public AudioChannelType GetChannelTypeForSound(string key)
        {
            if (_soundAddressesByKey.TryGetValue(key, out SoundAddress address))
            {
                return address.ChannelType;
            }
            
            return AudioChannelType.Master;
        }
        
        /// <summary>
        /// Gets the volume scale for a specific audio key
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <returns>Volume scale for the sound, or 1.0 if not found</returns>
        public float GetVolumeScaleForSound(string key)
        {
            if (_soundAddressesByKey.TryGetValue(key, out SoundAddress address))
            {
                return address.VolumeScale;
            }
            
            return 1.0f;
        }
    }
}