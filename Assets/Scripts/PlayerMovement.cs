using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Animator anim;
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 500.0f;
    private bool canJump = true;
    private float mouseX;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;// Prevent physics-based rotation drift
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
        Vector3 moveDir = transform.forward * Input.GetAxis("Vertical") + transform.right * Input.GetAxis("Horizontal");
        rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
        transform.Rotate(0, mouseX, 0); // rotates only around Y-axis
        anim.SetFloat("BlendV", Input.GetAxis("Vertical"));
        anim.SetFloat("BlendH", Input.GetAxis("Horizontal"));  
    }
    private void Update()
    {
        if (Input.GetButtonDown("Jump") && canJump == true)
        {
            canJump = false;
            rb.AddForce(Vector3.up * 130 * Time.deltaTime, ForceMode.VelocityChange);
            StartCoroutine(JumpAgain());
        }
    }
    IEnumerator JumpAgain()
    {
        yield return new WaitForSeconds(1);
        canJump = true;
    }
}
