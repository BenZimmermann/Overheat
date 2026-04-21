using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// this script is responsible for managing all sound effects and music in the game. It uses a singleton pattern to allow easy access from other scripts. It handles playing UI sounds, 
/// 3D sound effects, and background music based on the current scene. It also allows for volume control and ensures that music transitions smoothly when changing scenes.
/// </summary>
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
// This struct represents a sound effect, containing its name, audio clip, and volume scale for adjusting the sound's loudness when played.
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
    //checks the name of the loaded scene and decides whether to play the menu music or continue playing game tracks based on the current track index.
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
    //music playback, if the slider value is very low, it pauses the music, otherwise it unpauses it if it's not already playing and has a clip assigned.
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
    //plays a 2D sound effect.
    public void PlaySound(SoundType type, float globalVolume = 1f)
    {
        int index = (int)type;
        // Check if the index is within bounds and the clip is assigned before attempting to play the sound
        if (index < _sfxEffects.Length && _sfxEffects[index].clip != null)
        {
         
            float finalVolume = _sfxEffects[index].volumeScale * globalVolume;
            _uiSource.PlayOneShot(_sfxEffects[index].clip, finalVolume);
        }
    }
    //plays 3D sound effects at a specific position in the game world, creating a temporary GameObject with an AudioSource component to play the sound and then destroying it after the clip finishes playing.
    public void Play3DSound(SoundType type, Vector3 position, float globalVolume = 1f)
    {
        int index = (int)type;
        if (index >= _sfxEffects.Length || _sfxEffects[index].clip == null) return;

        GameObject temp = new GameObject("3D_SFX_" + type);
        temp.transform.position = position;
        AudioSource s = temp.AddComponent<AudioSource>();

        // Configure the AudioSource for 3D sound
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