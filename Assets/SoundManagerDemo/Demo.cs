using UnityEngine;
using UnityEngine.UI;
using com.thelegends.sound.manager;
using System.Collections;

/// <summary>
/// Demo class that demonstrates how to use the SoundManager with UI controls.
/// Handles the interaction between audio sliders, mute toggles, and the SoundManager.
/// </summary>
public class Demo : MonoBehaviour
{
    /// <summary>
    /// Plays the gameplay background music
    /// </summary>
    public void PlayMusic() {
        SoundManager.Instance.Play(SoundKeys.MUSIC_GAMEPLAYMUSIC, 1, true, 3);
    }

    /// <summary>
    /// Plays the main menu background music
    /// </summary>
    public void PlayMainMenuMusic() {
        SoundManager.Instance.Play(SoundKeys.MUSIC_MAINMENU, 1, true, 3);
    }

    /// <summary>
    /// Plays the first sound effect (punch SFX)
    /// </summary>
    public void PlaySfx1() {
        SoundManager.Instance.PlayOneShot(SoundKeys.VFX_PUNCHSFX);
    }

    /// <summary>
    /// Plays the second sound effect (UI button click)
    /// </summary>
    public void PlaySfx2() {
        SoundManager.Instance.PlayOneShot(SoundKeys.UI_BUTTON_CLICK);
    }

    /// <summary>
    /// Pauses all music tracks
    /// </summary>
    public void PauseMusic() {
        SoundManager.Instance.Pause(SoundKeys.MUSIC);  // Using MUSIC constant
    }

    /// <summary>
    /// Resumes all paused music tracks
    /// </summary>
    public void ResumeMusic() {
        SoundManager.Instance.Resume(SoundKeys.MUSIC); // Using MUSIC constant
    }

    /// <summary>
    /// Stops all music tracks
    /// </summary>
    public void StopMusic() {
        SoundManager.Instance.Stop(SoundKeys.MUSIC);   // Using MUSIC constant
    }
}