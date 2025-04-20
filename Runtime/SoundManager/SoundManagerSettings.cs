using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// ScriptableObject containing all settings for the SoundManager
    /// </summary>
    [CreateAssetMenu(fileName = "SoundManagerSettings", menuName = "TripSoft/Sound Manager/Settings")]
    public class SoundManagerSettings : ScriptableObject
    {
        [Header("Audio Mixer")]
        [Tooltip("The AudioMixer used for routing and volume control")]
        public AudioMixer AudioMixer;
        
        [Header("Pool Settings")]
        [Tooltip("Initial number of audio players to create in each pool")]
        [Range(5, 50)]
        public int InitialPoolSize = 10;
        
        [Header("Default Volumes")]
        [Tooltip("Default volume for Master channel")]
        [Range(0, 1)]
        public float DefaultMasterVolume = 1f;
        
        [Tooltip("Default volume for Music channel")]
        [Range(0, 1)]
        public float DefaultMusicVolume = 0.7f;
        
        [Tooltip("Default volume for Vfx channel")]
        [Range(0, 1)]
        public float DefaultVfxVolume = 1f;
        
        [Tooltip("Default volume for UI channel")]
        [Range(0, 1)]
        public float DefaultUIVolume = 0.8f;
        
        [Header("Performance Settings")]
        [Tooltip("Maximum concurrent sounds to allow playing at once (0 = unlimited)")]
        public int MaxConcurrentSounds = 32;
        
        [Tooltip("Preload addressable audio clips at startup to avoid loading during gameplay")]
        public bool PreloadAddressables = false;
        
        [Header("Addressable Sounds")]
        [Tooltip("List of all sound addresses managed by the sound manager")]
        public List<SoundAddress> SoundAddresses = new List<SoundAddress>();
        
        [Header("Debug Settings")]
        [Tooltip("Enable debug logging")]
        public bool DebugLogging = false;
    }
}