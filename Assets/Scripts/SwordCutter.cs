using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordCutter : MonoBehaviour
{

    internal Vector3 hiltPoint;    
    internal Vector3 entryPoint;
    internal Vector3 exitPoint;

    public LayerMask destructable;

    GameObject hiltParticles;
    GameObject tipParticles;

    bool colliding = false;

    Light bladeLight;
    bool redLight = true;
    Material blade;

    Color defaultBlade;
    float intensity;

    GameObject hiltTrail;
    GameObject tipTrail;
    GameObject cutTrail;

    private void Start()
    {
        hiltParticles = GameObject.Find("CollisionHilt");
        tipParticles = GameObject.Find("CollisionTip");
        bladeLight = GameObject.Find("BladeLight").GetComponent<Light>();
        blade = GameObject.Find("Blade").GetComponent<Renderer>().material;

        defaultBlade = blade.GetColor("_EmissionColor");
        intensity = defaultBlade.maxColorComponent;

        hiltTrail = GameObject.Find("CutTrailHilt");
        hiltTrail.SetActive(false);

        tipTrail = GameObject.Find("CutTrailTip");
        tipTrail.SetActive(false);
        cutTrail = (GameObject)Instantiate(Resources.Load("CutTrail", typeof(GameObject)));
    }

    private void OnTriggerEnter(Collider other)
    {
        colliding = true;

        SpawnParticles();

        hiltPoint = transform.Find("Hilt").position;
        entryPoint = transform.Find("Tip").position;

        hiltTrail.GetComponent<TrailRenderer>().Clear();
        tipTrail.GetComponent<TrailRenderer>().Clear();

        cutTrail.transform.position = entryPoint;
        cutTrail.GetComponentInChildren<TrailRenderer>().Clear();
    }

    private void Update()
    {
        //ShowParticles();


        
    }

    private void OnTriggerExit(Collider other)
    {
        colliding = false;
        SpawnParticles();

        exitPoint = transform.Find("Tip").position;

        //Create plane
        Vector3 sideA = exitPoint - entryPoint;
        Vector3 sideB = exitPoint - hiltPoint;

        Vector3 normal = Vector3.Cross(sideA, sideB).normalized;

        Vector3 planeNormal = ((Vector3)(other.gameObject.transform.localToWorldMatrix.transpose * normal)).normalized;
        Vector3 startPoint = other.gameObject.transform.InverseTransformPoint(hiltPoint);

        Plane intersection = new Plane();
        intersection.SetNormalAndPosition(planeNormal, startPoint);

        GameObject obj = other.gameObject;
        Enemy enemy = obj.GetComponentInParent<Enemy>();

        if (enemy)
        {
            Debug.Log("CUT ENEMY");
            enemy.Cut(intersection);
        }
        else
        {
            Slicer slc = obj.GetComponent<Slicer>();

            if (!slc)
            {
                slc = obj.AddComponent<Slicer>();
            }

            if (!slc.BeingDestroyed())
            {
                slc.DestroyMesh(intersection);
            }
        }

        if (obj.tag == "Enemy")
        {
            GameObject blood = (GameObject)Instantiate(Resources.Load("Particles/BloodExplode", typeof(GameObject)));
            blood.transform.position = (exitPoint + entryPoint)/2;
        }

        //redLight = true;
        //SetLightColour();
        hiltTrail.GetComponent<TrailRenderer>().Clear();
        hiltTrail.SetActive(false);
        tipTrail.GetComponent<TrailRenderer>().Clear();
        tipTrail.SetActive(false);

        cutTrail.transform.position = exitPoint;
    }

    private void OnTriggerStay(Collider other)
    {
        SpawnParticles();
        
    }

    void SpawnParticles()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.Find("Hilt").position, transform.up, out hit, destructable))
        {
            if (hit.distance < transform.localScale.y)
            {
                GameObject particle = (GameObject)Instantiate(Resources.Load("Particles/Collision"));
                particle.transform.position = hit.point;
                hiltTrail.transform.position = hit.point;
                hiltTrail.SetActive(true);
                //GameObject.Find("BladeLight").transform.position = hit.point;
            }
            else
            {
                hiltTrail.SetActive(false);
            }
        }

        if (Physics.Raycast(transform.Find("Tip").position, -transform.up, out hit, destructable))
        {
            if (hit.distance < transform.localScale.y)
            {
                GameObject particle = (GameObject)Instantiate(Resources.Load("Particles/Collision"));
                particle.transform.position = hit.point;
                tipTrail.transform.position = hit.point;
                tipTrail.SetActive(true);
            }
            else
            {
                tipTrail.SetActive(false);
            }
        }
    }

    void ShowParticles()
    {
        if (!colliding)
        {
            tipParticles.SetActive(false);
            hiltParticles.SetActive(false);
            return;
        }

        RaycastHit hit;

        if (Physics.Raycast(transform.Find("Hilt").position, transform.up, out hit, destructable))
        {
            hiltParticles.SetActive(true);
            hiltParticles.transform.position = hit.point;
        }
        else
        {
            hiltParticles.SetActive(false);
        }

        if (Physics.Raycast(transform.Find("Tip").position, -transform.up, out hit, destructable) &&
            hit.distance < transform.localScale.y)
        {
            tipParticles.SetActive(true);
            tipParticles.transform.position = hit.point;
        }
        else
        {
            tipParticles.SetActive(false);
        }
    }

    private void FlashLight()
    {
        redLight = !redLight;
    }

    private void SetLightColour()
    {
        if (!redLight)
        {
            bladeLight.color = Color.red;
            blade.SetColor("_EmissionColor", defaultBlade);
            return;
        }

        Color col = new Color(0.5f, 0, 0);

        blade.SetColor("_EmissionColor", col * intensity);
        bladeLight.color = Color.yellow;
    }
}
