using UnityEngine;
using System.Collections;

public class TeleporterDaydream : MonoBehaviour
{

    //public LineRenderer laser;
    public GameObject redTarget;
    public GvrTrackedController trackedController;

    Vector3 currentTargetPos;
    public GameObject daydreamPlayer;
    void Awake()
    {
        if (trackedController == null)
        {
            Debug.LogWarning("Assign controller");
            GvrTrackedController left = (GvrTrackedController)GameObject.Find("GvrControllerPointer");
            if (left != null)
            {
                trackedController = left;
                Debug.Log("FOUND!");
            }
        }
        
    }
    void Start()
        {
            Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
            //laser.SetPositions(initLaserPositions);
            //laser.startWidth = 0.01f;
            //laser.endWidth = 0.01f;
        }

    void Update()
    {
        ShootLaserFromTargetPosition(transform.position, this.transform.forward, 500f);
        //laser.enabled = true;

        if (trackedController.ControllerInputDevice.GetButtonDown(GvrControllerButton.App))
        {
            Debug.Log("click click");
            // teleport to location
            Vector3 pointerTeleportPos = new Vector3(currentTargetPos.x, 1.75f, currentTargetPos.z);
            daydreamPlayer.transform.position = pointerTeleportPos;
        }
    }

    void ShootLaserFromTargetPosition(Vector3 targetPosition, Vector3 direction, float length)
    {
        Ray ray = new Ray(targetPosition, direction);
        RaycastHit raycastHit;

        if (Physics.Raycast(ray, out raycastHit, length))
        {
            GameObject gameObj = raycastHit.transform.gameObject;
            if (gameObj.tag == "GroundTag")
            {
                // Show the target and follow track to the pointer
                currentTargetPos = raycastHit.point;
            //    redTarget.transform.localPosition = raycastHit.point;
            }
            else
            {
             //   redTarget.transform.localPosition = new Vector3(0f, -10f, 0f);
            }
        }

        Vector3 endPosition = targetPosition + (length * direction);
        //laser.SetPosition(0, targetPosition);
        //laser.SetPosition(1, endPosition);
    }
}