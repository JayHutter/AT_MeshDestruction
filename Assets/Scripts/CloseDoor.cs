using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseDoor : MonoBehaviour
{
    public GameObject door;
    public Transform raisedPoint;

    public float closeDistance = 40;
    public float speed = 2;

    GameObject player;

    bool closing = false;

    private void Start()
    {
        player = FindObjectOfType<PController>().gameObject;
        door.transform.position = raisedPoint.position;
    }

    private void Update()
    {
        if (!door)
        {
            Destroy(this);
        }

        Vector3 dif = transform.position - player.transform.position;
        
        if (dif.magnitude <= closeDistance)
        {
            closing = true; 
        }

        if (closing)
        {
            Vector3 pos = Vector3.Lerp(door.transform.localPosition, Vector3.zero, Time.deltaTime * speed);
            door.transform.localPosition = pos;
        }

        if (Vector3.Distance(door.transform.localPosition, Vector3.zero) < 0.2f)
        {
            Destroy(this);
        }
    }
}
