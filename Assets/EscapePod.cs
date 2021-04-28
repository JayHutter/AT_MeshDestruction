using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapePod : MonoBehaviour
{
    public GameObject door;
    public Transform endPoint;
    public Light doorLight;
    public GameObject circleLock;
    public float speed = 1.5f;
    private Transform closePoint;

    bool locked = true;

    bool opening = false;
    bool closing = false;

    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
        if (!door || !endPoint)
        {
            Destroy(this);
        }

        closePoint = transform;
        canvas.SetActive(false);
    }

    public void Unlock()
    {
        if (doorLight)
        {
            doorLight.color = Color.green;
        }

        if (circleLock)
        {
            circleLock.GetComponent<MeshRenderer>().material = (Material)Resources.Load("Materials/World/Unlocked", typeof(Material));
        }

        locked = false;
    }

    private void Update()
    {
        GameObject player = FindObjectOfType<PController>().gameObject;
        Vector3 playerDistance = door.transform.position - player.transform.position;

        if (!locked && playerDistance.magnitude <= 5)
        {
            opening = true;
           
        }

        if (opening)
        {
            Vector3 pos = Vector3.Lerp(door.transform.position, endPoint.position, Time.deltaTime * speed);
            door.transform.position = pos;

            Vector3 distance = door.transform.position - endPoint.position;
            if (distance.magnitude <= 0.1f)
            {
                opening = false;
                locked = true;
            }
        }

        if (closing)
        {
            Vector3 pos = Vector3.Lerp(door.transform.position, closePoint.position, Time.deltaTime * speed);
            door.transform.position = pos;

            Vector3 distance = door.transform.position - closePoint.position;
            if (distance.magnitude <= 0.15f)
            {
                Destroy(this);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {

        if (other.gameObject.tag == "Player")
        {
            Debug.Log("S");
            closing = true;
            canvas.SetActive(true);
        }
    }
}
