using System.Collections;
using System.Collections.Generic;
using com.thelegends.sound.manager;
using UnityEngine;

public class Test : MonoBehaviour
{
    public void PlayMusic() {
        SoundManager.Instance.Play(SoundKeys.MUSIC_GAMEPLAYMUSIC, 1, true, 3);
    }

    public void PlayMainMenuMusic() {
        SoundManager.Instance.Play(SoundKeys.MUSIC_MAINMENU, 1, true, 3);
    }

    public void PlaySfx1() {
        SoundManager.Instance.PlayOneShot(SoundKeys.VFX_PUNCHSFX);
    }

    public void PlaySfx2() {
        SoundManager.Instance.PlayOneShot(SoundKeys.UI_BUTTON_CLICK);
    }

    public void PauseMUsic() {
        SoundManager.Instance.Pause(SoundKeys.MUSIC);  // Sử dụng hằng số MUSIC
    }
    public void ResumeMusic() {
        SoundManager.Instance.Resume(SoundKeys.MUSIC); // Sử dụng hằng số MUSIC
    }
    public void StopMusic() {
        SoundManager.Instance.Stop(SoundKeys.MUSIC);   // Sử dụng hằng số MUSIC
    }
}
