using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using System.Collections.Generic;
using System.IO;

namespace com.thelegends.sound.manager.Editor
{
    /// <summary>
    /// Editor utility for automatically setting up addressable sound assets
    /// </summary>
    public static class AudioAddressableSetup
    {
        private const string MusicFolderPath = "Assets/TripSoft/SoundManager/Music";
        private const string VfxFolderPath = "Assets/TripSoft/SoundManager/Vfx";
        private const string UIFolderPath = "Assets/TripSoft/SoundManager/UI";
        
        private const string AudioGroupPrefix = "Sound_";
        private const string MusicGroupName = AudioGroupPrefix + "Music";
        private const string VfxGroupName = AudioGroupPrefix + "Vfx";
        private const string UIGroupName = AudioGroupPrefix + "UI";

        /// <summary>
        /// Sets up Addressable assets for all sound files in the standard folders
        /// </summary>
        /// <returns>List of configured sound addresses</returns>
        public static List<SoundAddress> SetupAddressables()
        {
            // Initialize Addressables if needed
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("Failed to initialize or find Addressable Asset Settings.");
                return null;
            }
            
            List<SoundAddress> allSoundAddresses = new List<SoundAddress>();
            
            // Create the base directory structure if it doesn't exist
            CreateSoundFolderStructure();
            
            // Process each folder with appropriate channel type
            AddressableAssetGroup musicGroup = CreateOrGetAddressableGroup(settings, MusicGroupName);
            List<SoundAddress> musicAddresses = ProcessAudioFolder(settings, MusicFolderPath, AudioChannelType.Music, musicGroup);
            allSoundAddresses.AddRange(musicAddresses);
            
            AddressableAssetGroup vfxGroup = CreateOrGetAddressableGroup(settings, VfxGroupName);
            List<SoundAddress> vfxAddresses = ProcessAudioFolder(settings, VfxFolderPath, AudioChannelType.Vfx, vfxGroup);
            allSoundAddresses.AddRange(vfxAddresses);
            
            AddressableAssetGroup uiGroup = CreateOrGetAddressableGroup(settings, UIGroupName);
            List<SoundAddress> uiAddresses = ProcessAudioFolder(settings, UIFolderPath, AudioChannelType.UI, uiGroup);
            allSoundAddresses.AddRange(uiAddresses);
            
            Debug.Log($"Sound Manager Addressables setup completed. Processed {allSoundAddresses.Count} audio files.");
            return allSoundAddresses;
        }

        /// <summary>
        /// Creates or gets an existing Addressable asset group
        /// </summary>
        private static AddressableAssetGroup CreateOrGetAddressableGroup(AddressableAssetSettings settings, string groupName)
        {
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                group = settings.CreateGroup(groupName, false, false, true, null);
                
                // Add default schemas - using proper API for adding schemas
                
                // Add ContentUpdateGroupSchema
                var contentUpdateSchema = ScriptableObject.CreateInstance<ContentUpdateGroupSchema>();
                contentUpdateSchema.name = "ContentUpdateGroupSchema";
                group.AddSchema(contentUpdateSchema);
                
                // Add BundledAssetGroupSchema
                var bundledAssetSchema = ScriptableObject.CreateInstance<BundledAssetGroupSchema>();
                bundledAssetSchema.name = "BundledAssetGroupSchema";
                bundledAssetSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
                bundledAssetSchema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
                group.AddSchema(bundledAssetSchema);
            }
            return group;
        }
        
        /// <summary>
        /// Creates folder structure for sound assets if it doesn't exist
        /// </summary>
        private static void CreateSoundFolderStructure()
        {
            // Create base folders
            if (!AssetDatabase.IsValidFolder("Assets/TripSoft"))
            {
                AssetDatabase.CreateFolder("Assets", "TripSoft");
            }
            
            if (!AssetDatabase.IsValidFolder("Assets/TripSoft/SoundManager"))
            {
                AssetDatabase.CreateFolder("Assets/TripSoft", "SoundManager");
            }
            
            // Create specific sound type folders
            if (!AssetDatabase.IsValidFolder(MusicFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/TripSoft/SoundManager", "Music");
            }
            
            if (!AssetDatabase.IsValidFolder(VfxFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/TripSoft/SoundManager", "Vfx");
            }
            
            if (!AssetDatabase.IsValidFolder(UIFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/TripSoft/SoundManager", "UI");
            }
        }

        /// <summary>
        /// Process all audio files in a given folder and add them to Addressables
        /// </summary>
        private static List<SoundAddress> ProcessAudioFolder(
            AddressableAssetSettings settings, 
            string folderPath, 
            AudioChannelType channelType, 
            AddressableAssetGroup group)
        {
            List<SoundAddress> soundAddresses = new List<SoundAddress>();
            
            // Find all audio clips in the folder and subfolders
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
                
                if (clip != null)
                {
                    // Get the relative path from the base folder
                    string relativePath = assetPath.Substring(folderPath.Length + 1);
                    // Remove extension
                    relativePath = Path.ChangeExtension(relativePath, null);
                    // Replace backslashes with forward slashes
                    relativePath = relativePath.Replace('\\', '/');
                    
                    // Create a key for the clip based on its channel and path
                    string key = $"{channelType.ToString().ToLower()}/{relativePath}";
                    
                    // Add to Addressables or update existing entry
                    AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
                    entry.address = key;
                    
                    // Add to our list of sound addresses
                    soundAddresses.Add(new SoundAddress(key, channelType, key));
                    
                    Debug.Log($"Added {key} to Addressables");
                }
            }
            
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
            return soundAddresses;
        }
    }
}