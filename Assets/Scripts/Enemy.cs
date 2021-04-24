using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public GameObject gun;
    internal GameObject body;

    Animator anim;
    NavMeshAgent agent;

    enum State
    {
        idle = 0,
        roam,
        attack
    }

    [SerializeField]
    State state = State.idle;
    GameObject player;

    public float attackRange = 10;
    public float roamDistance = 10;

    Vector3 startPos;

    public bool roam;
    float timer = 0;

    private void Start()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        player = FindObjectOfType<PController>().gameObject;
        startPos = transform.position;
    }


    public void Cut(Plane intersection)
    {
        if (gun)
        {
            gun.transform.parent = null;
            Rigidbody rb = gun.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            gun = null;
        }

        if (body == null)
        {
            body = (GameObject)Instantiate(Resources.Load("EnemyCut", typeof(GameObject)));
            body.transform.position = transform.position;
            body.transform.rotation = transform.rotation;
            body.transform.localScale = transform.localScale;

            Slicer slc = body.GetComponent<Slicer>();

            if (!slc.BeingDestroyed())
            {
                slc.DestroyMesh(intersection);
            }
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        switch (state)
        {
            case State.idle:
                Idle();
                break;
            case State.roam:
                Roaming();
                break;
            case State.attack:
                Attack();
                break;
        }

        Vector3 vel = agent.velocity;
        anim.SetBool("Moving", vel.magnitude > 0.1f);
    }

    private void Idle()
    {
        anim.SetInteger("State", 0);

        Vector3 dif = transform.position - player.transform.position;
        if (dif.magnitude < attackRange)
        {
            state = State.attack;
        }


        if (roam)
        {
            Vector3 dir = Random.insideUnitSphere * roamDistance;
            dir += startPos;

            NavMeshHit navHit;
            NavMesh.SamplePosition(dir, out navHit, roamDistance, -1);

            agent.SetDestination(navHit.position);
            state = State.roam;
        }
    }

    private void Roaming()
    {
        anim.SetInteger("State", 0);
        Vector3 dif = transform.position - player.transform.position;
        if (dif.magnitude < attackRange)
        {
            state = State.attack;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathComplete)
        {
            timer += Time.deltaTime;

            if (timer >= 1)
            {
                agent.SetDestination(transform.position);
                state = State.idle;
            }

            roam = false;
        }
    }

    private void Attack()
    {
        anim.SetInteger("State", 1);

        Vector3 dif = transform.position - player.transform.position;
        if (dif.magnitude > attackRange)
        {
            state = State.idle;
        }
    }
}
