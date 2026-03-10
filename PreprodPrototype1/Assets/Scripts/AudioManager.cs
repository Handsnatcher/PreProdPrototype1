using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private AudioSource source;
    public AudioClip soundEffect;

    void Start()
    {
        //get source attached to game object
        source = GetComponent<AudioSource>();
    }

    public void PlaySoundEffect()
    {
        if (source != null && soundEffect != null)
        {
            source.PlayOneShot(soundEffect);
        }
    }

}
