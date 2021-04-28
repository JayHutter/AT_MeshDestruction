using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnObject : MonoBehaviour
{
    public GameObject[] objects;

    Transform playerTransform;

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            int index = Random.Range(0, objects.Length);

            Vector3 pos = playerTransform.position + (playerTransform.forward * 10) + (playerTransform.up * 3);

            var newObj = Instantiate(objects[index]);
            newObj.transform.position = pos;
        }
    }
}
