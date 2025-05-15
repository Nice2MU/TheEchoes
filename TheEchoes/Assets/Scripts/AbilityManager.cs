using UnityEngine;

public interface AbilityManager
{
    void OnAbilityEnter(GameObject player);
    void OnAbilityExit(GameObject player);
    void PlayRevertAnimation(GameObject player);
}