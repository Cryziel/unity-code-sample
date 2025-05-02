using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.FirstPerson;

public class Boat : MonoBehaviour
{
    private NavMeshAgent agent;
    public Transform[] SpawnPoints;
    public FuelTanker FuelTanker;
    public Transform PlayerRoot;
    public Waypoints Route;
    public Waypoints[] Routes;
    public int PointIndex;
    public int RoutNumberSpawn; // when loading, determine which route the boat should be on so it spawns there
    public Transform target;

    public float rotationSpeed = 1f;
    public float ParkingDistance = 3f; // the distance at which the boat starts maneuvering to dock

    public GameObject FuelEmptyHint;

    public GameObject Smoke;
    public GameObject StallSound;

    bool EngineWork;


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    private void Update()
    {
        BoatMovement();
    }

    void MoveToTarget()
    {
        agent.SetDestination(target.position);
    }

    public void Spawn() // spawn on correct route when loading. In the PrefsForSave class, world state is loaded and RoutNumberSpawn is assigned
    {
        agent.enabled = false;
        target = null;

        if (RoutNumberSpawn > 0)
            FuelTanker.Fuel = true;

        int routeIndex = RoutNumberSpawn - 1;
        if (routeIndex >= 0)
        {
            Route = Routes[routeIndex];
            transform.SetPositionAndRotation(SpawnPoints[RoutNumberSpawn].position, SpawnPoints[RoutNumberSpawn].rotation);
        }

        agent.enabled = true;
    }

    public void Sail() // start boat on the route
    {
        if (FuelTanker.Fuel)
        {
            EngineWork = true;
            if (Smoke != null) Smoke.SetActive(true);

            Route.Route[PointIndex].gameObject.SetActive(true);
            target = Route.Route[PointIndex];

            if (!PlayerPrefs.HasKey("TheBoatiIsAGuide")) // Get achievement
            {
                Achievements.TheBoatiIsAGuide();
                PlayerPrefs.SetInt("TheBoatiIsAGuide", 1);
                PlayerPrefs.Save();
            }
        }
        else
        {
            FuelEmptyHint.SetActive(true); // if there's no fuel, show a hint
        }
    }
    public void NextPoint() // next point within the current route
    {
        PointIndex++;
        if (PointIndex >= Route.Route.Length)
        {
            PointIndex = 0;
        }
        Sail();
    }
    public void NextRoute() // switch to the required route, which will have its own waypoints
    {
        PointIndex = 0;
        Route = Route.NextRoute;
    }

    IEnumerator EngineOffDelay()
    {
        yield return new WaitForSeconds(1);
        if (Smoke != null) Smoke.GetComponent<ParticleSystem>().loop = false;
        yield return new WaitForSeconds(1);
        StallSound.SetActive(true);
        Smoke.SetActive(false);
    }

    public void BoatMovement()
    {
        if (agent == null || target == null)
            return;

        MoveToTarget();

        var waypoints = target.GetComponent<Waypoints>();
        bool isAtStop = agent.remainingDistance <= agent.stoppingDistance;
        bool isAtParking = agent.remainingDistance <= ParkingDistance;

        if (isAtStop && waypoints.Finish) // boat has stopped
        {
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= agent.stoppingDistance && EngineWork)
            {
                EngineWork = false;
                StartCoroutine(EngineOffDelay());
            }
        }

        if (isAtParking && waypoints.Finish) // if boat is close to finish, it smoothly turns in advance toward the right direction to avoid turning during the start
        {
            Vector3 targetDirection = target.forward;
            targetDirection.y = 0f;

            if (targetDirection.sqrMagnitude > Mathf.Epsilon)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.5f * Time.deltaTime);
            }
        }
        else if (agent.velocity != Vector3.zero) // if boat is moving, it turns in the direction of its movement
        {
            Vector3 movementDirection = agent.velocity.normalized;
            movementDirection.y = 0f;

            Quaternion targetRotation = Quaternion.LookRotation(movementDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other) 
    {
        var rb = other.attachedRigidbody;

        if (other.CompareTag("Player")) // the player boarded the boat seamlessly
        {
            other.transform.SetParent(transform);

            var fpc = other.GetComponent<FirstPersonController>();
            if (fpc.PcControls) // to ensure seamlessness, adjust the player's view to new coordinates so their actual position doesn't change
            {
                fpc.m_MouseLook.m_CharacterTargetRot = Quaternion.Euler(0f, other.transform.eulerAngles.y - transform.eulerAngles.y, 0f);
            }
            else
            {
                other.GetComponent<ForCamRotate>()._RotatingY += transform.eulerAngles.y;
            }
        }
        else if (rb != null && !rb.isKinematic) // for transporting dynamic objects in the boat
        {
            other.transform.SetParent(transform);
        }

        if (other.CompareTag("Waypoint")) // next point in the current route
        {
            other.gameObject.SetActive(false);
            if (!other.GetComponent<Waypoints>().Finish)
            {
                NextPoint();
            }
            else
            {
                PointIndex = (PointIndex < Route.Route.Length - 1) ? PointIndex + 1 : 0;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        var rb = other.attachedRigidbody;

        if (other.CompareTag("Player")) // player exited the boat
        {
            other.transform.SetParent(PlayerRoot);

            var fpc = other.GetComponent<FirstPersonController>();
            if (fpc.PcControls) // to ensure seamlessness, adjust the player's view to new coordinates so their actual position doesn't change
            {
                fpc.m_MouseLook.m_CharacterTargetRot = Quaternion.Euler(0f, other.transform.eulerAngles.y, 0f);
            }
            else
            {
                other.GetComponent<ForCamRotate>()._RotatingY -= transform.eulerAngles.y;
            }
        }
        else if (rb != null && !rb.isKinematic) // dynamic object exited the boat
        {
            other.transform.parent = null;
        }
    }
}
