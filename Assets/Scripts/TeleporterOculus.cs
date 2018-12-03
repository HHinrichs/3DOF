using UnityEngine;
using System.Collections;

public class TeleporterOculus: MonoBehaviour
{

    //public LineRenderer laser;
 //   public GameObject redTarget;
    private Transform leftHandAnchor;
    private Transform rightHandAnchor;
    private Transform centerEyeAnchor;
    Vector3 currentTargetPos;
    public GameObject player;
    void Awake()
    {
        if (leftHandAnchor == null)
        {
            Debug.LogWarning("Assign LeftHandAnchor in the inspector!");
            GameObject left = GameObject.Find("LeftHandAnchor");
            if (left != null)
            {
                leftHandAnchor = left.transform;
            }
        }
        if (rightHandAnchor == null)
        {
            Debug.LogWarning("Assign RightHandAnchor in the inspector!");
            GameObject right = GameObject.Find("RightHandAnchor");
            if (right != null)
            {
                Debug.Log("Found left hand anchor");
                rightHandAnchor = right.transform;
            }
        }
        if (centerEyeAnchor == null)
        {
            Debug.LogWarning("Assign CenterEyeAnchor in the inspector!");
            GameObject center = GameObject.Find("CenterEyeAnchor");
            if (center != null)
            {
                centerEyeAnchor = center.transform;
            }
        }
    }

    Transform Pointer
    {
        get
        {
            OVRInput.Controller controller = OVRInput.GetConnectedControllers();
            if ((controller & OVRInput.Controller.LTrackedRemote) != OVRInput.Controller.None)
            {
                return leftHandAnchor;
            }
            else if ((controller & OVRInput.Controller.RTrackedRemote) != OVRInput.Controller.None)
            {
                return rightHandAnchor;
            }
            // If no controllers are connected, we use ray from the view camera. 
            // This looks super ackward! Should probably fall back to a simple reticle!
            return centerEyeAnchor;
        }
    }

    void Start()
        {
            Vector3[] initLaserPositions = new Vector3[2] { Vector3.zero, Vector3.zero };
        //laser.SetPositions(initLaserPositions);
        //laser.startWidth = 0.01f;
        //laser.endWidth = 0.01f;
        rightHandAnchor = this.transform;
        }

    void Update()
    {
        ShootLaserFromTargetPosition(rightHandAnchor.position, rightHandAnchor.forward, 500f);
        //laser.enabled = true;

        if (OVRInput.GetDown(OVRInput.Button.Two) == true)
        {
            // teleport to location
            Vector3 pointerTeleportPos = new Vector3(currentTargetPos.x, 1.75f, currentTargetPos.z);
            player.transform.position = pointerTeleportPos;
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