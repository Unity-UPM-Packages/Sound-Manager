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
        private readonly Dictionary<string, SoundAddress> _soundAddressesByAddress = 
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
                if (!string.IsNullOrEmpty(soundAddress.Address))
                {
                    _soundAddressesByAddress[soundAddress.Address] = soundAddress;
                }
            }
        }

        /// <summary>
        /// Loads an audio clip by address
        /// </summary>
        /// <param name="address">The address of the sound to load</param>
        /// <param name="onLoaded">Callback when loading completes successfully</param>
        /// <param name="onFailed">Callback when loading fails</param>
        public void LoadClip(string address, Action<AudioClip> onLoaded, Action<Exception> onFailed = null)
        {
            // If already loaded, return the clip immediately
            if (_loadedClips.TryGetValue(address, out AudioClip clip))
            {
                onLoaded?.Invoke(clip);
                return;
            }

            // If already loading, wait for it to complete
            if (_loadingOperations.TryGetValue(address, out AsyncOperationHandle<AudioClip> loadingOp))
            {
                if (loadingOp.IsDone)
                {
                    if (loadingOp.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedClips[address] = loadingOp.Result;
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
                            _loadedClips[address] = op.Result;
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
            if (!_soundAddressesByAddress.TryGetValue(address, out SoundAddress soundAddress))
            {
                onFailed?.Invoke(new KeyNotFoundException($"Sound address with address '{address}' not found."));
                return;
            }

            // Start loading
            try
            {
                AsyncOperationHandle<AudioClip> handle = Addressables.LoadAssetAsync<AudioClip>(soundAddress.Address);
                _loadingOperations[address] = handle;
                
                handle.Completed += (op) => {
                    if (op.Status == AsyncOperationStatus.Succeeded)
                    {
                        _loadedClips[address] = op.Result;
                        onLoaded?.Invoke(op.Result);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load audio clip for address: {address}, address: {soundAddress.Address}. Error: {op.OperationException?.Message}");
                        onFailed?.Invoke(op.OperationException);
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception when loading audio clip: {address}. Error: {ex.Message}");
                onFailed?.Invoke(ex);
            }
        }

        /// <summary>
        /// Preloads an audio clip but doesn't return it immediately
        /// </summary>
        /// <param name="address">The address of the sound to preload</param>
        public void PreloadClip(string address)
        {
            // Skip if already loaded or loading
            if (_loadedClips.ContainsKey(address) || _loadingOperations.ContainsKey(address))
                return;
                
            LoadClip(address, 
                clip => { Debug.Log($"Preloaded audio: {address}"); }, 
                error => { Debug.LogError($"Failed to preload audio: {address}. Error: {error.Message}"); });
        }

        /// <summary>
        /// Releases a loaded audio clip
        /// </summary>
        /// <param name="address">The address of the clip to release</param>
        public void ReleaseClip(string address)
        {
            if (_loadedClips.TryGetValue(address, out AudioClip _))
            {
                _loadedClips.Remove(address);
                
                if (_loadingOperations.TryGetValue(address, out AsyncOperationHandle<AudioClip> handle))
                {
                    Addressables.Release(handle);
                    _loadingOperations.Remove(address);
                }
            }
        }

        /// <summary>
        /// Releases all loaded audio clips
        /// </summary>
        public void ReleaseAllClips()
        {
            foreach (var address in new List<string>(_loadedClips.Keys))
            {
                ReleaseClip(address);
            }
        }

        /// <summary>
        /// Gets the channel type for a specific audio address
        /// </summary>
        /// <param name="address">The address to lookup</param>
        /// <returns>Channel type for the sound, or Master if not found</returns>
        public AudioChannelType GetChannelTypeForSound(string address)
        {
            if (_soundAddressesByAddress.TryGetValue(address, out SoundAddress soundAddress))
            {
                return soundAddress.ChannelType;
            }
            
            return AudioChannelType.Master;
        }
        
        /// <summary>
        /// Gets the volume scale for a specific audio address
        /// </summary>
        /// <param name="address">The address to lookup</param>
        /// <returns>Volume scale for the sound, or 1.0 if not found</returns>
        public float GetVolumeScaleForSound(string address)
        {
            if (_soundAddressesByAddress.TryGetValue(address, out SoundAddress soundAddress))
            {
                return soundAddress.VolumeScale;
            }
            
            return 1.0f;
        }
    }
}