using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using com.thelegends.unity.pooling;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Adapter class that connects SoundManager with PoolManager.
    /// Replaces the original AudioPlayerPool implementation.
    /// </summary>
    public class AudioPlayerPoolAdapter
    {
        private readonly AudioChannelType _channelType;
        private readonly AudioMixerGroup _mixerGroup;
        private readonly string _poolKey;
        private readonly Dictionary<string, AudioPlayer> _activePlayers = new Dictionary<string, AudioPlayer>();
        private readonly Transform _parentTransform;
        
        // Reference to prefab for creating pooled objects
        private GameObject _templatePrefab;
        
        /// <summary>
        /// Number of currently active AudioPlayers
        /// </summary>
        public int ActiveCount => _activePlayers.Count;
        
        /// <summary>
        /// Gets the channel type this pool is associated with
        /// </summary>
        public AudioChannelType ChannelType => _channelType;

        /// <summary>
        /// Creates a new AudioPlayerPoolAdapter
        /// </summary>
        /// <param name="initialSize">Initial number of AudioPlayers to create</param>
        /// <param name="parent">Parent transform for all AudioPlayers</param>
        /// <param name="mixerGroup">AudioMixerGroup for all AudioPlayers in this pool</param>
        /// <param name="channelType">Channel type this pool is for</param>
        public AudioPlayerPoolAdapter(int initialSize, Transform parent, AudioMixerGroup mixerGroup, AudioChannelType channelType)
        {
            _channelType = channelType;
            _mixerGroup = mixerGroup;
            _poolKey = $"AudioPool_{channelType}";
            _parentTransform = parent;
            
            Initialize(initialSize);
        }

        /// <summary>
        /// Initializes the pool by creating a template AudioPlayer and registering it with PoolManager
        /// </summary>
        private async void Initialize(int initialSize)
        {
            // Create a template AudioPlayer GameObject
            _templatePrefab = new GameObject($"AudioPlayer_{_channelType}_Template");
            AudioPlayer templatePlayer = _templatePrefab.AddComponent<AudioPlayer>();
            templatePlayer.Initialize(_mixerGroup, "template");
            
            // Đặt parent cho template (quan trọng để sau này các instance được tạo ra cũng có parent này)
            if (_parentTransform != null)
            {
                _templatePrefab.transform.SetParent(_parentTransform);
            }
            
            // Deactivate template
            _templatePrefab.SetActive(false);
            
            // Create pool configuration
            PoolConfig config = new PoolConfig(
                initialSize: initialSize,
                allowGrowth: true,
                maxSize: 100
            );
            
            // Đợi chút để đảm bảo template được tạo đầy đủ
            await Task.Delay(10);
            
            // Register with PoolManager using the template GameObject as the key
            await PoolManager.Instance.CreatePoolAsync(_templatePrefab, config);
            
            // Lưu ý: KHÔNG hủy template prefab, vì PoolManager sẽ sử dụng nó để tạo các instance
        }

        /// <summary>
        /// Gets an AudioPlayer from the pool
        /// </summary>
        /// <param name="soundId">Unique ID for this sound instance</param>
        /// <returns>An AudioPlayer ready for use</returns>
        public AudioPlayer Get(string soundId)
        {
            // Đảm bảo template prefab đã được khởi tạo
            if (_templatePrefab == null)
            {
                Debug.LogError($"[AudioPlayerPool] Template prefab not initialized for {_channelType}");
                return null;
            }
            
            GameObject gameObject = PoolManager.Instance.Get<GameObject>(_templatePrefab);
            
            if (gameObject == null)
            {
                Debug.LogError($"[AudioPlayerPool] Failed to get object from pool for {_channelType}");
                return null;
            }
            
            // Đảm bảo parent transform đúng
            if (_parentTransform != null && gameObject.transform.parent != _parentTransform)
            {
                gameObject.transform.SetParent(_parentTransform, false);
            }
            
            AudioPlayer player = gameObject.GetComponent<AudioPlayer>();
            
            if (player != null)
            {
                player.Initialize(_mixerGroup, soundId);
                _activePlayers[soundId] = player;
            }
            else
            {
                Debug.LogError($"[AudioPlayerPool] AudioPlayer component missing on pooled object for {_channelType}");
                PoolManager.Instance.ReturnToPool(gameObject);
                return null;
            }
            
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
            PoolManager.Instance.ReturnToPool(player.gameObject);
            
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