using UnityEngine;

public class ObjectsManager : MonoBehaviour
{
    [Header("Objects To Enable")]
    public GameObject[] objectsToEnable;

    [Header("Objects To Disable")]
    public GameObject[] objectsToDisable;

    public void Apply()
    {
        if (objectsToEnable != null)
        {
            foreach (var obj in objectsToEnable)
            {
                if (obj != null)
                    obj.SetActive(true);
            }
        }

        if (objectsToDisable != null)
        {
            foreach (var obj in objectsToDisable)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }
}