using System.Collections;
using System.Collections.Generic;
using com.thelegends.sound.manager;
using UnityEngine;

public class Test : MonoBehaviour
{
    public void PlayMusic() {
        SoundManager.Instance.Play("music/GamePlayMusic",1,true,1);
    }

    public void PlaySfx1() {
        SoundManager.Instance.PlayOneShot("vfx/PunchSFX");   
        SoundManager.Instance.PlayOneShot("vfx/PunchSFX");   
        SoundManager.Instance.PlayOneShot("vfx/PunchSFX");   
        SoundManager.Instance.PlayOneShot("vfx/PunchSFX");   
        SoundManager.Instance.PlayOneShot("vfx/PunchSFX");   

    }

    public void PlaySfx2() {
        SoundManager.Instance.PlayOneShot("ui/Button_Click");
    }

    public void PauseMUsic() {
        SoundManager.Instance.Pause("music/GamePlayMusic");
    }
    public void ResumeMusic() {
        SoundManager.Instance.Resume("music/GamePlayMusic");
    }
    public void StopMusic() {
        SoundManager.Instance.Stop("music/GamePlayMusic");
    }
}
