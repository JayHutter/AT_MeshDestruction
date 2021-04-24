using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    int controlPanels = 0;
    int totalPanels = 0;
    private void Start()
    {
        GameObject[] obj = GameObject.FindGameObjectsWithTag("Control Panel");
        totalPanels = obj.Length;
    }

    private void Update()
    {
        GameObject[] obj = GameObject.FindGameObjectsWithTag("Control Panel");
        controlPanels = obj.Length;

        if (controlPanels <= 0)
        {
            EscapePod[] pods = FindObjectsOfType<EscapePod>();
            
            foreach(EscapePod pod in pods)
            {
                pod.Unlock();
            }
        }
    }
}
