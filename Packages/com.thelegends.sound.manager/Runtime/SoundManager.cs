using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Main SoundManager for handling all audio playback and management.
    /// Uses Addressables for audio loading and AudioMixer for audio routing.
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        
        /// <summary>
        /// The singleton instance of SoundManager
        /// </summary>
        public static SoundManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("SoundManager");
                    _instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private SoundManagerSettings _settings;
        
        private AudioMixerController _mixerController;
        private AudioAddressableLoader _addressableLoader;
        private Dictionary<AudioChannelType, AudioPlayerPool> _playerPools;
        private Dictionary<string, string> _customSoundIds = new Dictionary<string, string>();
        private int _soundIdCounter = 0;
        private bool _isInitialized = false;
        private string _currentMusicId = null;
        
        /// <summary>
        /// Event raised when a sound starts playing
        /// </summary>
        public event Action<string, AudioChannelType> OnSoundStarted;
        
        /// <summary>
        /// Event raised when a sound stops playing
        /// </summary>
        public event Action<string, AudioChannelType> OnSoundStopped;
        
        /// <summary>
        /// Settings used by the SoundManager
        /// </summary>
        public SoundManagerSettings Settings => _settings;
        
        /// <summary>
        /// Whether the SoundManager is initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (_audioMixer != null && _settings != null)
            {
                Initialize(_audioMixer, _settings);
            }
        }

        /// <summary>
        /// Initializes the SoundManager with an AudioMixer and settings
        /// </summary>
        /// <param name="audioMixer">Audio mixer to use</param>
        /// <param name="settings">Settings to use</param>
        public void Initialize(AudioMixer audioMixer, SoundManagerSettings settings)
        {
            if (_isInitialized)
            {
                Log("SoundManager is already initialized.");
                return;
            }
            
            _audioMixer = audioMixer;
            _settings = settings;
            
            _mixerController = new AudioMixerController(_audioMixer);
            _addressableLoader = new AudioAddressableLoader(settings.SoundAddresses);
            _playerPools = new Dictionary<AudioChannelType, AudioPlayerPool>();
            
            // Create a GameObject to contain all pool objects
            Transform poolsContainer = new GameObject("AudioPools").transform;
            poolsContainer.SetParent(transform);
            
            // Create pools for each channel type
            foreach (AudioChannelType channelType in Enum.GetValues(typeof(AudioChannelType)))
            {
                // Skip Master channel as it shouldn't play sounds directly
                if (channelType == AudioChannelType.Master)
                    continue;
                    
                AudioMixerGroup mixerGroup = _mixerController.GetGroupByChannel(channelType);
                
                if (mixerGroup != null)
                {
                    // Create a child GameObject for each pool
                    Transform poolParent = new GameObject($"Pool_{channelType}").transform;
                    poolParent.SetParent(poolsContainer);
                    
                    _playerPools[channelType] = new AudioPlayerPool(
                        settings.InitialPoolSize, 
                        poolParent, 
                        mixerGroup, 
                        channelType);
                }
            }
            
            
            
            // Preload addressables if configured to do so
            if (settings.PreloadAddressables && settings.SoundAddresses != null)
            {
                foreach (var address in settings.SoundAddresses)
                {
                    _addressableLoader.PreloadClip(address.Key);
                }
            }
            
            // Mark as initialized after everything is set up
            _isInitialized = true;
            
            Log("SoundManager initialized successfully.");
        }

        void Start()
        {
            // Check for volume values in PlayerPrefs first, otherwise use defaults
            LoadVolumesFromPlayerPrefs();
        }

        /// <summary>
        /// Loads volume values from PlayerPrefs, or uses defaults if not found
        /// </summary>
        private void LoadVolumesFromPlayerPrefs()
        {
            // Kiểm tra volume từ PlayerPrefs, nếu không có thì dùng giá trị mặc định
            float masterVolume = PlayerPrefs.GetFloat("SoundManager_MasterVolume", _settings.DefaultMasterVolume);
            float musicVolume = PlayerPrefs.GetFloat("SoundManager_MusicVolume", _settings.DefaultMusicVolume);
            float vfxVolume = PlayerPrefs.GetFloat("SoundManager_VfxVolume", _settings.DefaultVfxVolume);
            float uiVolume = PlayerPrefs.GetFloat("SoundManager_UIVolume", _settings.DefaultUIVolume);
            
            // Thiết lập volume cho các kênh
            _mixerController.SetVolume(AudioChannelType.Master, masterVolume);
            _mixerController.SetVolume(AudioChannelType.Music, musicVolume);
            _mixerController.SetVolume(AudioChannelType.Vfx, vfxVolume);
            _mixerController.SetVolume(AudioChannelType.UI, uiVolume);
            
            Log($"Loaded volumes from PlayerPrefs - Master: {masterVolume}, Music: {musicVolume}, VFX: {vfxVolume}, UI: {uiVolume}");
        }

        /// <summary>
        /// Plays a sound using its addressable key
        /// </summary>
        /// <param name="soundKey">The key of the sound to play</param>
        /// <param name="volume">Volume scale (0-1)</param>
        /// <param name="loop">Whether to loop the sound</param>
        /// <param name="fadeInDuration">Optional fade in duration (seconds)</param>
        /// <param name="onComplete">Callback when sound completes (not called for looping)</param>
        /// <returns>The sound instance ID for controlling it later</returns>
        public string Play(string soundKey, float volume = 1f, bool loop = false, float fadeInDuration = 0f, Action onComplete = null)
        {
            EnsureInitialized();
            
            AudioChannelType channelType = _addressableLoader.GetChannelTypeForSound(soundKey);
            float volumeScale = _addressableLoader.GetVolumeScaleForSound(soundKey);
            
            // Apply volume scale from sound settings
            volume *= volumeScale;
            
            // For music, we might want to stop previous music
            if (channelType == AudioChannelType.Music)
            {
                // Stop previous music if playing
                if (!string.IsNullOrEmpty(_currentMusicId))
                {
                    if (fadeInDuration > 0)
                    {
                        // Fade out the current music before playing new one
                        FadeOut(_currentMusicId, fadeInDuration);
                    }
                    else
                    {
                        Stop(_currentMusicId);
                    }
                }
                
                // Music always uses a consistent ID
                _currentMusicId = "music";
                
                // Map the consistent ID to a unique one
                string uniqueId = GenerateSoundId();
                _customSoundIds[_currentMusicId] = uniqueId;
                
                PlaySoundWithCallback(soundKey, channelType, uniqueId, volume, loop, fadeInDuration, onComplete);
                return _currentMusicId;
            }
            
            // For non-music, we generate a unique ID
            string soundId = GenerateSoundId();
            PlaySoundWithCallback(soundKey, channelType, soundId, volume, loop, fadeInDuration, onComplete);
            
            return soundId;
        }
        
        /// <summary>
        /// Helper method to play a sound with the addressable loader callback
        /// </summary>
        private void PlaySoundWithCallback(string soundKey, AudioChannelType channelType, string soundId, float volume, bool loop, 
                                          float fadeInDuration, Action onComplete)
        {
            // Check if we've hit the max concurrent sounds limit
            if (_settings.MaxConcurrentSounds > 0)
            {
                int totalActive = 0;
                foreach (var pool in _playerPools.Values)
                {
                    totalActive += pool.ActiveCount;
                }
                
                if (totalActive >= _settings.MaxConcurrentSounds && channelType != AudioChannelType.Music)
                {
                    Log($"Max concurrent sounds reached ({_settings.MaxConcurrentSounds}). Skipping sound: {soundKey}");
                    return;
                }
            }
            
            _addressableLoader.LoadClip(soundKey, (clip) => 
            {
                if (clip == null)
                {
                    Debug.LogError($"Failed to load audio clip for key: {soundKey}");
                    return;
                }
                
                if (_playerPools.TryGetValue(channelType, out AudioPlayerPool pool))
                {
                    AudioPlayer player = pool.Get(soundId);
                    
                    // If fade-in is requested, start at zero volume
                    float startVolume = fadeInDuration > 0 ? 0 : volume;
                    
                    player.Play(clip, startVolume, loop, () => 
                    {
                        // On complete callback
                        onComplete?.Invoke();
                        
                        // Only release non-looping sounds
                        if (!loop)
                        {
                            pool.Release(soundId);
                            OnSoundStopped?.Invoke(soundId, channelType);
                            
                            // Clean up custom ID mapping if it exists
                            foreach (var kvp in new Dictionary<string, string>(_customSoundIds))
                            {
                                if (kvp.Value == soundId)
                                {
                                    _customSoundIds.Remove(kvp.Key);
                                }
                            }
                        }
                    });
                    
                    // Apply fade-in if requested
                    if (fadeInDuration > 0)
                    {
                        player.FadeIn(fadeInDuration, volume);
                    }
                    
                    OnSoundStarted?.Invoke(soundId, channelType);
                    Log($"Playing sound: {soundKey} (ID: {soundId}, Channel: {channelType})");
                }
                else
                {
                    Debug.LogError($"No audio player pool found for channel: {channelType}");
                }
            }, (error) => 
            {
                Debug.LogError($"Failed to load sound '{soundKey}': {error.Message}");
            });
        }

        /// <summary>
        /// Plays a one-shot sound without tracking its instance
        /// </summary>
        /// <param name="soundKey">The key of the sound to play</param>
        /// <param name="volume">Volume scale (0-1)</param>
        public void PlayOneShot(string soundKey, float volume = 1f)
        {
            EnsureInitialized();
            
            AudioChannelType channelType = _addressableLoader.GetChannelTypeForSound(soundKey);
            float volumeScale = _addressableLoader.GetVolumeScaleForSound(soundKey);
            
            // Apply volume scale from sound settings
            volume *= volumeScale;
            
            _addressableLoader.LoadClip(soundKey, (clip) => 
            {
                if (clip == null)
                {
                    Debug.LogError($"Failed to load audio clip for key: {soundKey}");
                    return;
                }
                
                if (_playerPools.TryGetValue(channelType, out AudioPlayerPool pool))
                {
                    string soundId = GenerateSoundId();
                    AudioPlayer player = pool.Get(soundId);
                    player.PlayOneShot(clip, volume);
                    
                    OnSoundStarted?.Invoke(soundId, channelType);
                    
                    // We need to wait for the clip to finish playing before returning to pool
                    StartCoroutine(ReleaseAfterDelay(pool, player, clip.length));
                }
            });
        }

        /// <summary>
        /// Coroutine to release a player after a delay
        /// </summary>
        private IEnumerator ReleaseAfterDelay(AudioPlayerPool pool, AudioPlayer player, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (player != null)
            {
                OnSoundStopped?.Invoke(player.SoundId, pool.ChannelType);
                pool.Release(player);
            }
        }

        /// <summary>
        /// Pauses playback of a sound
        /// </summary>
        /// <param name="soundId">ID of the sound to pause</param>
        public void Pause(string soundId)
        {
            soundId = ResolveCustomSoundId(soundId);
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(soundId, out AudioPlayer player))
                {
                    player.Pause();
                    Log($"Paused sound with ID: {soundId}");
                    return;
                }
            }
        }

        /// <summary>
        /// Resumes playback of a paused sound
        /// </summary>
        /// <param name="soundId">ID of the sound to resume</param>
        public void Resume(string soundId)
        {
            soundId = ResolveCustomSoundId(soundId);
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(soundId, out AudioPlayer player))
                {
                    player.Resume();
                    Log($"Resumed sound with ID: {soundId}");
                    return;
                }
            }
        }

        /// <summary>
        /// Stops playback of a sound
        /// </summary>
        /// <param name="soundId">ID of the sound to stop</param>
        public void Stop(string soundId)
        {
            string resolvedId = ResolveCustomSoundId(soundId);
            
            // Check if this is the current music track
            if (soundId == _currentMusicId)
            {
                _currentMusicId = null;
            }
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(resolvedId, out AudioPlayer player))
                {
                    AudioChannelType channelType = pool.ChannelType;
                    pool.Release(resolvedId);
                    OnSoundStopped?.Invoke(soundId, channelType);
                    
                    // Clean up custom ID mapping if it exists
                    foreach (var kvp in new Dictionary<string, string>(_customSoundIds))
                    {
                        if (kvp.Value == resolvedId)
                        {
                            _customSoundIds.Remove(kvp.Key);
                        }
                    }
                    
                    Log($"Stopped sound with ID: {soundId}");
                    return;
                }
            }
        }
        
        /// <summary>
        /// Fades in the volume of a sound
        /// </summary>
        /// <param name="soundId">ID of the sound to fade</param>
        /// <param name="duration">Duration of fade in seconds</param>
        /// <param name="targetVolume">Target volume after fade</param>
        public void FadeIn(string soundId, float duration, float targetVolume = 1f)
        {
            soundId = ResolveCustomSoundId(soundId);
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(soundId, out AudioPlayer player))
                {
                    player.FadeIn(duration, targetVolume);
                    Log($"Fading in sound with ID: {soundId}, Duration: {duration}s, Target Volume: {targetVolume}");
                    return;
                }
            }
        }

        /// <summary>
        /// Fades out the volume of a sound
        /// </summary>
        /// <param name="soundId">ID of the sound to fade</param>
        /// <param name="duration">Duration of fade in seconds</param>
        /// <param name="stopAfterFade">Whether to stop the sound after fading</param>
        public void FadeOut(string soundId, float duration, bool stopAfterFade = true)
        {
            soundId = ResolveCustomSoundId(soundId);
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(soundId, out AudioPlayer player))
                {
                    AudioChannelType channelType = pool.ChannelType;
                    
                    player.FadeOut(duration, stopAfterFade, () => {
                        if (stopAfterFade)
                        {
                            // If this is the current music and we're stopping it
                            if (ResolveCustomSoundId(_currentMusicId) == soundId)
                            {
                                _currentMusicId = null;
                            }
                            
                            pool.Release(soundId);
                            OnSoundStopped?.Invoke(soundId, channelType);
                            
                            // Clean up custom ID mapping if it exists
                            foreach (var kvp in new Dictionary<string, string>(_customSoundIds))
                            {
                                if (kvp.Value == soundId)
                                {
                                    _customSoundIds.Remove(kvp.Key);
                                }
                            }
                        }
                    });
                    
                    Log($"Fading out sound with ID: {soundId}, Duration: {duration}s, Stop After: {stopAfterFade}");
                    return;
                }
            }
        }
        
        /// <summary>
        /// Sets the volume for an audio channel
        /// </summary>
        /// <param name="channelType">The channel to adjust</param>
        /// <param name="volume">Volume from 0 to 1</param>
        public void SetVolume(AudioChannelType channelType, float volume)
        {
            EnsureInitialized();
            _mixerController.SetVolume(channelType, volume);
            Log($"Set {channelType} volume to {volume}");
        }

        /// <summary>
        /// Gets the current volume for an audio channel
        /// </summary>
        /// <param name="channelType">The channel to query</param>
        /// <returns>Volume from 0 to 1</returns>
        public float GetVolume(AudioChannelType channelType)
        {
            EnsureInitialized();
            return _mixerController.GetVolume(channelType);
        }
        
        /// <summary>
        /// Toggles mute state for a channel
        /// </summary>
        /// <param name="channelType">Channel to toggle</param>
        /// <returns>True if now muted, false if unmuted</returns>
        public bool ToggleMute(AudioChannelType channelType)
        {
            EnsureInitialized();
            bool isMuted = _mixerController.ToggleMute(channelType);
            Log($"{channelType} {(isMuted ? "muted" : "unmuted")}");
            return isMuted;
        }

        /// <summary>
        /// Sets the volume of a specific sound instance
        /// </summary>
        /// <param name="soundId">ID of the sound</param>
        /// <param name="volume">Volume from 0 to 1</param>
        public void SetSoundVolume(string soundId, float volume)
        {
            soundId = ResolveCustomSoundId(soundId);
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(soundId, out AudioPlayer player))
                {
                    player.Volume = volume;
                    return;
                }
            }
        }

        /// <summary>
        /// Stops all sounds of a specific channel
        /// </summary>
        /// <param name="channelType">Channel type to stop</param>
        public void StopChannel(AudioChannelType channelType)
        {
            if (!_playerPools.TryGetValue(channelType, out AudioPlayerPool pool))
                return;
                
            if (channelType == AudioChannelType.Music)
            {
                _currentMusicId = null;
            }
                
            pool.ReleaseAll();
            Log($"Stopped all sounds in channel: {channelType}");
        }

        /// <summary>
        /// Stops all sounds across all channels
        /// </summary>
        public void StopAll()
        {
            foreach (var pool in _playerPools.Values)
            {
                pool.ReleaseAll();
            }
            
            _currentMusicId = null;
            _customSoundIds.Clear();
            Log("Stopped all sounds");
        }
        
        /// <summary>
        /// Checks if a sound is currently playing
        /// </summary>
        /// <param name="soundId">ID of the sound to check</param>
        /// <returns>True if the sound is playing</returns>
        public bool IsPlaying(string soundId)
        {
            soundId = ResolveCustomSoundId(soundId);
            
            foreach (var pool in _playerPools.Values)
            {
                if (pool.TryGetPlayer(soundId, out AudioPlayer player))
                {
                    return player.IsPlaying;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Resolves a custom sound ID to a real sound ID
        /// </summary>
        /// <param name="soundId">The ID to resolve</param>
        /// <returns>The resolved ID</returns>
        private string ResolveCustomSoundId(string soundId)
        {
            if (string.IsNullOrEmpty(soundId)) return string.Empty;
            
            if (_customSoundIds.TryGetValue(soundId, out string resolvedId))
            {
                return resolvedId;
            }
            
            return soundId;
        }
        
        /// <summary>
        /// Generates a unique ID for a sound instance
        /// </summary>
        private string GenerateSoundId()
        {
            return $"sound_{_soundIdCounter++}_{DateTime.Now.Ticks}";
        }
        
        /// <summary>
        /// Ensures the SoundManager is initialized before use
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Debug.LogError("SoundManager is not initialized. Call Initialize() first.");
                throw new InvalidOperationException("SoundManager is not initialized");
            }
        }
        
        /// <summary>
        /// Logs a debug message if debug logging is enabled
        /// </summary>
        private void Log(string message)
        {
            if (_settings != null && _settings.DebugLogging)
            {
                Debug.Log($"[SoundManager] {message}");
            }
        }
        
        /// <summary>
        /// Clean up resources when destroyed
        /// </summary>
        private void OnDestroy()
        {
            if (_addressableLoader != null)
            {
                _addressableLoader.ReleaseAllClips();
            }
            
            if (_playerPools != null)
            {
                foreach (var pool in _playerPools.Values)
                {
                    pool.ReleaseAll();
                }
            }
        }
        
        /// <summary>
        /// Check for duplicating singleton instances
        /// </summary>
        private void OnApplicationQuit()
        {
            _instance = null;
        }
    }
}