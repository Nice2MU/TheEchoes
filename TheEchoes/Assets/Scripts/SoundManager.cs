using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource effectSource;
    public AudioSource distanceSource;
    public AudioSource musicSource;

    [Header("UI Clips")]
    public AudioClip start;
    public AudioClip point;
    public AudioClip click;
    public AudioClip typing;
    public AudioClip locks;
    public AudioClip unlock;
    public AudioClip save;
    public AudioClip delete;

    [Header("Action Clips")]
    public AudioClip walk;
    public AudioClip jump;
    public AudioClip sjump;
    public AudioClip climb;
    public AudioClip grab;
    public AudioClip fall;
    public AudioClip consume;
    public AudioClip transforms;
    public AudioClip roll;
    public AudioClip shock;
    public AudioClip shake;
    public AudioClip sleep;
    public AudioClip drown;

    [Header("Environment Clips")]
    public AudioClip grass;
    public AudioClip wind;
    public AudioClip cave;
    public AudioClip water;
    public AudioClip drippingwater;
    public AudioClip firefly;
    public AudioClip vine;
    public AudioClip walldestroy;

    [Header("Enemies Clips")]
    public AudioClip anon;
    public AudioClip demon;
    public AudioClip walkramble;
    public AudioClip jumpramble;
    public AudioClip fallramble;
    public AudioClip rollramble;
    public AudioClip ongroundlumerin;
    public AudioClip swimlumerin;
    public AudioClip boostlumerin;

    [Header("Music Clips")]
    public AudioClip music1;
    public AudioClip music2;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        else Destroy(gameObject);
    }

    void Start()
    {
        if (VolumeManager.instance != null)
        {
            VolumeManager.instance.ApplyVolumeSettings();
        }
    }

    public void PlaySFX(string sfxName)
    {
        switch (sfxName)
        {
            //-----------------------------UI-----------------------------
            case "Start": effectSource.PlayOneShot(start); break;

            case "Point": effectSource.PlayOneShot(point); break;

            case "Click": effectSource.PlayOneShot(click); break;

            case "Typing": effectSource.PlayOneShot(typing); break;

            case "Lock": effectSource.PlayOneShot(locks); break;

            case "Unlock": effectSource.PlayOneShot(unlock); break;

            case "Save": effectSource.PlayOneShot(save); break;

            case "Delete": effectSource.PlayOneShot(delete); break;

            //---------------------------Action---------------------------
            case "Walk": effectSource.PlayOneShot(walk); break;

            case "Jump": effectSource.PlayOneShot(jump); break;

            case "SJump": effectSource.PlayOneShot(sjump); break;

            case "Climb": effectSource.PlayOneShot(climb); break;

            case "Grab": effectSource.PlayOneShot(grab); break;

            case "Fall": effectSource.PlayOneShot(fall); break;

            case "Consume": effectSource.PlayOneShot(consume); break;

            case "Transform": effectSource.PlayOneShot(transforms); break;

            case "Roll": effectSource.PlayOneShot(roll); break;

            case "Shock": effectSource.PlayOneShot(shock); break;

            case "Shake": effectSource.PlayOneShot(shake); break;

            case "Sleep": effectSource.PlayOneShot(sleep); break;

            case "Drown": effectSource.PlayOneShot(drown); break;

            //------------------------Environment-------------------------
            case "Grass": effectSource.PlayOneShot(grass); break;

            case "Wind": effectSource.PlayOneShot(wind); break;

            case "Cave": effectSource.PlayOneShot(cave); break;

            case "Water": effectSource.PlayOneShot(water); break;

            case "DrippingWater": effectSource.PlayOneShot(drippingwater); break;

            case "Firefly": effectSource.PlayOneShot(firefly); break;

            case "Vine": distanceSource.PlayOneShot(vine); break;

            case "WallDestroy": effectSource.PlayOneShot(walldestroy); break;

            //--------------------------Enemies---------------------------
            case "Anon": effectSource.PlayOneShot(anon); break;

            case "Demon": effectSource.PlayOneShot(demon); break;

            case "WalkRamble": effectSource.PlayOneShot(walkramble); break;

            case "JumpRamble": effectSource.PlayOneShot(jumpramble); break;

            case "FallRamble": effectSource.PlayOneShot(fallramble); break;

            case "RollRamble": effectSource.PlayOneShot(rollramble); break;

            case "OnGroundLumerin": effectSource.PlayOneShot(ongroundlumerin); break;

            case "SwimLumerin": effectSource.PlayOneShot(swimlumerin); break;

            case "BoostLumerin": effectSource.PlayOneShot(boostlumerin); break;

            //---------------------------Music----------------------------
            case "Music1": musicSource.PlayOneShot(music1); break;

            case "Music2": musicSource.PlayOneShot(music2); break;

            default:
                break;
        }
    }
}