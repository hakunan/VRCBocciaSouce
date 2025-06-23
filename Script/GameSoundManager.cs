
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
public enum SeKey
{
    Pop,
    DrumRoll,
    Additional,
    Start
}

public class GameSoundManager : UdonSharpBehaviour
{
    [SerializeField] AudioSource seAudioSouce;
    [SerializeField] AudioClip[] seAudioSouceClips;

    public void PlaySeByInt(int id)
    {
        seAudioSouce.clip = seAudioSouceClips[id];
        seAudioSouce.Play();
    }
}
