using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MemePlayer : MonoBehaviour
{
    [Header("References")]
    public RawImage display;
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    RawImage _currentVideoSlot;
    RenderTexture _frozenFrame;
    bool _hasFrozenFrame;
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

    void FreezeFrame()
    {
        if (_rt == null || !_rt.IsCreated()) return;

        // Allocate on first use
        if (_frozenFrame == null)
        {
            _frozenFrame = new RenderTexture(_rt.width, _rt.height, 0, _rt.format);
            _frozenFrame.Create();
        }

        // Blit the live _rt into the frozen RenderTexture
        Graphics.Blit(_rt, _frozenFrame);
        _hasFrozenFrame = true;
    }

    /// Routes video to target slot display with optional sound.
    /// Freezes the previous slot's last frame so both slots show different content.
    public void PlaySlot(RawImage targetSlot, Reel reel, bool withSound)
    {
        if (reel?.memeData == null) return;

        if (reel.memeData.memeVideo != null)
        {
            // Freeze current frame for previous slot before switching video
            if (_currentVideoSlot != null && _currentVideoSlot != targetSlot)
            {
                FreezeFrame();
                if (_hasFrozenFrame)
                    _currentVideoSlot.texture = _frozenFrame;
            }
            _currentVideoSlot = targetSlot;

            videoPlayer.clip = reel.memeData.memeVideo;
            videoPlayer.isLooping = false;
            videoPlayer.Stop();
            videoPlayer.time = 0;
            audioSource.volume = withSound ? 1f : 0f;
            videoPlayer.Play();
            if (targetSlot != null) targetSlot.texture = _rt;
        }
        else if (reel.memeData.memeImage != null)
        {
            if (_currentVideoSlot == targetSlot)
                _currentVideoSlot = null;
            if (targetSlot != null) targetSlot.texture = reel.memeData.memeImage;
            if (withSound && reel.memeData.memeSound != null)
            {
                audioSource.volume = 1f;
                audioSource.PlayOneShot(reel.memeData.memeSound);
            }
        }
    }

    /// Plays video (muted) on hover target — only if no slot is actively using the VideoPlayer.
    public void PlayHover(RawImage target, Reel reel)
    {
        if (reel?.memeData == null || target == null) return;

        // Static image fallback
        if (reel.memeData.memeImage != null)
        {
            target.texture = reel.memeData.memeImage;
            return;
        }

        if (reel.memeData.memeVideo != null)
        {
            // If VideoPlayer already has this reel's clip loaded (slot or previous hover), share texture
            if (videoPlayer.clip == reel.memeData.memeVideo && _rt != null)
            {
                target.texture = _rt;
                return;
            }

            // Play muted only if no slot is using the VideoPlayer
            if (_currentVideoSlot == null)
            {
                videoPlayer.clip = reel.memeData.memeVideo;
                videoPlayer.isLooping = true;
                videoPlayer.time = 0;
                audioSource.volume = 0f;
                videoPlayer.Play();
                target.texture = _rt;
            }
        }
    }

    /// Stops hover video — skips if a slot is actively using the VideoPlayer.
    public void StopHover()
    {
        if (_currentVideoSlot != null) return;
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
    }

    /// Replays just the sound for a reel (on slot click).
    public void PlaySound(Reel reel)
    {
        if (reel?.memeData?.memeSound != null)
        {
            audioSource.volume = 1f;
            audioSource.PlayOneShot(reel.memeData.memeSound);
        }
    }

    public void Stop()
    {
        _currentVideoSlot = null;
        _hasFrozenFrame = false;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
        if (display != null) display.texture = null;
    }

    void OnDestroy()
    {
        if (_frozenFrame != null)
        {
            _frozenFrame.Release();
            Destroy(_frozenFrame);
        }
    }
}
