using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MemePlayer : MonoBehaviour
{
    [Header("References")]
    public RawImage display;
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;
    public AudioSource audioSource2;

    RawImage _currentVideoSlot;
    RenderTexture _frozenFrame;
    bool _hasFrozenFrame;
    RenderTexture _rt;
    RawImage _slot1Target;
    RawImage _slot2Target;

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

    /// Routes video to target slot display with per-slot independent audio.
    /// Freezes the previous slot's last frame so both slots show different content.
    public void PlaySlot(RawImage targetSlot, Reel reel, bool withSound)
    {
        if (reel?.memeData == null) return;

        // Track which slot images we've seen
        if (_slot1Target == null) _slot1Target = targetSlot;
        else if (_slot2Target == null && targetSlot != _slot1Target) _slot2Target = targetSlot;

        AudioSource slotAudio = targetSlot == _slot1Target ? audioSource : audioSource2;
        if (slotAudio == null) slotAudio = audioSource;

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
            videoPlayer.Play();
            if (targetSlot != null) targetSlot.texture = _rt;

            // Per-slot audio independent of VideoPlayer (so both slots play simultaneously)
            if (withSound)
            {
                slotAudio.volume = 1f;
                if (reel.memeData.memeSound != null)
                    slotAudio.PlayOneShot(reel.memeData.memeSound);
            }
        }
        else if (reel.memeData.memeImage != null)
        {
            if (_currentVideoSlot == targetSlot)
                _currentVideoSlot = null;
            if (targetSlot != null) targetSlot.texture = reel.memeData.memeImage;
            if (withSound)
            {
                slotAudio.volume = 1f;
                if (reel.memeData.memeSound != null)
                    slotAudio.PlayOneShot(reel.memeData.memeSound);
            }
        }
    }

    /// Plays video (muted) on hover target — shows image if video can't play.
    public void PlayHover(RawImage target, Reel reel)
    {
        if (reel?.memeData == null || target == null) return;

        // Prefer video (muted) for animated preview
        if (reel.memeData.memeVideo != null)
        {
            // Same clip already loaded → share the render texture
            if (videoPlayer.clip == reel.memeData.memeVideo && _rt != null)
            {
                target.texture = _rt;
                return;
            }

            // Play muted if no slot is using the VideoPlayer
            if (_currentVideoSlot == null)
            {
                videoPlayer.clip = reel.memeData.memeVideo;
                videoPlayer.isLooping = true;
                videoPlayer.time = 0;
                audioSource.volume = 0f;
                videoPlayer.Play();
                target.texture = _rt;
                return;
            }
        }

        // Static image fallback (if no video, or slot is using VideoPlayer for another clip)
        if (reel.memeData.memeImage != null)
        {
            target.texture = reel.memeData.memeImage;
        }
    }

    /// Stops hover video — skips if a slot is actively using the VideoPlayer.
    public void StopHover()
    {
        if (_currentVideoSlot != null) return;
        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
    }

    /// Plays sound for a reel on the audio source associated with the given slot.
    /// This avoids restarting video — only the audio is triggered.
    public void PlaySlotSound(RawImage slotImage, Reel reel)
    {
        if (reel?.memeData?.memeSound == null || slotImage == null) return;

        AudioSource src = slotImage == _slot1Target ? audioSource : audioSource2;
        if (src == null) src = audioSource;
        src.volume = 1f;
        src.PlayOneShot(reel.memeData.memeSound);
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
        _slot1Target = null;
        _slot2Target = null;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
        if (audioSource2 != null && audioSource2.isPlaying)
            audioSource2.Stop();
        if (display != null) display.texture = null;
    }

    void OnDestroy()
    {
        if (_rt != null)
        {
            _rt.Release();
            Destroy(_rt);
        }
        if (_frozenFrame != null)
        {
            _frozenFrame.Release();
            Destroy(_frozenFrame);
        }
    }
}
