using UnityEngine;

public class ObjectReset : MonoBehaviour
{
    private Vector3 initialPosition;

    void Start()
    {
        // เก็บตำแหน่งเริ่มต้น
        initialPosition = transform.position;
        Debug.Log(gameObject.name + " initial position: " + initialPosition);
    }

    public void ResetPosition()
    {
        // รีเซ็ตตำแหน่ง
        transform.position = initialPosition;
        Debug.Log(gameObject.name + " position reset to: " + initialPosition);  // ตรวจสอบว่าเรียกใช้งาน ResetPosition
    }
}
