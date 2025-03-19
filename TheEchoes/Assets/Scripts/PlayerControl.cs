using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControl : MonoBehaviour
{
    public float speed = 5f;
    public float minJumpForce = 5f;
    public float maxJumpForce = 12f;
    public float chargeTime = 1f;
    public Transform groundCheck;
    public LayerMask groundLayer;
    public Slider jumpChargeBar;

    public float health = 100f;
    public static Vector3 lastCheckpointPosition;

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private float jumpCharge;

    public GameObject[] objectsToReset; // ออบเจกต์ที่ต้องการรีเซ็ตตำแหน่ง

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (jumpChargeBar != null)
            jumpChargeBar.value = 0;

        // ตั้งค่าตำแหน่ง Checkpoint เริ่มต้น
        lastCheckpointPosition = transform.position;
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(moveX * speed, rb.velocity.y);

        animator.SetBool("walk", moveX != 0);
        if (moveX != 0) spriteRenderer.flipX = moveX < 0;

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
        animator.SetBool("isGrounded", isGrounded);

        if (Input.GetButton("Jump") && isGrounded)
        {
            jumpCharge += Time.deltaTime / chargeTime;
            jumpCharge = Mathf.Clamp(jumpCharge, 0f, 1f);

            if (jumpChargeBar != null)
                jumpChargeBar.value = jumpCharge;
        }

        if (Input.GetButtonUp("Jump") && isGrounded)
        {
            float jumpPower = Mathf.Lerp(minJumpForce, maxJumpForce, jumpCharge);
            rb.velocity = new Vector2(rb.velocity.x, jumpPower);
            animator.SetTrigger("jump");

            jumpCharge = 0;
            if (jumpChargeBar != null)
                jumpChargeBar.value = 0;
        }

        if (health <= 0f)
        {
            Respawn();
        }
    }

    // ฟังก์ชันที่จะรีเซ็ตตำแหน่งผู้เล่นและตำแหน่งของออบเจกต์ที่กำหนด
    void Respawn()
    {
        transform.position = lastCheckpointPosition;
        health = 100f; // ฟื้นฟูพลังชีวิตผู้เล่น

        // รีเซ็ตตำแหน่งของออบเจกต์ที่กำหนด
        foreach (GameObject obj in objectsToReset)
        {
            ObjectReset objectResetScript = obj.GetComponent<ObjectReset>();
            if (objectResetScript != null)
            {
                objectResetScript.ResetPosition(); // รีเซ็ตตำแหน่งของออบเจกต์
            }
        }
    }

    // ฟังก์ชันที่ทำงานเมื่อผู้เล่นตกน้ำ
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            health = 0f; // ลดพลังชีวิตของผู้เล่นให้เป็น 0
        }
    }
}
