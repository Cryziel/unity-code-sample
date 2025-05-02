using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantView : MonoBehaviour
{
    public Transform startPoint; // point from where the gaze raycast is fired
    public Transform targetObject; // object the giant should watch. Usually player
    public LayerMask IgnoreRaycast;

    bool Danger;
    bool Founded;
    Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        GiantWatch();
    }

    void GiantWatch()
    {
        if (startPoint == null || targetObject == null)
            return;

        Vector3 direction = (targetObject.position - startPoint.position).normalized;

        float maxDistance = Vector3.Distance(startPoint.position, targetObject.position);

        RaycastHit hit;
        if (Physics.Raycast(startPoint.position, direction, out hit, maxDistance, ~IgnoreRaycast, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject == targetObject.gameObject)
            {
                Debug.DrawLine(startPoint.position, hit.point, Color.green);

                if (Danger) // If the giant is currently able to see
                {
                    Debug.DrawLine(startPoint.position, hit.point, Color.yellow);

                    if (Player.InZone) // If player is in a specific zone where giant starts searching for player
                    {
                        FoundPlayer();
                    }
                }
            }
            else
            {
                Debug.DrawLine(startPoint.position, hit.point, Color.blue);
                animator.SetBool("Spot", false);
                Founded = false;
            }
        }
        else
        {
            Debug.DrawLine(startPoint.position, startPoint.position + direction * maxDistance, Color.red);
        }
    }

    void FoundPlayer()
    {
        if (!Founded)
        {
            Debug.Log("Player Founded");

            Danger = false;
            Founded = true;
            animator.SetBool("Spot", true); // event trigger calls Giant.Attack();
        }
    }
}
