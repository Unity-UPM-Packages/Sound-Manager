using System;
using UnityEngine;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Represents a sound address in the Addressable system, including its channel type and address
    /// </summary>
    [Serializable]
    public class SoundAddress
    {
        /// <summary>
        /// Channel type for the sound (Music, Vfx, UI)
        /// </summary>
        public AudioChannelType ChannelType;
        
        /// <summary>
        /// Addressable address for loading the sound
        /// </summary>
        public string Address;
        
        /// <summary>
        /// Optional volume multiplier specific to this sound
        /// </summary>
        [Range(0, 1)]
        public float VolumeScale = 1f;

        public SoundAddress() { }

        public SoundAddress(AudioChannelType channelType, string address)
        {
            ChannelType = channelType;
            Address = address;
        }
    }
}