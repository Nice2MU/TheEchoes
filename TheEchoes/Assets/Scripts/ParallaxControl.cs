using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParallaxControl : MonoBehaviour
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

    private Vector3[] velocity;

    void Start()
    {
        cam = Camera.main.transform;
        camStartPos = cam.position;

        int backCount = transform.childCount;
        mat = new Material[backCount];
        backSpeed = new float[backCount];
        backgrounds = new GameObject[backCount];
        velocity = new Vector3[backCount];

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

        camOffset.x = Mathf.Lerp(camOffset.x, cam.position.x - camStartPos.x, 0.1f * Time.deltaTime);
        camOffset.y = Mathf.Lerp(camOffset.y, cam.position.y - camStartPos.y, 0.1f * Time.deltaTime);

        for (int i = 0; i < backgrounds.Length; i++)
        {
            float speed = backSpeed[i] * parallaxSpeed;
            Vector3 moveOffset = new Vector3(camOffset.x, 0, 0);
            Vector3 targetPos = customStartPositions[i] + moveOffset;

            backgrounds[i].transform.position = Vector3.SmoothDamp(backgrounds[i].transform.position, targetPos, ref velocity[i], 0.2f, Mathf.Infinity, Time.deltaTime);

            if (mat[i] != null)
            {
                mat[i].SetTextureOffset("_MainTex", new Vector2(camOffset.x, 0) * speed);
            }
        }
    }
}