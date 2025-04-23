using UnityEngine;

public interface IMorphAbility
{
    void OnMorphEnter(GameObject player);
    void OnMorphExit(GameObject player);
    void PlayRevertAnimation(GameObject player);
}