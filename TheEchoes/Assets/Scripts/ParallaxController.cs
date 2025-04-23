using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    Transform cam;
    Vector3 camStartPos;
    float distance;

    GameObject[] backgrounds;
    Material[] mat;
    float[] backSpeed;

    float farthestBack;

    [Range(0.01f, 0.05f)]
    public float parallaxSpeed = 0.03f;

    public Vector3[] customStartPositions;

    [Header("Pixel Art Settings")]
    public float pixelsPerUnit = 100f;

    void Start()
    {
        cam = Camera.main.transform;
        camStartPos = cam.position;

        int backCount = transform.childCount;
        mat = new Material[backCount];
        backSpeed = new float[backCount];
        backgrounds = new GameObject[backCount];

        if (customStartPositions == null || customStartPositions.Length != backCount)
        {
            customStartPositions = new Vector3[backCount];
            for (int i = 0; i < backCount; i++)
            {
                customStartPositions[i] = transform.GetChild(i).position;
            }
        }

        for (int i = 0; i < backCount; i++)
        {
            backgrounds[i] = transform.GetChild(i).gameObject;
            Renderer renderer = backgrounds[i].GetComponent<Renderer>();
            if (renderer != null)
            {
                mat[i] = renderer.material;
            }
        }

        BackSpeedCalculate(backCount);
    }

    void BackSpeedCalculate(int backCount)
    {
        for (int i = 0; i < backCount; i++)
        {
            float backDist = backgrounds[i].transform.position.z - cam.position.z;
            if (backDist > farthestBack)
            {
                farthestBack = backDist;
            }
        }

        for (int i = 0; i < backCount; i++)
        {
            float backDist = backgrounds[i].transform.position.z - cam.position.z;
            backSpeed[i] = 1 - (backDist / farthestBack);
        }
    }

    private void FixedUpdate()
    {
        Vector3 camOffset = cam.position - camStartPos;

        camOffset.x = Mathf.Lerp(camOffset.x, cam.position.x - camStartPos.x, 0.05f);
        camOffset.y = Mathf.Lerp(camOffset.y, cam.position.y - camStartPos.y, 0.05f);

        for (int i = 0; i < backgrounds.Length; i++)
        {
            float speed = backSpeed[i] * parallaxSpeed;
            Vector3 moveOffset = new Vector3(camOffset.x, 0, 0);
            Vector3 newPos = customStartPositions[i] + moveOffset;

            backgrounds[i].transform.position = Vector3.Lerp(backgrounds[i].transform.position, newPos, 0.1f);

            if (mat[i] != null)
            {
                mat[i].SetTextureOffset("_MainTex", new Vector2(camOffset.x, 0) * speed);
            }
        }
    }
}