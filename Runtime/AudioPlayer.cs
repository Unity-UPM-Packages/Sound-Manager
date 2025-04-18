using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Component responsible for playing and controlling a single audio source
    /// Provides methods for play, pause, stop, fade in/out, and other audio controls
    /// </summary>
    public class AudioPlayer : MonoBehaviour
    {
        private AudioSource _audioSource;
        private Coroutine _fadeCoroutine;
        private Coroutine _completionCoroutine;
        private bool _isPaused;
        private float _pausedTime;
        private float _originalVolume;
        private Action _onComplete;
        private string _soundId;
        
        /// <summary>
        /// The current AudioClip being played
        /// </summary>
        public AudioClip CurrentClip => _audioSource?.clip;
        
        /// <summary>
        /// Is audio currently playing (not stopped or paused)
        /// </summary>
        public bool IsPlaying => _audioSource != null && _audioSource.isPlaying;
        
        /// <summary>
        /// Is audio currently paused
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>
        /// Is the audio set to loop
        /// </summary>
        public bool IsLooping => _audioSource != null && _audioSource.loop;
        
        /// <summary>
        /// Current volume (0-1)
        /// </summary>
        public float Volume
        {
            get => _audioSource != null ? _audioSource.volume : 0;
            set { if (_audioSource != null) _audioSource.volume = value; }
        }
        
        /// <summary>
        /// Returns the unique identifier for this sound instance
        /// </summary>
        public string SoundId => _soundId;

        private void Awake()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        /// <summary>
        /// Initializes the AudioPlayer with the specified mixer group
        /// </summary>
        /// <param name="mixerGroup">The AudioMixerGroup to route audio through</param>
        /// <param name="soundId">Unique identifier for this sound instance</param>
        public void Initialize(AudioMixerGroup mixerGroup, string soundId)
        {
            _soundId = soundId;
            _audioSource.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// Plays an audio clip
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume">Volume level from 0 to 1</param>
        /// <param name="loop">Whether to loop the audio</param>
        /// <param name="onComplete">Action to execute when audio finishes playing (not called when looping)</param>
        public void Play(AudioClip clip, float volume = 1f, bool loop = false, Action onComplete = null)
        {
            StopAllCoroutines();
            _fadeCoroutine = null;
            _completionCoroutine = null;
            _originalVolume = volume;
            
            _audioSource.clip = clip;
            _audioSource.volume = volume;
            _audioSource.loop = loop;
            _audioSource.time = 0;
            _onComplete = onComplete;
            _isPaused = false;
            
            _audioSource.Play();

            if (!loop && onComplete != null)
            {
                _completionCoroutine = StartCoroutine(WaitForCompletion());
            }
        }

        /// <summary>
        /// Plays a one-shot sound that doesn't track completion
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume">Volume level from 0 to 1</param>
        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            _audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Pauses the currently playing audio
        /// </summary>
        public void Pause()
        {
            if (_audioSource == null || !IsPlaying || _isPaused) return;
            
            _pausedTime = _audioSource.time;
            _audioSource.Pause();
            _isPaused = true;
            
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        /// <summary>
        /// Resumes playing from a paused state
        /// </summary>
        public void Resume()
        {
            if (_audioSource == null || !_isPaused) return;
            
            _audioSource.time = _pausedTime;
            _audioSource.Play();
            _isPaused = false;
        }

        /// <summary>
        /// Stops the current audio and resets state
        /// </summary>
        public void Stop()
        {
            if (_audioSource == null) return;
            
            StopAllCoroutines();
            _fadeCoroutine = null;
            _completionCoroutine = null;
            _onComplete = null;
            
            _audioSource.Stop();
            _isPaused = false;
        }

        /// <summary>
        /// Fades in the audio from zero to target volume
        /// </summary>
        /// <param name="duration">Duration of the fade in seconds</param>
        /// <param name="targetVolume">Target volume to reach</param>
        public void FadeIn(float duration, float targetVolume = 1f)
        {
            if (_audioSource == null || duration <= 0) return;
            
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            
            _originalVolume = targetVolume;
            _audioSource.volume = 0;
            
            if (!IsPlaying && !_isPaused)
            {
                _audioSource.Play();
            }
            
            _fadeCoroutine = StartCoroutine(FadeCoroutine(0, targetVolume, duration));
        }

        /// <summary>
        /// Fades out the audio from current volume to zero
        /// </summary>
        /// <param name="duration">Duration of the fade in seconds</param>
        /// <param name="stopAfterFade">Whether to stop the audio after fading out</param>
        /// <param name="onComplete">Callback when fade completes</param>
        public void FadeOut(float duration, bool stopAfterFade = true, Action onComplete = null)
        {
            if (_audioSource == null || !IsPlaying || duration <= 0) return;
            
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            
            _fadeCoroutine = StartCoroutine(FadeCoroutine(_audioSource.volume, 0, duration, () => {
                if (stopAfterFade)
                {
                    Stop();
                }
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Coroutine that fades the volume over time
        /// </summary>
        private IEnumerator FadeCoroutine(float startVolume, float endVolume, float duration, Action onComplete = null)
        {
            float startTime = Time.time;
            float elapsedTime = 0;
            
            while (elapsedTime < duration)
            {
                elapsedTime = Time.time - startTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                _audioSource.volume = Mathf.Lerp(startVolume, endVolume, t);
                yield return null;
            }
            
            _audioSource.volume = endVolume;
            onComplete?.Invoke();
            _fadeCoroutine = null;
        }

        /// <summary>
        /// Coroutine that waits for the audio clip to finish playing
        /// </summary>
        private IEnumerator WaitForCompletion()
        {
            // Wait until audio is no longer playing
            while (IsPlaying || _isPaused)
            {
                yield return null;
            }
            
            // We got here, so the clip has completed playing and wasn't just paused
            if (!_isPaused && _onComplete != null)
            {
                Action callback = _onComplete;
                _onComplete = null;
                callback.Invoke();
            }
            
            _completionCoroutine = null;
        }
    }
}