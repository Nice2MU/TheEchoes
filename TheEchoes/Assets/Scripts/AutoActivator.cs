using UnityEngine;

public class AutoActivator : MonoBehaviour
{
    [SerializeField] private GameObject[] objectsToEnable;

    private void Awake()
    {
        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
    }
}