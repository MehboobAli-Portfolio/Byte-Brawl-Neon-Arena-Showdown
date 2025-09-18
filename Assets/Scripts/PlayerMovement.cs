using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    private Animator anim;
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 500.0f;
    private bool canJump = true;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
        Vector3 rotateY = new Vector3(0, Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime, 0);
        rb.MoveRotation(rb.rotation* Quaternion.Euler(rotateY));
        rb.MovePosition(
            rb.position +
            transform.forward * Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime +
            transform.right * (-Input.GetAxis("Horizontal")) * -moveSpeed * Time.deltaTime
        );
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
