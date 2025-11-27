using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;

    [Header("Audio Sources")]
    public AudioSource effectSource;
    public AudioSource distanceSource;
    public AudioSource musicSource;
    public AudioSource uiSource;

    [Header("UI Clips")]
    public AudioClip point;
    public AudioClip click;
    public AudioClip typing;

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
    public AudioClip shake;
    public AudioClip sleep;
    public AudioClip drown;

    [Header("Environment Clips")]
    public AudioClip vine;
    public AudioClip walldestroy;
    public AudioClip earthquake;

    [Header("Enemies Clips")]
    public AudioClip walkramble;
    public AudioClip jumpramble;
    public AudioClip fallramble;
    public AudioClip rollramble;
    public AudioClip ongroundlumerin;
    public AudioClip swimlumerin;
    public AudioClip boostlumerin;

    [Header("Music Clips")]
    public AudioClip music1;

    [Header("Boss Clips")]
    public AudioClip bossArmSpawn;
    public AudioClip bossArmMove;
    public AudioClip bossArmSlam;
    public AudioClip bossMagicFly;
    public AudioClip bossMagicImpact;
    public AudioClip bossDie;
    public AudioClip bossArmSweep;

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
            VolumeManager.instance.ApplyVolumeSettings();
    }

    public void PlaySFX(string sfxName)
    {
        switch (sfxName)
        {
            //-----------------------------UI-----------------------------
            case "Point": uiSource.PlayOneShot(point); break;
            case "Click": uiSource.PlayOneShot(click); break;
            case "Typing": uiSource.PlayOneShot(typing); break;

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
            case "Shake": effectSource.PlayOneShot(shake); break;
            case "Sleep": effectSource.PlayOneShot(sleep); break;
            case "Drown": effectSource.PlayOneShot(drown); break;

            //------------------------Environment-------------------------
            case "Vine": distanceSource.PlayOneShot(vine); break;
            case "WallDestroy": effectSource.PlayOneShot(walldestroy); break;
            case "Earthquake": effectSource.PlayOneShot(earthquake); break;

            //--------------------------Enemies---------------------------
            case "WalkRamble": effectSource.PlayOneShot(walkramble); break;
            case "JumpRamble": effectSource.PlayOneShot(jumpramble); break;
            case "FallRamble": effectSource.PlayOneShot(fallramble); break;
            case "RollRamble": effectSource.PlayOneShot(rollramble); break;
            case "OnGroundLumerin": effectSource.PlayOneShot(ongroundlumerin); break;
            case "SwimLumerin": effectSource.PlayOneShot(swimlumerin); break;
            case "BoostLumerin": effectSource.PlayOneShot(boostlumerin); break;

            //---------------------------Music----------------------------
            case "Music1": musicSource.PlayOneShot(music1); break;

            //---------------------------Boss-----------------------------
            case "BossArmSpawn":
                if (bossArmSpawn != null) effectSource.PlayOneShot(bossArmSpawn);
                break;
            case "BossArmMove":
                if (bossArmMove != null) effectSource.PlayOneShot(bossArmMove);
                break;
            case "BossArmSlam":
                if (bossArmSlam != null) effectSource.PlayOneShot(bossArmSlam);
                break;
            case "BossMagicFly":
                if (bossMagicFly != null) effectSource.PlayOneShot(bossMagicFly);
                break;
            case "BossMagicImpact":
                if (bossMagicImpact != null) effectSource.PlayOneShot(bossMagicImpact);
                break;
            case "BossDie":
                if (bossDie != null) effectSource.PlayOneShot(bossDie);
                break;
            case "BossArmSweep":
                if (bossArmSweep != null) effectSource.PlayOneShot(bossArmSweep);
                break;

            default:
                break;
        }
    }

    public void MuteAll(bool mute)
    {
        if (effectSource != null) effectSource.mute = mute;
        if (musicSource != null) musicSource.mute = mute;
        if (distanceSource != null) distanceSource.mute = mute;
    }
}