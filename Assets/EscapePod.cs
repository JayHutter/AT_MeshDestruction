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

    bool locked = true;

    // Start is called before the first frame update
    void Start()
    {
        if (!door || !endPoint)
        {
            Destroy(this);
        }
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
            Vector3 pos = Vector3.Lerp(door.transform.position, endPoint.position, Time.deltaTime * speed);
            door.transform.position = pos;

            Vector3 distance = door.transform.position - endPoint.position;
            if (distance.magnitude <= 0.1f)
            {
                Destroy(this);
            }
        }
    }
}
