using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MouseMovement : MonoBehaviour {

    public float hp;
    public bool damageable = true;

    private NavMeshAgent agent;
    private float movSpeed = 10f;
    private float turnSpeed = 120f;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = movSpeed;
        agent.angularSpeed = turnSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        Destroy();
    }

    private void Destroy() {
        if (hp == 0.0f) {
            Destroy(gameObject);
        }
    }
}
