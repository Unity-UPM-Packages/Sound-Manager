# Sound Manager Documentation

## Table of Contents
1. [Introduction](#introduction)
2. [Installation](#installation)
3. [Setup](#setup)
   - [Sound Manager Setup Window](#sound-manager-setup-window)
   - [Creating Essential Assets](#creating-essential-assets)
   - [Setting Up Addressables](#setting-up-addressables)
   - [Creating SoundManager GameObject](#creating-soundmanager-gameobject)
4. [Audio File Organization](#audio-file-organization)
5. [Usage](#usage)
   - [Playing Sounds](#playing-sounds)
   - [Controlling Playback](#controlling-playback)
   - [Volume Control](#volume-control)
   - [Fading In/Out](#fading-in-and-out)
6. [Advanced Features](#advanced-features)
   - [Sound ID System](#sound-id-system)
   - [Events](#events)
   - [Performance Settings](#performance-settings)
7. [Troubleshooting](#troubleshooting)
8. [API Reference](#api-reference)

## Introduction

Sound Manager is a comprehensive audio solution for Unity, providing a streamlined way to manage and play audio in your game projects. Features include:

- **Channel-based audio organization**: Music, VFX, and UI sounds
- **Volume control** for each audio channel with automatic PlayerPrefs persistence
- **Addressable asset integration** for efficient audio loading and memory management
- **Customizable audio pooling** to prevent performance issues
- **Fade in/out controls** for smooth audio transitions
- **Automatic audio mixing** with properly configured AudioMixerGroups

This package handles the technical complexities of audio management in Unity, allowing you to focus on creating great audio experiences for your players.

## Installation

### Requirements
- Unity 2019.4 or newer
- Addressables package

## Setup

### Sound Manager Setup Window

The Sound Manager comes with an editor window to help you set up all necessary components. To access it:

1. Go to the Unity menu: `Tools > TripSoft > Sound Manager Setup`
2. This will open the Sound Manager Setup window, providing various configuration options

### Creating Essential Assets

Before using the Sound Manager, you need to create two essential assets:

1. **Settings Asset**
   - In the Sound Manager Setup window, click "Create Settings Asset"
   - This will create a SoundManagerSettings asset at `Assets/TripSoft/SoundManager/SoundManagerSettings.asset`
   - This asset contains all the configuration settings for Sound Manager

2. **Audio Mixer**
   - In the Sound Manager Setup window, click "Create Audio Mixer"
   - This will create an AudioMixer asset at `Assets/TripSoft/SoundManager/MainAudioMixer.mixer`
   - The mixer will be configured with the needed groups (Master, Music, VFX, UI) and exposed parameters

   **Details about exposed parameters in AudioMixer:**
   
   The AudioMixer automatically sets up and exposes the following parameters, allowing you to control volume through code:
   
   | Parameter | Exposed Name | Default Value | Description |
   |-----------|-------------|--------------|-------------|
   | MasterVolume | "MasterVolume" | 0dB | Controls the overall volume of all sounds |
   | MusicVolume | "MusicVolume" | 0dB | Controls the volume for the music group |
   | VfxVolume | "VfxVolume" | 0dB | Controls the volume for sound effects |
   | UIVolume | "UIVolume" | 0dB | Controls the volume for interface sounds |
   
   *Note: These values are set in decibel units (dB) in the AudioMixer. A value of 0dB corresponds to unchanged volume. In code, when you use the `SetVolume()` method, volume is automatically converted from the 0-1 scale to the logarithmic decibel scale required by AudioMixer.*
   
   The parameter exposing process is performed by the `ExposeMixerParameters()` method in `AudioMixerSetup.cs`:
   ```csharp
   private static void ExposeMixerParameters(AudioMixer mixer)
   {
       SerializedObject serializedObject = new SerializedObject(mixer);
       SerializedProperty exposedParams = serializedObject.FindProperty("exposedParameters");
       
       if (exposedParams == null)
       {
           Debug.LogError("Could not find 'exposedParameters' property in AudioMixer.");
           return;
       }
       
       AddExposedParameter(exposedParams, MasterVolumeParam); // "MasterVolume"
       AddExposedParameter(exposedParams, MusicVolumeParam);  // "MusicVolume"
       AddExposedParameter(exposedParams, VfxVolumeParam);    // "VfxVolume"
       AddExposedParameter(exposedParams, UIVolumeParam);     // "UIVolume"
       
       serializedObject.ApplyModifiedProperties();
   }
   ```

### Setting Up Addressables

Sound Manager uses Unity's Addressable Asset System to efficiently load and manage audio clips. Follow these steps to set up your audio files:

1. Organize your audio files in the Unity project
2. Place them in the following directories (they will be created if they don't exist):
   - Music: `Assets/TripSoft/SoundManager/Music/`
   - Sound Effects: `Assets/TripSoft/SoundManager/Vfx/`
   - UI Sounds: `Assets/TripSoft/SoundManager/UI/`
3. In the Sound Manager Setup window, click "Scan and Configure Addressables"
4. This will:
   - Set up addressable groups for your audio files
   - Create addressable entries for each audio file with appropriate labels
   - Generate a SoundKeys.cs file with constants for easy access to your sound files

### Creating SoundManager GameObject

Finally, you need to add the SoundManager component to your scene:

1. In the Sound Manager Setup window, click "Create SoundManager GameObject"
2. This will create a GameObject with the SoundManager component attached
3. The SoundManager is configured as a singleton and will persist between scene loads

> **Note**: Make sure to include the SoundManager in your first/initial scene to ensure audio functionality throughout your game.

## Audio File Organization

Sound Manager uses a channel-based system to organize and control audio:

- **Music**: Background music, ambient tracks, etc.
- **VFX**: Sound effects for gameplay events, environment, etc.
- **UI**: Interface sounds, button clicks, etc.

Each channel has its own volume control and can be managed independently.

To add sounds to your project:

1. Place audio files in the appropriate folders:
   - `Assets/TripSoft/SoundManager/Music/`
   - `Assets/TripSoft/SoundManager/Vfx/`
   - `Assets/TripSoft/SoundManager/UI/`
2. Run "Scan and Configure Addressables" in the Sound Manager Setup window
3. Your sounds will be available through the generated constants in `SoundKeys.cs`

## Usage

### Playing Sounds

To play sounds in your scripts, first reference the SoundManager namespace:

```csharp
using com.thelegends.sound.manager;
```

#### Basic Sound Playback

```csharp
// Play a one-shot sound effect (fire and forget)
SoundManager.Instance.PlayOneShot(SoundKeys.VFX_EXPLOSION);

// Play sound with volume control
SoundManager.Instance.PlayOneShot(SoundKeys.UI_BUTTON_CLICK, 0.5f);

// Play a sound and get its ID for later control
string soundId = SoundManager.Instance.Play(SoundKeys.VFX_AMBIENT_WIND);

// Play looping sound
string loopingSoundId = SoundManager.Instance.Play(SoundKeys.VFX_ENGINE_IDLE, 1f, true);
```

#### Playing Background Music

```csharp
// Play background music (automatically stops previous music)
SoundManager.Instance.Play(SoundKeys.MUSIC_MAIN_THEME, 0.7f, true);

// Play with fade-in effect (3 seconds)
SoundManager.Instance.Play(SoundKeys.MUSIC_BATTLE, 1f, true, 3.0f);

// Play with completion callback
SoundManager.Instance.Play(SoundKeys.MUSIC_VICTORY, 1f, false, 0f, () => {
    Debug.Log("Victory music finished!");
});
```

### Controlling Playback

Use the sound ID returned from the Play method to control playback:

```csharp
// Play sound and store ID
string soundId = SoundManager.Instance.Play(SoundKeys.VFX_ALARM);

// Pause the sound
SoundManager.Instance.Pause(soundId);

// Resume the sound
SoundManager.Instance.Resume(soundId);

// Stop the sound
SoundManager.Instance.Stop(soundId);

// Check if sound is playing
if (SoundManager.Instance.IsPlaying(soundId)) {
    Debug.Log("Sound is still playing");
}
```

#### Special Case: Music Control

You can use a special ID to control the currently playing music track:

```csharp
// Pause the current music track
SoundManager.Instance.Pause(SoundKeys.MUSIC);

// Resume the current music track
SoundManager.Instance.Resume(SoundKeys.MUSIC);

// Stop the current music track
SoundManager.Instance.Stop(SoundKeys.MUSIC);
```

### Volume Control

#### Channel Volume

Control the volume of entire audio channels:

```csharp
// Set master volume (affects all sounds)
SoundManager.Instance.SetVolume(AudioChannelType.Master, 0.8f);

// Set music volume
SoundManager.Instance.SetVolume(AudioChannelType.Music, 0.5f);

// Set sound effects volume
SoundManager.Instance.SetVolume(AudioChannelType.Vfx, 0.7f);

// Set UI sounds volume
SoundManager.Instance.SetVolume(AudioChannelType.UI, 0.6f);

// Get current volume of a channel
float musicVolume = SoundManager.Instance.GetVolume(AudioChannelType.Music);

// Toggle mute state for a channel
bool isMuted = SoundManager.Instance.ToggleMute(AudioChannelType.Music);
```

#### Individual Sound Volume

Control the volume of specific sound instances:

```csharp
string soundId = SoundManager.Instance.Play(SoundKeys.VFX_AMBIENT_RAIN);

// Adjust volume of playing sound
SoundManager.Instance.SetSoundVolume(soundId, 0.3f);
```

### Fading In and Out

Create smooth transitions with fade effects:

```csharp
// Play with fade in
string soundId = SoundManager.Instance.Play(SoundKeys.MUSIC_LEVEL_START, 1f, true, 2.5f);

// Fade in an already playing sound
SoundManager.Instance.FadeIn(soundId, 3.0f, 0.8f);

// Fade out a sound (without stopping)
SoundManager.Instance.FadeOut(soundId, 2.0f, false);

// Fade out a sound and stop after fade completes
SoundManager.Instance.FadeOut(soundId, 1.5f, true);
```

### Stopping Multiple Sounds

```csharp
// Stop all sounds in a specific channel
SoundManager.Instance.StopChannel(AudioChannelType.Vfx);

// Stop all sounds in all channels
SoundManager.Instance.StopAll();
```

## Advanced Features

### Sound ID System

Sound Manager uses a unique ID system to track and control each sound instance:

- Non-music sounds get a unique ID each time they are played
- The current music track is always accessible through the fixed ID `SoundKeys.MUSIC`
- IDs can be stored and used for controlling sounds throughout their lifecycle

### Events

Subscribe to events to monitor sound activity:

```csharp
// Register for sound started events
SoundManager.Instance.OnSoundStarted += HandleSoundStarted;

// Register for sound stopped events
SoundManager.Instance.OnSoundStopped += HandleSoundStopped;

// Event handlers
private void HandleSoundStarted(string soundId, AudioChannelType channelType) {
    Debug.Log($"Sound started: {soundId} on channel {channelType}");
}

private void HandleSoundStopped(string soundId, AudioChannelType channelType) {
    Debug.Log($"Sound stopped: {soundId} on channel {channelType}");
}
```

### Performance Settings

The Sound Manager settings asset contains several performance-related settings:

- **Initial Pool Size**: The number of audio players pre-created at startup
- **Max Concurrent Sounds**: Maximum number of sounds that can play simultaneously
- **Preload Addressables**: Whether to preload audio clips at startup for faster playback

You can adjust these settings in the Inspector for the SoundManagerSettings asset.

## Troubleshooting

### Common Issues

1. **No Sound Playing**
   - Check if SoundManager GameObject exists in the scene
   - Verify that the AudioMixer and Settings assets are properly assigned
   - Check if the volume levels are not set to 0 or muted

2. **Sound Keys Not Found**
   - Make sure you've run "Scan and Configure Addressables" after adding new audio files
   - Check if audio files are placed in the correct folder structure

3. **Error: SoundManager is not initialized**
   - Ensure the SoundManager is instantiated in the first scene of your game
   - Check for any script execution order issues

### Debug Logging

You can enable debug logging in the SoundManager settings to get more information about audio operations:

1. Select the SoundManagerSettings asset in the Project window
2. Check the "Debug Logging" option in the Inspector
3. Run your game and check the Console for detailed logs

## API Reference

### Core Methods

```csharp
// Play methods
string Play(string soundKey, float volume = 1f, bool loop = false, float fadeInDuration = 0f, Action onComplete = null);
void PlayOneShot(string soundKey, float volume = 1f);

// Playback control
void Pause(string soundId);
void Resume(string soundId);
void Stop(string soundId);
bool IsPlaying(string soundId);

// Volume control
void SetVolume(AudioChannelType channelType, float volume);
float GetVolume(AudioChannelType channelType);
bool ToggleMute(AudioChannelType channelType);
void SetSoundVolume(string soundId, float volume);

// Fade effects
void FadeIn(string soundId, float duration, float targetVolume = 1f);
void FadeOut(string soundId, float duration, bool stopAfterFade = true);

// Multiple sound control
void StopChannel(AudioChannelType channelType);
void StopAll();
```

### Events

```csharp
event Action<string, AudioChannelType> OnSoundStarted;
event Action<string, AudioChannelType> OnSoundStopped;
```

### Enums

```csharp
// Audio channel types
public enum AudioChannelType
{
    Master,
    Music,
    Vfx,
    UI
}
```

---

For more information or support, please contact us at support@example.com or visit our website at https://example.com

---

# Tài Liệu Sound Manager

## Mục Lục
1. [Giới Thiệu](#giới-thiệu)
2. [Cài Đặt](#cài-đặt)
3. [Thiết Lập](#thiết-lập)
   - [Cửa Sổ Thiết Lập Sound Manager](#cửa-sổ-thiết-lập-sound-manager)
   - [Tạo Các Tài Nguyên Cần Thiết](#tạo-các-tài-nguyên-cần-thiết)
   - [Thiết Lập Addressables](#thiết-lập-addressables)
   - [Tạo GameObject SoundManager](#tạo-gameobject-soundmanager)
4. [Tổ Chức Tệp Âm Thanh](#tổ-chức-tệp-âm-thanh)
5. [Cách Sử Dụng](#cách-sử-dụng)
   - [Phát Âm Thanh](#phát-âm-thanh)
   - [Điều Khiển Việc Phát](#điều-khiển-việc-phát)
   - [Điều Chỉnh Âm Lượng](#điều-chỉnh-âm-lượng)
   - [Hiệu Ứng Fade In/Out](#hiệu-ứng-fade-inout)
6. [Tính Năng Nâng Cao](#tính-năng-nâng-cao)
   - [Hệ Thống Sound ID](#hệ-thống-sound-id)
   - [Sự Kiện](#sự-kiện)
   - [Cài Đặt Hiệu Suất](#cài-đặt-hiệu-suất)
7. [Xử Lý Sự Cố](#xử-lý-sự-cố)
8. [Tài Liệu Tham Khảo API](#tài-liệu-tham-khảo-api)

## Giới Thiệu

Sound Manager là một giải pháp âm thanh toàn diện cho Unity, cung cấp cách đơn giản và hiệu quả để quản lý và phát âm thanh trong dự án game của bạn. Các tính năng bao gồm:

- **Tổ chức âm thanh theo kênh**: Nhạc nền, Hiệu ứng âm thanh, và Âm thanh giao diện
- **Điều khiển âm lượng** cho từng kênh âm thanh với khả năng lưu trữ tự động qua PlayerPrefs
- **Tích hợp Addressable assets** để tải âm thanh hiệu quả và quản lý bộ nhớ tốt hơn
- **Pool âm thanh có thể tùy chỉnh** để tránh các vấn đề về hiệu suất
- **Điều khiển fade in/out** cho các chuyển đổi âm thanh mượt mà
- **Trộn âm thanh tự động** với AudioMixerGroups được cấu hình đúng cách

Gói này xử lý các phức tạp kỹ thuật của việc quản lý âm thanh trong Unity, cho phép bạn tập trung vào việc tạo trải nghiệm âm thanh tuyệt vời cho người chơi.

## Cài Đặt

### Yêu Cầu
- Unity 2019.4 hoặc mới hơn
- Gói Addressables

## Thiết Lập

### Cửa Sổ Thiết Lập Sound Manager

Sound Manager đi kèm với một cửa sổ trình soạn thảo để giúp bạn thiết lập tất cả các thành phần cần thiết. Để truy cập:

1. Vào menu Unity: `Tools > TripSoft > Sound Manager Setup`
2. Điều này sẽ mở cửa sổ Thiết lập Sound Manager, cung cấp các tùy chọn cấu hình khác nhau

### Tạo Các Tài Nguyên Cần Thiết

Trước khi sử dụng Sound Manager, bạn cần tạo hai tài nguyên thiết yếu:

1. **Tài Nguyên Cài Đặt (Settings Asset)**
   - Trong cửa sổ Thiết lập Sound Manager, nhấp vào "Create Settings Asset"
   - Thao tác này sẽ tạo một tài nguyên SoundManagerSettings tại đường dẫn `Assets/TripSoft/SoundManager/SoundManagerSettings.asset`
   - Tài nguyên này chứa tất cả các cài đặt cấu hình cho Sound Manager

2. **Audio Mixer**
   - Trong cửa sổ Thiết lập Sound Manager, nhấp vào "Create Audio Mixer"
   - Thao tác này sẽ tạo một tài nguyên AudioMixer tại đường dẫn `Assets/TripSoft/SoundManager/MainAudioMixer.mixer`
   - Mixer sẽ được cấu hình với các nhóm cần thiết (Master, Music, VFX, UI) và các tham số được exposed

   **Chi tiết về các tham số được exposed trong AudioMixer:**
   
   AudioMixer sẽ tự động thiết lập và expose các tham số sau đây, cho phép bạn điều khiển âm lượng qua code:
   
   | Tham Số | Tên Exposed | Giá Trị Mặc Định | Mô Tả |
   |---------|-------------|-----------------|-------|
   | MasterVolume | "MasterVolume" | 0dB | Điều khiển âm lượng tổng thể của tất cả các âm thanh |
   | MusicVolume | "MusicVolume" | 0dB | Điều khiển âm lượng cho nhóm nhạc nền |
   | VfxVolume | "VfxVolume" | 0dB | Điều khiển âm lượng cho hiệu ứng âm thanh |
   | UIVolume | "UIVolume" | 0dB | Điều khiển âm lượng cho âm thanh giao diện |
   
   *Lưu ý: Các giá trị này được cài đặt ở đơn vị decibel (dB) trong AudioMixer. Giá trị 0dB tương đương với âm lượng không thay đổi. Trong code, khi bạn sử dụng phương thức `SetVolume()`, âm lượng được chuyển đổi tự động từ thang 0-1 sang thang logarit decibel cần thiết cho AudioMixer.*
   
   Quá trình expose tham số được thực hiện bởi phương thức `ExposeMixerParameters()` trong `AudioMixerSetup.cs`:
   ```csharp
   private static void ExposeMixerParameters(AudioMixer mixer)
   {
       SerializedObject serializedObject = new SerializedObject(mixer);
       SerializedProperty exposedParams = serializedObject.FindProperty("exposedParameters");
       
       if (exposedParams == null)
       {
           Debug.LogError("Không thể tìm thấy thuộc tính 'exposedParameters' trong AudioMixer.");
           return;
       }
       
       AddExposedParameter(exposedParams, MasterVolumeParam); // "MasterVolume"
       AddExposedParameter(exposedParams, MusicVolumeParam);  // "MusicVolume"
       AddExposedParameter(exposedParams, VfxVolumeParam);    // "VfxVolume"
       AddExposedParameter(exposedParams, UIVolumeParam);     // "UIVolume"
       
       serializedObject.ApplyModifiedProperties();
   }
   ```

### Thiết Lập Addressables

Sound Manager sử dụng Hệ thống Addressable Asset của Unity để tải và quản lý các clip âm thanh một cách hiệu quả. Làm theo các bước sau để thiết lập các tệp âm thanh của bạn:

1. Tổ chức các tệp âm thanh của bạn trong dự án Unity
2. Đặt chúng vào các thư mục sau (sẽ được tạo nếu chúng không tồn tại):
   - Nhạc nền: `Assets/TripSoft/SoundManager/Music/`
   - Hiệu ứng âm thanh: `Assets/TripSoft/SoundManager/Vfx/`
   - Âm thanh giao diện: `Assets/TripSoft/SoundManager/UI/`
3. Trong cửa sổ Thiết lập Sound Manager, nhấp vào "Scan and Configure Addressables"
4. Thao tác này sẽ:
   - Thiết lập các nhóm addressable cho các tệp âm thanh của bạn
   - Tạo các mục addressable cho từng tệp âm thanh với các nhãn thích hợp
   - Tạo một tệp SoundKeys.cs với các hằng số để truy cập dễ dàng các tệp âm thanh của bạn

### Tạo GameObject SoundManager

Cuối cùng, bạn cần thêm thành phần SoundManager vào scene của mình:

1. Trong cửa sổ Thiết lập Sound Manager, nhấp vào "Create SoundManager GameObject"
2. Thao tác này sẽ tạo một GameObject với thành phần SoundManager được gắn vào
3. SoundManager được cấu hình như một singleton và sẽ tồn tại giữa các lần tải scene

> **Lưu ý**: Hãy đảm bảo bao gồm SoundManager trong scene đầu tiên/khởi tạo của bạn để đảm bảo chức năng âm thanh xuyên suốt game của bạn.

## Tổ Chức Tệp Âm Thanh

Sound Manager sử dụng một hệ thống dựa trên kênh để tổ chức và kiểm soát âm thanh:

- **Nhạc nền (Music)**: Nhạc nền, các bản nhạc môi trường, v.v.
- **Hiệu ứng âm thanh (VFX)**: Hiệu ứng âm thanh cho các sự kiện trong gameplay, môi trường, v.v.
- **Giao diện (UI)**: Âm thanh giao diện, tiếng nhấp chuột, v.v.

Mỗi kênh có điều khiển âm lượng riêng và có thể được quản lý độc lập.

Để thêm âm thanh vào dự án của bạn:

1. Đặt các tệp âm thanh vào các thư mục thích hợp:
   - `Assets/TripSoft/SoundManager/Music/`
   - `Assets/TripSoft/SoundManager/Vfx/`
   - `Assets/TripSoft/SoundManager/UI/`
2. Chạy "Scan and Configure Addressables" trong cửa sổ Thiết lập Sound Manager
3. Âm thanh của bạn sẽ khả dụng thông qua các hằng số được tạo trong `SoundKeys.cs`

## Cách Sử Dụng

### Phát Âm Thanh

Để phát âm thanh trong các script của bạn, trước tiên hãy tham chiếu tới namespace SoundManager:

```csharp
using com.thelegends.sound.manager;
```

#### Phát Âm Thanh Cơ Bản

```csharp
// Phát hiệu ứng âm thanh một lần (fire and forget)
SoundManager.Instance.PlayOneShot(SoundKeys.VFX_EXPLOSION);

// Phát âm thanh với điều khiển âm lượng
SoundManager.Instance.PlayOneShot(SoundKeys.UI_BUTTON_CLICK, 0.5f);

// Phát âm thanh và lấy ID để điều khiển sau này
string soundId = SoundManager.Instance.Play(SoundKeys.VFX_AMBIENT_WIND);

// Phát âm thanh lặp lại
string loopingSoundId = SoundManager.Instance.Play(SoundKeys.VFX_ENGINE_IDLE, 1f, true);
```

#### Phát Nhạc Nền

```csharp
// Phát nhạc nền (tự động dừng nhạc nền trước đó)
SoundManager.Instance.Play(SoundKeys.MUSIC_MAIN_THEME, 0.7f, true);

// Phát với hiệu ứng fade-in (3 giây)
SoundManager.Instance.Play(SoundKeys.MUSIC_BATTLE, 1f, true, 3.0f);

// Phát với callback hoàn thành
SoundManager.Instance.Play(SoundKeys.MUSIC_VICTORY, 1f, false, 0f, () => {
    Debug.Log("Nhạc chiến thắng đã kết thúc!");
});
```

### Điều Khiển Việc Phát

Sử dụng ID âm thanh được trả về từ phương thức Play để điều khiển việc phát:

```csharp
// Phát âm thanh và lưu ID
string soundId = SoundManager.Instance.Play(SoundKeys.VFX_ALARM);

// Tạm dừng âm thanh
SoundManager.Instance.Pause(soundId);

// Tiếp tục phát âm thanh
SoundManager.Instance.Resume(soundId);

// Dừng âm thanh
SoundManager.Instance.Stop(soundId);

// Kiểm tra xem âm thanh có đang phát không
if (SoundManager.Instance.IsPlaying(soundId)) {
    Debug.Log("Âm thanh vẫn đang phát");
}
```

#### Trường Hợp Đặc Biệt: Điều Khiển Nhạc Nền

Bạn có thể sử dụng một ID đặc biệt để điều khiển bản nhạc đang phát hiện tại:

```csharp
// Tạm dừng bản nhạc hiện tại
SoundManager.Instance.Pause(SoundKeys.MUSIC);

// Tiếp tục phát bản nhạc hiện tại
SoundManager.Instance.Resume(SoundKeys.MUSIC);

// Dừng bản nhạc hiện tại
SoundManager.Instance.Stop(SoundKeys.MUSIC);
```

### Điều Chỉnh Âm Lượng

#### Âm Lượng Kênh

Điều chỉnh âm lượng của toàn bộ các kênh âm thanh:

```csharp
// Đặt âm lượng tổng (ảnh hưởng đến tất cả âm thanh)
SoundManager.Instance.SetVolume(AudioChannelType.Master, 0.8f);

// Đặt âm lượng nhạc nền
SoundManager.Instance.SetVolume(AudioChannelType.Music, 0.5f);

// Đặt âm lượng hiệu ứng âm thanh
SoundManager.Instance.SetVolume(AudioChannelType.Vfx, 0.7f);

// Đặt âm lượng âm thanh giao diện
SoundManager.Instance.SetVolume(AudioChannelType.UI, 0.6f);

// Lấy âm lượng hiện tại của một kênh
float musicVolume = SoundManager.Instance.GetVolume(AudioChannelType.Music);

// Bật/tắt chế độ tắt âm cho một kênh
bool isMuted = SoundManager.Instance.ToggleMute(AudioChannelType.Music);
```

#### Âm Lượng Âm Thanh Riêng Lẻ

Điều chỉnh âm lượng của các đối tượng âm thanh cụ thể:

```csharp
string soundId = SoundManager.Instance.Play(SoundKeys.VFX_AMBIENT_RAIN);

// Điều chỉnh âm lượng của âm thanh đang phát
SoundManager.Instance.SetSoundVolume(soundId, 0.3f);
```

### Hiệu Ứng Fade In/Out

Tạo các chuyển đổi mượt mà với hiệu ứng fade:

```csharp
// Phát với fade in
string soundId = SoundManager.Instance.Play(SoundKeys.MUSIC_LEVEL_START, 1f, true, 2.5f);

// Fade in một âm thanh đang phát
SoundManager.Instance.FadeIn(soundId, 3.0f, 0.8f);

// Fade out một âm thanh (không dừng)
SoundManager.Instance.FadeOut(soundId, 2.0f, false);

// Fade out một âm thanh và dừng sau khi fade hoàn tất
SoundManager.Instance.FadeOut(soundId, 1.5f, true);
```

### Dừng Nhiều Âm Thanh

```csharp
// Dừng tất cả âm thanh trong một kênh cụ thể
SoundManager.Instance.StopChannel(AudioChannelType.Vfx);

// Dừng tất cả âm thanh trong tất cả các kênh
SoundManager.Instance.StopAll();
```

## Tính Năng Nâng Cao

### Hệ Thống Sound ID

Sound Manager sử dụng hệ thống ID duy nhất để theo dõi và kiểm soát từng đối tượng âm thanh:

- Âm thanh không phải nhạc nền sẽ nhận một ID duy nhất mỗi khi chúng được phát
- Bản nhạc nền hiện tại luôn có thể truy cập thông qua ID cố định `SoundKeys.MUSIC`
- ID có thể được lưu trữ và sử dụng để điều khiển âm thanh trong suốt vòng đời của chúng

### Sự Kiện

Đăng ký sự kiện để theo dõi hoạt động âm thanh:

```csharp
// Đăng ký sự kiện khi âm thanh bắt đầu
SoundManager.Instance.OnSoundStarted += HandleSoundStarted;

// Đăng ký sự kiện khi âm thanh kết thúc
SoundManager.Instance.OnSoundStopped += HandleSoundStopped;

// Xử lý sự kiện
private void HandleSoundStarted(string soundId, AudioChannelType channelType) {
    Debug.Log($"Âm thanh bắt đầu: {soundId} trên kênh {channelType}");
}

private void HandleSoundStopped(string soundId, AudioChannelType channelType) {
    Debug.Log($"Âm thanh kết thúc: {soundId} trên kênh {channelType}");
}
```

### Cài Đặt Hiệu Suất

Tài nguyên SoundManagerSettings chứa một số cài đặt liên quan đến hiệu suất:

- **Initial Pool Size**: Số lượng trình phát âm thanh được tạo sẵn lúc khởi động
- **Max Concurrent Sounds**: Số lượng âm thanh tối đa có thể phát đồng thời
- **Preload Addressables**: Liệu có nên tải trước các clip âm thanh lúc khởi động để phát nhanh hơn hay không

Bạn có thể điều chỉnh các cài đặt này trong Inspector cho tài nguyên SoundManagerSettings.

## Xử Lý Sự Cố

### Vấn Đề Thường Gặp

1. **Không Phát Âm Thanh**
   - Kiểm tra xem GameObject SoundManager có tồn tại trong scene không
   - Xác minh rằng tài nguyên AudioMixer và Settings được gán đúng cách
   - Kiểm tra xem mức âm lượng có bị đặt về 0 hoặc bị tắt tiếng không

2. **Không Tìm Thấy Sound Keys**
   - Đảm bảo bạn đã chạy "Scan and Configure Addressables" sau khi thêm các tệp âm thanh mới
   - Kiểm tra xem các tệp âm thanh có được đặt trong cấu trúc thư mục đúng không

3. **Lỗi: SoundManager chưa được khởi tạo**
   - Đảm bảo SoundManager được khởi tạo trong scene đầu tiên của game
   - Kiểm tra các vấn đề về thứ tự thực thi script

### Ghi Log Gỡ Lỗi

Bạn có thể bật ghi log gỡ lỗi trong cài đặt SoundManager để có thêm thông tin về các hoạt động âm thanh:

1. Chọn tài nguyên SoundManagerSettings trong cửa sổ Project
2. Đánh dấu tùy chọn "Debug Logging" trong Inspector
3. Chạy game và kiểm tra Console để biết thông tin log chi tiết

## Tài Liệu Tham Khảo API

### Các Phương Thức Cốt Lõi

```csharp
// Phương thức phát
string Play(string soundKey, float volume = 1f, bool loop = false, float fadeInDuration = 0f, Action onComplete = null);
void PlayOneShot(string soundKey, float volume = 1f);

// Điều khiển phát
void Pause(string soundId);
void Resume(string soundId);
void Stop(string soundId);
bool IsPlaying(string soundId);

// Điều khiển âm lượng
void SetVolume(AudioChannelType channelType, float volume);
float GetVolume(AudioChannelType channelType);
bool ToggleMute(AudioChannelType channelType);
void SetSoundVolume(string soundId, float volume);

// Hiệu ứng fade
void FadeIn(string soundId, float duration, float targetVolume = 1f);
void FadeOut(string soundId, float duration, bool stopAfterFade = true);

// Điều khiển nhiều âm thanh
void StopChannel(AudioChannelType channelType);
void StopAll();
```

### Sự Kiện

```csharp
event Action<string, AudioChannelType> OnSoundStarted;
event Action<string, AudioChannelType> OnSoundStopped;
```

### Enums

```csharp
// Các loại kênh âm thanh
public enum AudioChannelType
{
    Master,
    Music,
    Vfx,
    UI
}
```

---

Để biết thêm thông tin hoặc được hỗ trợ, vui lòng liên hệ với chúng tôi theo địa chỉ support@example.com hoặc truy cập trang web của chúng tôi tại https://example.com