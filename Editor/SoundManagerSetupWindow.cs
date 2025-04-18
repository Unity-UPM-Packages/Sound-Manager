using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace com.thelegends.sound.manager.Editor
{
    /// <summary>
    /// Editor window for setting up and managing the SoundManager
    /// </summary>
    public class SoundManagerSetupWindow : EditorWindow
    {
        private SoundManagerSettings _settings;
        private AudioMixer _audioMixer;
        private bool _showSoundAddresses = false;
        private Vector2 _scrollPosition;
        private string _searchFilter = string.Empty;
        private bool _showMusicSounds = true;
        private bool _showVfxSounds = true;
        private bool _showUISounds = true;

        [MenuItem("Tools/TripSoft/Sound Manager Setup")]
        public static void ShowWindow()
        {
            SoundManagerSetupWindow window = GetWindow<SoundManagerSetupWindow>();
            window.titleContent = new GUIContent("Sound Manager Setup");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // Try to find existing settings
            string[] guids = AssetDatabase.FindAssets("t:SoundManagerSettings");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<SoundManagerSettings>(path);
            }
            
            // Try to find existing mixer
            _audioMixer = AssetDatabase.LoadAssetAtPath<AudioMixer>("Assets/TripSoft/SoundManager/MainAudioMixer.mixer");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Sound Manager Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Settings object
            EditorGUI.BeginChangeCheck();
            _settings = (SoundManagerSettings)EditorGUILayout.ObjectField("Settings Asset", _settings, typeof(SoundManagerSettings), false);
            if (EditorGUI.EndChangeCheck() && _settings != null)
            {
                _audioMixer = _settings.AudioMixer;
            }
            
            EditorGUILayout.Space();
            
            // Audio Mixer
            _audioMixer = (AudioMixer)EditorGUILayout.ObjectField("Audio Mixer", _audioMixer, typeof(AudioMixer), false);
            
            EditorGUILayout.Space();

            // Assets settings section
            EditorGUILayout.LabelField("Assets", EditorStyles.boldLabel);
            
            // Create settings asset
            if (_settings == null)
            {
                if (GUILayout.Button("Create Settings Asset"))
                {
                    CreateSettingsAsset();
                }
            }
            
            // Create audio mixer
            if (_audioMixer == null)
            {
                if (GUILayout.Button("Create Audio Mixer"))
                {
                    _audioMixer = AudioMixerSetup.CreateOrGetAudioMixer();
                    
                    if (_settings != null)
                    {
                        _settings.AudioMixer = _audioMixer;
                        EditorUtility.SetDirty(_settings);
                        AssetDatabase.SaveAssets();
                    }
                }
            }
            
            EditorGUILayout.Space();
            
            // Update Addressables
            if (GUILayout.Button("Scan and Configure Addressables"))
            {
                List<SoundAddress> addresses = AudioAddressableSetup.SetupAddressables();
                
                if (_settings != null && addresses != null)
                {
                    // Merge with existing addresses if there are any, preserving custom settings
                    MergeAddresses(_settings.SoundAddresses, addresses);
                    
                    // Automatically generate sound key constants after scanning
                    GenerateSoundKeysConstants(_settings.SoundAddresses);
                    
                    EditorUtility.SetDirty(_settings);
                    AssetDatabase.SaveAssets();
                    
                    Debug.Log("Addressables scan complete and Sound Keys constants file generated successfully!");
                }
            }
            
            EditorGUILayout.Space();
            
            // Create SoundManager GameObject
            if (GUILayout.Button("Create SoundManager GameObject"))
            {
                CreateSoundManagerGameObject();
            }
            
            EditorGUILayout.Space();
            
            // Show sound addresses with filtering
            if (_settings != null && _settings.SoundAddresses != null && _settings.SoundAddresses.Count > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                // Header with filtering options
                EditorGUILayout.BeginHorizontal();
                _showSoundAddresses = EditorGUILayout.Foldout(_showSoundAddresses, 
                    $"Sound Addresses ({_settings.SoundAddresses.Count})");
                
                if (_showSoundAddresses)
                {
                    // Search field
                    _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(150));
                    
                    GUILayout.FlexibleSpace();
                    
                    // Filter toggles by audio type
                    _showMusicSounds = GUILayout.Toggle(_showMusicSounds, "Music", EditorStyles.miniButtonLeft);
                    _showVfxSounds = GUILayout.Toggle(_showVfxSounds, "VFX", EditorStyles.miniButtonMid);
                    _showUISounds = GUILayout.Toggle(_showUISounds, "UI", EditorStyles.miniButtonRight);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // Draw sound addresses list
                if (_showSoundAddresses)
                {
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    
                    // Column headers
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Key", EditorStyles.boldLabel, GUILayout.Width(200));
                    EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.LabelField("Volume", EditorStyles.boldLabel, GUILayout.Width(60));
                    EditorGUILayout.EndHorizontal();
                    
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(300));
                    
                    // Filter by type and search term
                    foreach (var address in _settings.SoundAddresses)
                    {
                        // Check if we should show this type
                        bool showByType = 
                            (address.ChannelType == AudioChannelType.Music && _showMusicSounds) ||
                            (address.ChannelType == AudioChannelType.Vfx && _showVfxSounds) ||
                            (address.ChannelType == AudioChannelType.UI && _showUISounds);
                            
                        // Check if it matches search (if any)
                        bool matchesSearch = string.IsNullOrEmpty(_searchFilter) || 
                            address.Key.ToLower().Contains(_searchFilter.ToLower());
                            
                        if (showByType && matchesSearch)
                        {
                            EditorGUILayout.BeginHorizontal();
                            
                            // Show key (with tooltip for address)
                            EditorGUILayout.LabelField(new GUIContent(address.Key, address.Address), 
                                GUILayout.Width(200));
                            
                            // Show channel type
                            EditorGUILayout.LabelField(address.ChannelType.ToString(), GUILayout.Width(60));
                            
                            // Allow editing the volume scale
                            float newVolume = EditorGUILayout.Slider(address.VolumeScale, 0f, 1f, GUILayout.Width(100));
                            if (newVolume != address.VolumeScale)
                            {
                                address.VolumeScale = newVolume;
                                EditorUtility.SetDirty(_settings);
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        /// <summary>
        /// Generates a C# file with constants for all sound keys
        /// </summary>
        private void GenerateSoundKeysConstants(List<SoundAddress> soundAddresses)
        {
            if (soundAddresses == null || soundAddresses.Count == 0)
            {
                Debug.LogWarning("No sound addresses to generate constants for.");
                return;
            }

            string directory = "Assets/TripSoft/SoundManager";
            
            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string parentFolder = Path.GetDirectoryName(directory).Replace('\\', '/');
                string folderName = Path.GetFileName(directory);
                
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "TripSoft");
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create the constants file content
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// This file is auto-generated. Do not modify manually.");
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace com.thelegends.sound.manager");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Constants for Sound Keys to use with SoundManager");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class SoundKeys");
            sb.AppendLine("    {");
            
            // Add special constant for controlling background music
            sb.AppendLine();
            sb.AppendLine("        #region Special Sound IDs");
            sb.AppendLine("        /// <summary>Special ID for controlling the currently playing background music</summary>");
            sb.AppendLine("        /// <remarks>Use with Pause(), Resume(), Stop(), etc. methods - NOT with Play()</remarks>");
            sb.AppendLine("        public const string MUSIC = \"music\";");
            sb.AppendLine("        #endregion");

            // Group addresses by channel type for better organization
            Dictionary<AudioChannelType, List<SoundAddress>> groupedAddresses = new Dictionary<AudioChannelType, List<SoundAddress>>();
            
            foreach (var address in soundAddresses)
            {
                if (!groupedAddresses.ContainsKey(address.ChannelType))
                {
                    groupedAddresses[address.ChannelType] = new List<SoundAddress>();
                }
                
                groupedAddresses[address.ChannelType].Add(address);
            }

            // Output constants by category
            foreach (var channelType in groupedAddresses.Keys)
            {
                sb.AppendLine();
                sb.AppendLine($"        #region {channelType}");

                foreach (var address in groupedAddresses[channelType].OrderBy(a => a.Key))
                {
                    // Convert key to valid C# identifier (replace slashes with underscores)
                    string constantName = address.Key.Replace('/', '_').Replace('-', '_').ToUpperInvariant();
                    sb.AppendLine($"        /// <summary>Addressable key for {address.Key}</summary>");
                    sb.AppendLine($"        public const string {constantName} = \"{address.Key}\";");
                }

                sb.AppendLine($"        #endregion");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            // Write to file
            string filePath = $"{directory}/SoundKeys.cs";
            File.WriteAllText(filePath, sb.ToString());
            AssetDatabase.Refresh();
            
            Debug.Log($"Generated sound key constants at {filePath}");
        }

        /// <summary>
        /// Merges new addresses with existing ones, preserving custom settings like volume scale
        /// </summary>
        private void MergeAddresses(List<SoundAddress> existing, List<SoundAddress> newAddresses)
        {
            // Create a dictionary of existing addresses by key for quick lookup
            Dictionary<string, SoundAddress> existingDict = new Dictionary<string, SoundAddress>();
            foreach (var address in existing)
            {
                existingDict[address.Key] = address;
            }
            
            // Create the merged list
            List<SoundAddress> merged = new List<SoundAddress>();
            
            // Add all new addresses, preserving settings from existing ones where applicable
            foreach (var newAddress in newAddresses)
            {
                if (existingDict.TryGetValue(newAddress.Key, out SoundAddress existingAddress))
                {
                    // Preserve volume scale but update address (in case path changed)
                    newAddress.VolumeScale = existingAddress.VolumeScale;
                }
                
                merged.Add(newAddress);
            }
            
            // Update the settings with the merged list
            _settings.SoundAddresses = merged;
        }

        private void CreateSettingsAsset()
        {
            string directory = "Assets/TripSoft/SoundManager";
            
            // Create folder if it doesn't exist
            if (!AssetDatabase.IsValidFolder(directory))
            {
                string parentFolder = Path.GetDirectoryName(directory).Replace('\\', '/');
                string folderName = Path.GetFileName(directory);
                
                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    AssetDatabase.CreateFolder("Assets", "TripSoft");
                }
                
                AssetDatabase.CreateFolder(parentFolder, folderName);
            }
            
            // Create settings asset
            SoundManagerSettings settings = CreateInstance<SoundManagerSettings>();
            settings.AudioMixer = _audioMixer;
            settings.InitialPoolSize = 10;
            settings.DefaultMasterVolume = 1f;
            settings.DefaultMusicVolume = 0.8f;
            settings.DefaultVfxVolume = 1f;
            settings.DefaultUIVolume = 0.7f;
            
            string assetPath = $"{directory}/SoundManagerSettings.asset";
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            
            _settings = settings;
            Selection.activeObject = settings;
            
            Debug.Log($"Created SoundManagerSettings at {assetPath}");
        }

        private void CreateSoundManagerGameObject()
        {
            // Check if we already have one
            SoundManager existingManager = FindObjectOfType<SoundManager>();
            if (existingManager != null)
            {
                Selection.activeGameObject = existingManager.gameObject;
                EditorGUIUtility.PingObject(existingManager.gameObject);
                Debug.Log("SoundManager already exists in the scene.");
                return;
            }
            
            // Create new GameObject with SoundManager
            GameObject go = new GameObject("SoundManager");
            SoundManager manager = go.AddComponent<SoundManager>();
            
            // Set up references
            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("_audioMixer").objectReferenceValue = _audioMixer;
            so.FindProperty("_settings").objectReferenceValue = _settings;
            so.ApplyModifiedProperties();
            
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            
            Debug.Log("Created SoundManager GameObject in the current scene");
        }
    }
}