using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PController : MonoBehaviour
{
    [SerializeField] private float speed = 100.0f;
    [SerializeField] private float camSpeed = 100.0f;
    [SerializeField] private float swordSpeed = 100.0f;

    public bool invert = false;
    public bool disableLimit = false;

    Rigidbody rb;
    Animator anim;

    GameObject blade;
    GameObject trail;

    bool active = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        Cursor.lockState = CursorLockMode.Locked;
        blade = GameObject.Find("Blade");
        trail = GameObject.Find("TrailEmitters");
        trail.SetActive(false);
    }

    private void Update()
    {
        SpinsSword();

        if (Input.GetButton("Aim"))
        {
            anim.SetBool("Aiming", true);
            AimSword();
            return;
        }
        else
        {
            anim.SetBool("Aiming", false);
        }
        
        if (Input.GetButtonDown("Attack"))
        {
            Cursor.lockState = CursorLockMode.Locked;
            anim.SetTrigger("Swing");
        }

        if (Input.GetButtonDown("Activate"))
        {
            Activate();
        }

        Move();
        MoveCamera();
    }

    private void Move()
    {
        Vector3 move = (transform.forward * Input.GetAxis("Vertical"));
        move += transform.right * Input.GetAxis("Horizontal");
        move *= Time.deltaTime * speed;

        transform.position += move;
    }

    private void MoveCamera()
    {
        Vector3 angle = transform.eulerAngles;
        angle.y += Input.GetAxis("HorizontalAim") * Time.deltaTime * camSpeed;
        transform.eulerAngles = angle;

        Transform cam = GameObject.Find("Main Camera").transform;
        Vector3 aim = cam.localEulerAngles;
        aim.x += Input.GetAxis("VerticalAim") * Time.deltaTime * camSpeed * (invert ? 1 : -1);

        if (!disableLimit)
        {
            if (aim.x < 20)
            {
                aim.x = 20;

            }
            else if (aim.x > 160)
            {
                aim.x = 160;
            }
        }

        cam.localEulerAngles = aim;
    }


    private void AimSword()
    {
        Transform hand = GameObject.Find("Hand").transform;
        float y = Input.GetAxis("HorizontalAim") * Time.deltaTime * swordSpeed;
        float x = Input.GetAxis("VerticalAim") * Time.deltaTime * swordSpeed;

        hand.rotation = Quaternion.Euler(x, y, 0) * hand.rotation;
    }

    private void SpinsSword()
    {
        Transform sword = GameObject.Find("Sword").transform;

        Vector3 rot = sword.localEulerAngles;
        rot.y += Input.GetAxis("Spin") * Time.deltaTime * swordSpeed / 5;
        sword.localEulerAngles = rot;
    }

    private void Activate()
    {
        active = !active;
        blade.SetActive(active);
    }
}
