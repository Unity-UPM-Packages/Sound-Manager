using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Manages a pool of AudioPlayer components for efficient reuse
    /// </summary>
    public class AudioPlayerPool
    {
        private readonly Transform _parent;
        private readonly AudioMixerGroup _mixerGroup;
        private readonly Stack<AudioPlayer> _availablePlayers = new Stack<AudioPlayer>();
        private readonly Dictionary<string, AudioPlayer> _activePlayers = new Dictionary<string, AudioPlayer>();
        private readonly int _initialSize;
        private readonly AudioChannelType _channelType;
        
        /// <summary>
        /// Number of currently active AudioPlayers
        /// </summary>
        public int ActiveCount => _activePlayers.Count;
        
        /// <summary>
        /// Number of available AudioPlayers in the pool
        /// </summary>
        public int AvailableCount => _availablePlayers.Count;
        
        /// <summary>
        /// Gets the channel type this pool is associated with
        /// </summary>
        public AudioChannelType ChannelType => _channelType;

        /// <summary>
        /// Creates a new AudioPlayer pool
        /// </summary>
        /// <param name="initialSize">Initial number of AudioPlayers to create</param>
        /// <param name="parent">Parent transform for all AudioPlayers</param>
        /// <param name="mixerGroup">AudioMixerGroup for all AudioPlayers in this pool</param>
        /// <param name="channelType">Channel type this pool is for</param>
        public AudioPlayerPool(int initialSize, Transform parent, AudioMixerGroup mixerGroup, AudioChannelType channelType)
        {
            _initialSize = initialSize;
            _parent = parent;
            _mixerGroup = mixerGroup;
            _channelType = channelType;
            
            Initialize();
        }

        /// <summary>
        /// Creates the initial AudioPlayers in the pool
        /// </summary>
        private void Initialize()
        {
            for (int i = 0; i < _initialSize; i++)
            {
                CreateAudioPlayer();
            }
        }

        /// <summary>
        /// Creates a new AudioPlayer instance
        /// </summary>
        /// <returns>The created AudioPlayer</returns>
        private AudioPlayer CreateAudioPlayer()
        {
            GameObject go = new GameObject($"AudioPlayer_{_channelType}");
            go.transform.SetParent(_parent);
            
            AudioPlayer player = go.AddComponent<AudioPlayer>();
            go.SetActive(false);
            
            _availablePlayers.Push(player);
            return player;
        }

        /// <summary>
        /// Gets an AudioPlayer from the pool
        /// </summary>
        /// <param name="soundId">Unique ID for this sound instance</param>
        /// <returns>An AudioPlayer ready for use</returns>
        public AudioPlayer Get(string soundId)
        {
            AudioPlayer player;
            
            if (_availablePlayers.Count == 0)
            {
                player = CreateAudioPlayer();
            }
            else
            {
                player = _availablePlayers.Pop();
            }
            
            player.gameObject.SetActive(true);
            player.Initialize(_mixerGroup, soundId);
            _activePlayers[soundId] = player;
            
            return player;
        }

        /// <summary>
        /// Returns an AudioPlayer to the pool
        /// </summary>
        /// <param name="soundId">ID of the sound to release</param>
        /// <returns>True if the player was released, false if not found</returns>
        public bool Release(string soundId)
        {
            if (!_activePlayers.TryGetValue(soundId, out AudioPlayer player))
            {
                return false;
            }
            
            return Release(player);
        }

        /// <summary>
        /// Returns an AudioPlayer to the pool
        /// </summary>
        /// <param name="player">The AudioPlayer to release</param>
        /// <returns>True if the player was released, false if not found</returns>
        public bool Release(AudioPlayer player)
        {
            if (player == null) return false;
            
            string soundId = player.SoundId;
            if (!_activePlayers.ContainsKey(soundId)) return false;
            
            _activePlayers.Remove(soundId);
            
            player.Stop();
            player.gameObject.SetActive(false);
            _availablePlayers.Push(player);
            
            return true;
        }

        /// <summary>
        /// Checks if a sound is currently active in this pool
        /// </summary>
        /// <param name="soundId">ID of the sound to check</param>
        /// <returns>True if the sound is active</returns>
        public bool IsActive(string soundId)
        {
            return _activePlayers.ContainsKey(soundId);
        }

        /// <summary>
        /// Gets an active AudioPlayer by sound ID
        /// </summary>
        /// <param name="soundId">ID of the sound</param>
        /// <param name="player">Output AudioPlayer if found</param>
        /// <returns>True if the player was found</returns>
        public bool TryGetPlayer(string soundId, out AudioPlayer player)
        {
            return _activePlayers.TryGetValue(soundId, out player);
        }

        /// <summary>
        /// Releases all active AudioPlayers back to the pool
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var player in new List<AudioPlayer>(_activePlayers.Values))
            {
                Release(player);
            }
            
            _activePlayers.Clear();
        }
    }
}