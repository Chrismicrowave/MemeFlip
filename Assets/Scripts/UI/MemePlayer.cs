using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MemePlayer : MonoBehaviour
{
    [Header("References")]
    public RawImage display;
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    Reel _currentReel;
    RenderTexture _rt;

    void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (display == null) display = GetComponent<RawImage>();

        if (videoPlayer != null)
        {
            _rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32);
            _rt.Create();
            videoPlayer.targetTexture = _rt;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }
    }

    public void PlayMuted(Reel reel)
    {
        if (reel?.memeData == null) return;
        if (reel.isFaceDown || reel.isDestroyed) return;
        _currentReel = reel;

        if (reel.memeData.memeVideo != null)
        {
            videoPlayer.clip = reel.memeData.memeVideo;
            videoPlayer.isLooping = true;
            videoPlayer.time = 0;
            audioSource.volume = 0f;
            videoPlayer.Play();
            display.texture = _rt;
        }
        else if (reel.memeData.memeImage != null)
        {
            display.texture = reel.memeData.memeImage;
        }
    }

    public void PlayFull(Reel reel)
    {
        if (reel?.memeData == null) return;
        _currentReel = reel;

        if (reel.memeData.memeVideo != null)
        {
            videoPlayer.clip = reel.memeData.memeVideo;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
            videoPlayer.time = 0;
            audioSource.volume = 1f;
            videoPlayer.Play();
            display.texture = _rt;
        }
        else if (reel.memeData.memeImage != null)
        {
            display.texture = reel.memeData.memeImage;

            if (reel.memeData.memeSound != null)
            {
                audioSource.volume = 1f;
                audioSource.PlayOneShot(reel.memeData.memeSound);
            }
        }
    }

    public void Stop()
    {
        _currentReel = null;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();

        if (display != null)
            display.texture = null;
    }
}
