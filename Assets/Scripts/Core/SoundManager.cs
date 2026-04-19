using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum SoundType
{
    UIClick,
    Shoot,
    Melee,
    Reload,
    DamagePlayer,
    DamageEnemy,
    DestroyBox,
    EnemyDeath,
    PlayerDeath,
    BuyItem,
    UseItem,
    CollectMoney,
    PortalWarp,
    Exposion,
    Slide,
    Dash,
    Footstep
}
[System.Serializable]
public struct SoundEffect
{
    public string name; 
    public AudioClip clip;
    [Range(0f, 1f)] public float volumeScale; 
}
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Mixer & Groups")]
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private AudioMixerGroup _sfxGroup;
    [SerializeField] private AudioMixerGroup _musicGroup;

    [Header("Clips")]
    [SerializeField] private SoundEffect[] _sfxEffects;

    [Header("Music Setup")]
    [SerializeField] private string _menuSceneName = "StartScreen";
    [SerializeField] private AudioClip _menuMusic;
    [SerializeField] private AudioClip[] _gamePlaylist;

    private AudioSource _musicSource;
    private AudioSource _uiSource;
    private int _currentGameTrackIndex = -1;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.outputAudioMixerGroup = _musicGroup;
        _musicSource.loop = false;

        _uiSource = gameObject.AddComponent<AudioSource>();
        _uiSource.outputAudioMixerGroup = _sfxGroup;
    }
    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void Update()
    {
        if (!_musicSource.isPlaying && SceneManager.GetActiveScene().name != _menuSceneName)
        {
            if (_musicSource.time == 0)
            {
                PlayNextGameTrack();
            }
        }
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == _menuSceneName)
        {
            PlayMenuMusic();
        }
        else
        {
            if (_currentGameTrackIndex == -1)
                PlayNextGameTrack();
        }
    }
    private void PlayMenuMusic()
    {
        _currentGameTrackIndex = -1;
        if (_musicSource.clip == _menuMusic) return;

        _musicSource.clip = _menuMusic;
        _musicSource.loop = true;
        _musicSource.Play();
    }

    private void PlayNextGameTrack()
    {
        if (_gamePlaylist.Length == 0) return;

        _currentGameTrackIndex = (_currentGameTrackIndex + 1) % _gamePlaylist.Length;
        _musicSource.clip = _gamePlaylist[_currentGameTrackIndex];
        _musicSource.loop = false;
        _musicSource.Play();
    }

    public void UpdateMusicStatus(float sliderValue)
    {
        if (sliderValue <= 0.001f)
        {
            if (_musicSource.isPlaying) _musicSource.Pause();
        }
        else
        {
            if (!_musicSource.isPlaying && _musicSource.clip != null) _musicSource.UnPause();
        }
    }
    public void PlaySound(SoundType type, float globalVolume = 1f)
    {
        int index = (int)type;
        if (index < _sfxEffects.Length && _sfxEffects[index].clip != null)
        {
         
            float finalVolume = _sfxEffects[index].volumeScale * globalVolume;
            _uiSource.PlayOneShot(_sfxEffects[index].clip, finalVolume);
        }
    }

    public void Play3DSound(SoundType type, Vector3 position, float globalVolume = 1f)
    {
        int index = (int)type;
        if (index >= _sfxEffects.Length || _sfxEffects[index].clip == null) return;

        GameObject temp = new GameObject("3D_SFX_" + type);
        temp.transform.position = position;
        AudioSource s = temp.AddComponent<AudioSource>();

        s.clip = _sfxEffects[index].clip;
        s.outputAudioMixerGroup = _sfxGroup;
        s.spatialBlend = 1f;
        s.minDistance = 1f;
        s.maxDistance = 20f;

        s.volume = _sfxEffects[index].volumeScale * globalVolume;

        s.Play();
        Destroy(temp, _sfxEffects[index].clip.length);
    }
}