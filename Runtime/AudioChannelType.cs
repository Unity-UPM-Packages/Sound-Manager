using System;

namespace com.thelegends.sound.manager
{
    /// <summary>
    /// Defines the different audio channel types for organization and mixing
    /// </summary>
    [Serializable]
    public enum AudioChannelType
    {
        /// <summary>
        /// Master channel controlling overall volume
        /// </summary>
        Master,
        
        /// <summary>
        /// Channel for background music tracks
        /// </summary>
        Music,
        
        /// <summary>
        /// Channel for sound effects
        /// </summary>
        Vfx,
        
        /// <summary>
        /// Channel for UI-related sounds
        /// </summary>
        UI
    }
}