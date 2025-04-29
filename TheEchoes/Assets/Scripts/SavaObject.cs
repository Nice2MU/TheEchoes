using UnityEngine;
using System;

public class SavaObject : MonoBehaviour
{
    [SerializeField]
    private string uniqueID = Guid.NewGuid().ToString();
    public string UniqueID => uniqueID;

    public ObjectData GetData()
    {
        return new ObjectData(transform);
    }

    public void LoadData(ObjectData data)
    {
        data.ApplyTo(transform);
    }
}