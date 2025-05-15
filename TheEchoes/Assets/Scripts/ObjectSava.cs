using UnityEngine;
using System;

public class ObjectSave : MonoBehaviour
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