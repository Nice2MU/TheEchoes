using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjectData
{
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;
    public float scaleX, scaleY, scaleZ;
    public List<ObjectData> childrenData = new List<ObjectData>();

    public bool isRectTransform;

    public ObjectData() { }

    public ObjectData(Transform transform)
    {
        isRectTransform = transform is RectTransform;

        if (isRectTransform)
        {
            RectTransform rect = transform as RectTransform;
            posX = rect.localPosition.x;
            posY = rect.localPosition.y;
            posZ = rect.localPosition.z;
        }
        else
        {
            posX = transform.position.x;
            posY = transform.position.y;
            posZ = transform.position.z;
        }

        rotX = transform.rotation.x;
        rotY = transform.rotation.y;
        rotZ = transform.rotation.z;
        rotW = transform.rotation.w;

        scaleX = transform.localScale.x;
        scaleY = transform.localScale.y;
        scaleZ = transform.localScale.z;

        foreach (Transform child in transform)
        {
            childrenData.Add(new ObjectData(child));
        }
    }

    public void ApplyTo(Transform transform)
    {
        if (isRectTransform && transform is RectTransform rect)
        {
            rect.localPosition = new Vector3(posX, posY, posZ);
        }
        else
        {
            transform.position = new Vector3(posX, posY, posZ);
        }

        transform.rotation = new Quaternion(rotX, rotY, rotZ, rotW);
        transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

        int i = 0;
        foreach (Transform child in transform)
        {
            if (i < childrenData.Count)
            {
                childrenData[i].ApplyTo(child);
            }
            i++;
        }
    }
}