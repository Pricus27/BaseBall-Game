using UnityEngine;

namespace BaseballTyping
{
    public sealed class AudioHub : MonoBehaviour
    {
        [Header("Optional clips")]
        [SerializeField] private AudioClip pitchClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip homeRunClip;
        [SerializeField] private AudioClip strikeClip;
        [SerializeField] private AudioClip foulClip;

        private AudioSource source;
        private float volume = 0.7f;

        public int VolumePercent => Mathf.RoundToInt(volume * 100f);

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            pitchClip ??= Resources.Load<AudioClip>("Audio/Pitch");
            hitClip ??= Resources.Load<AudioClip>("Audio/Hit");
            homeRunClip ??= Resources.Load<AudioClip>("Audio/HomeRun");
            strikeClip ??= Resources.Load<AudioClip>("Audio/Strike");
            foulClip ??= Resources.Load<AudioClip>("Audio/Foul");
            volume = PlayerPrefs.GetFloat("BaseballTyping.Volume", 0.7f);
            ApplyVolume();
        }

        public void PlayPitch() => Play(pitchClip);
        public void PlayHit(bool homeRun) => Play(homeRun ? homeRunClip : hitClip);
        public void PlayStrike() => Play(strikeClip);
        public void PlayFoul() => Play(foulClip);

        public void AdjustVolume(float amount)
        {
            volume = Mathf.Clamp01(volume + amount);
            PlayerPrefs.SetFloat("BaseballTyping.Volume", volume);
            PlayerPrefs.Save();
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            AudioListener.volume = volume;
            if (source != null) source.volume = volume;
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null) source.PlayOneShot(clip);
        }
    }
}
