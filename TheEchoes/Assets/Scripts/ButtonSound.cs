using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private static AudioSource pointSource;
    private static AudioSource clickSource;
    private static bool mixerLinked = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayPointExclusive();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClickExclusive();
    }

    private static void EnsureSources()
    {
        if (pointSource != null && clickSource != null) return;

        var holder = GameObject.Find("__ButtonSoundPool__");
        if (holder == null)
        {
            holder = new GameObject("__ButtonSoundPool__");
            Object.DontDestroyOnLoad(holder);
        }

        if (pointSource == null)
        {
            pointSource = holder.AddComponent<AudioSource>();
            pointSource.playOnAwake = false;
        }

        if (clickSource == null)
        {
            clickSource = holder.AddComponent<AudioSource>();
            clickSource.playOnAwake = false;
        }

        var sm = SoundManager.instance;
        if (sm != null && sm.uiSource != null && !mixerLinked)
        {
            var group = sm.uiSource.outputAudioMixerGroup;
            if (group != null)
            {
                pointSource.outputAudioMixerGroup = group;
                clickSource.outputAudioMixerGroup = group;
            }
            mixerLinked = true;
        }
    }

    private static void PlayPointExclusive()
    {
        var sm = SoundManager.instance;
        if (sm == null || sm.point == null) return;

        EnsureSources();

        pointSource.Stop();
        pointSource.PlayOneShot(sm.point);
    }

    private static void PlayClickExclusive()
    {
        var sm = SoundManager.instance;
        if (sm == null || sm.click == null) return;

        EnsureSources();

        clickSource.Stop();
        clickSource.PlayOneShot(sm.click);
    }
}