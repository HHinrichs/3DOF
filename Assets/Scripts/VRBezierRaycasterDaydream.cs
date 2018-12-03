using UnityEngine;
using UnityEngine.Events;

public class VRBezierRaycasterDaydream : MonoBehaviour
{

    [System.Serializable]
    public class Callback : UnityEvent<Ray, RaycastHit> { }

    [Header("Settings")]
    [Tooltip("Maximum distance the ray gets casted")]
    public float maxRayDistance = 500.0f;
    [Tooltip("Number of interpolation points within the line. The more points the more overhead")]
    public int interpolationPoints;
    [Tooltip("Value between 0 and 100. Closer to 100 increases bending factor")]
    public float bezierPoint2PercentualPosition = 50;
    // How fast does the object gets away from me
    [Tooltip("Sensibility of touchpad swipe. Higher value increases sensibility")]
    public float zoomFactor = 3;
    // Offset the Grabed Object is close to me. It is recommended to set the factor that the Asset does not get clipped into the Camera
    [Tooltip("Offset from near Object to controller")]
    public float closeOffset = 0.0f;
    [Header("Modes")]
    [Tooltip("Enable editor Testing. 'e' Button for anchoring the Object")]
    public bool testMode = false;
    [Tooltip("Enables rotation of the Object")]
    public bool enableRotation = false;

    [Header("Prefabs")]
    [Tooltip("Prefab for the attachment Point")]
    public GameObject anglePointPrefab;
    [Tooltip("Prefab for the bezierPoint2 - 'bending Around Point'")]
    public GameObject bezierPoint2Prefab;
    [Tooltip("Prefab for the desired cursor")]
    public GameObject hitPointCursorPrefab;
    [Tooltip("The LineRenderer instance of the GO")]
    public LineRenderer lineRenderer = null;

    [Header("Spring Joint settings")]
    public float springForce = 50;
    public float damperForce = 2;
    public float minDistance = 0;
    public float maxDistance = 0.01f;
    public float tolerance = 0.025f;

    public GvrTrackedController trackedController;

    private Transform hitObject;
    private BezierLineRenderer bezierRenderer;

    [Header("Exclude Layers")]
    public LayerMask excludeLayers;
    public VRBezierRaycasterDaydream.Callback raycastHitCallback;
    
    private GameObject anglePointPrefabInstance;
    private GameObject hitPointCursorPrefabInstance;
    private GameObject bezierPoint2;

    private bool objectIsAttached = false;

    private bool tap, swipeLeft, swipeRight, swipeUp, swipeDown;
    private bool isDragging;
    private Vector2 startTouch, swipeDelta;

    private Vector3 colliderSize;

    private Vector2 oldPosition, newPosition;
    private Quaternion oldRotation = new Quaternion(0, 0, 0, 0);
    private Vector3 desigredRotation;

    private Rigidbody hitObjectRigidbody;
    private SpringJoint anglePointPrefabAnglePoint;
    private Transform originalParentTransform;
    private GameObject tempGameObject;

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
        
        if (lineRenderer == null)
        {
            Debug.LogWarning("Assign a line renderer in the inspector!");
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
            lineRenderer.widthMultiplier = 0.02f;
        }
    }

    void Start()
    {
        Color c1 = Color.white;
        Color c2 = new Color(1, 1, 1, 1);
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = c2;
        lineRenderer.endColor = c1;
        lineRenderer.startWidth = 0.017f;
        lineRenderer.endWidth = 0.005f;
        Transform pointer = Pointer;
        bezierRenderer = new BezierLineRenderer(lineRenderer, interpolationPoints);

        oldRotation = this.transform.rotation;
    }

    Transform Pointer
    {
        get
        {
            if (trackedController != null)
            {
                return trackedController.transform;
            }
            Debug.Log("No Controller found ...");
            return null;
        }
    }

    private void Reset()
    {
        oldPosition = startTouch;
        startTouch = swipeDelta = new Vector2(0f, 0f);
        isDragging = false;
    }

    void Update()
    {
        //this is just for editor testing
        Transform pointer;
        if (testMode)
            pointer = this.transform;
        else
            pointer = Pointer;

        if (pointer == null)
        {
            return;
        }
        #region TouchControls

        if(hitObjectRigidbody != null)
        {
            tap = swipeLeft = swipeRight = swipeUp = swipeDown = false;
        
            if (trackedController.ControllerInputDevice.GetButton(GvrControllerButton.TouchPadTouch))
            {
                startTouch = trackedController.ControllerInputDevice.TouchPos;
                if (!isDragging)
                {
                    oldPosition = startTouch;
                }
                isDragging = true;
            }
            if(trackedController.ControllerInputDevice.GetButtonUp(GvrControllerButton.TouchPadTouch))
            {
                isDragging = false;
                Reset();
            }

            swipeDelta = new Vector2(0f, 0f);

            // Distance Calculation
            if(isDragging)
            {
                if (trackedController.ControllerInputDevice.GetButton(GvrControllerButton.TouchPadTouch))
                {
                    swipeDelta = startTouch - oldPosition;
                }
            }
        
            // Deadzone Crossing
            if (swipeDelta.magnitude > 0f)
            {
                // Which Direction?
                float x = swipeDelta.x;
                float y = swipeDelta.y;
                if(Mathf.Abs(x) > Mathf.Abs(y))
                {
                    if (x < 0)
                    {
                        swipeLeft = true;
                    }
                   
                    else{
                        swipeRight = true;
                    }
                    
                }
                else
                {
                    float predictedDistance = Vector3.Distance(transform.TransformPoint(anglePointPrefabInstance.transform.localPosition + new Vector3(0f, 0f, y) * zoomFactor), this.transform.position );
                    Vector3 predictedPosition = anglePointPrefabInstance.transform.localPosition + new Vector3(0f, 0f, y) * zoomFactor;

                    if (y < 0)
                    {
                        swipeDown = true;
                        if (anglePointPrefabInstance != null && hitObjectRigidbody != null)
                        {

                            if(predictedDistance >= (colliderSize.z/2) && predictedPosition.z > 0)
                            {
                                anglePointPrefabInstance.transform.localPosition += new Vector3(0f, 0f, y) * zoomFactor;
                                bezierPoint2.transform.localPosition += new Vector3(0f, 0f, y * (bezierPoint2PercentualPosition / 100) ) * zoomFactor;
                            }
                            else
                            {
                                anglePointPrefabInstance.transform.localPosition = this.transform.localPosition + new Vector3(0f, 0f, (colliderSize.z / 2) + closeOffset);
                                bezierPoint2.transform.localPosition = this.transform.localPosition + new Vector3(0f, 0f, anglePointPrefabInstance.transform.localPosition.z * (bezierPoint2PercentualPosition / 100) );
                            }
                               
                        }

                    }
                    else
                    {
                        swipeUp = true;
                        if (anglePointPrefabInstance != null && hitObjectRigidbody != null)
                        {
                            if (predictedDistance >= (colliderSize.z / 2)+closeOffset && predictedPosition.z > 0)
                            {
                                anglePointPrefabInstance.transform.localPosition += new Vector3(0f, 0f, y) * zoomFactor;
                                bezierPoint2.transform.localPosition += new Vector3(0f, 0f, y * (bezierPoint2PercentualPosition / 100)) * zoomFactor;
                            }
                            else
                            {
                                anglePointPrefabInstance.transform.localPosition = this.transform.localPosition + new Vector3(0f, 0f, (colliderSize.z / 2) + closeOffset);
                                bezierPoint2.transform.localPosition = this.transform.localPosition + new Vector3(0f, 0f, anglePointPrefabInstance.transform.localPosition.z * (bezierPoint2PercentualPosition / 100));
                            }
                        }

                    }
                }

            Reset();
            }
        }
        #endregion

        #region AttachControls
        if (objectIsAttached == false) {

            Ray laserPointer = new Ray(pointer.position, pointer.forward);

            if (lineRenderer != null)
            {
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, laserPointer.origin);
                lineRenderer.SetPosition(1, laserPointer.origin + laserPointer.direction * maxRayDistance);
            }


            RaycastHit hit;
            if (Physics.Raycast(laserPointer, out hit, maxRayDistance, ~excludeLayers))
            {
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(1, hit.point);
                }

                 if(trackedController.ControllerInputDevice.GetButtonDown(GvrControllerButton.TouchPadButton) == true && (hit.rigidbody != null) || (Input.GetKeyDown("e") && testMode && (hit.rigidbody != null)) )
                {

                    Debug.Log("Object hit, creating dependencies ...");
                    objectIsAttached = true;
                    hitObject = hit.transform;

                    originalParentTransform = hitObject.parent;
                    tempGameObject = new GameObject("Temporary GameObject");
                    tempGameObject.transform.position = hitObject.position;
                    tempGameObject.transform.rotation = hitObject.rotation;
                    tempGameObject.transform.parent = hitObject.parent;
                    hitObject.parent = tempGameObject.transform;


                    Debug.Log("Instantiate hitPointCursor and align it right...");
                    hitPointCursorPrefabInstance = Instantiate(hitPointCursorPrefab, hit.point, Quaternion.identity);
                    hitPointCursorPrefabInstance.transform.rotation = Quaternion.LookRotation(this.transform.position - hit.point);
                    hitPointCursorPrefabInstance.transform.parent = tempGameObject.transform;

                    Debug.Log("Creating spring anchor...");
                    anglePointPrefabInstance = Instantiate(anglePointPrefab, hit.point, Quaternion.identity);

                    Debug.Log("Aligning Spring...");
                    anglePointPrefabAnglePoint = anglePointPrefabInstance.GetComponent<SpringJoint>();
                    anglePointPrefabAnglePoint.autoConfigureConnectedAnchor = false;
                    anglePointPrefabAnglePoint.connectedBody = hitObject.GetComponent<Rigidbody>();

                    anglePointPrefabAnglePoint.connectedAnchor = hit.transform.InverseTransformPoint(hit.point);
                    anglePointPrefabAnglePoint.spring = springForce;
                    anglePointPrefabAnglePoint.damper = damperForce;
                    anglePointPrefabAnglePoint.minDistance = minDistance;
                    anglePointPrefabAnglePoint.maxDistance = maxDistance;
                    anglePointPrefabAnglePoint.tolerance = tolerance;
                    anglePointPrefabAnglePoint.anchor = new Vector3(0f, 0f, 0f);
                    anglePointPrefabInstance.transform.parent = this.transform;
                    anglePointPrefabInstance.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);


                    Debug.Log("Calculating collider Box of the hitObject...");
                    Collider collider = hitObject.GetComponent<Collider>();
                    colliderSize = collider.bounds.size;
                    Debug.Log("Size of collider " + colliderSize.z);

                    Debug.Log("Creating bezierPoint2... ");
                    bezierPoint2 = Instantiate(bezierPoint2Prefab, hit.point, Quaternion.identity);
                    float lineLength = Vector3.Distance(this.transform.position, hit.point);
                    Debug.Log("LineLength to Instantiate " + lineLength);
                    bezierPoint2.transform.parent = this.transform;
                    Vector3 originPosition = bezierPoint2.transform.localPosition;

                    //Approaching from AnglePointPrefab ...
                    bezierPoint2.transform.localPosition = bezierPoint2.transform.localPosition + new Vector3(0f, 0f, (-lineLength * (100-bezierPoint2PercentualPosition))/100);
                    bezierPoint2.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

                    Debug.Log("Get the rigidbody component...");
                    hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                    
                    Debug.Log("Tweaking the Rigidbody...");
                    hitObjectRigidbody.useGravity = true;
                    hitObjectRigidbody.drag = 10;
                    hitObjectRigidbody.freezeRotation = true;

                    oldRotation = this.transform.rotation;
                }

                if (raycastHitCallback != null)
                {
                    raycastHitCallback.Invoke(laserPointer, hit);
                }
            }
        }
        else
        {
            bezierRenderer.DrawLinearCurve(transform, bezierPoint2.transform, hitPointCursorPrefabInstance.transform, pointer);
            // Take the transform of the hitObject, and look where is connected Anchor of it is. Then transform the point into worldspace and set the hitobjectCursorPrefabInstance to its position.
            hitPointCursorPrefabInstance.transform.position = hitObject.TransformPoint(anglePointPrefabAnglePoint.connectedAnchor);
            // Set the Rotation of the hitPointCursorPrefabInstance
            hitPointCursorPrefabInstance.transform.rotation = Quaternion.LookRotation(this.transform.position - hitPointCursorPrefabInstance.transform.position);
            if (trackedController.ControllerInputDevice.GetButtonDown(GvrControllerButton.TouchPadButton) == true && (hitObject != null) || (Input.GetKeyDown("e") && testMode))
            {
                Debug.Log("Removing anchor from object...");
                hitObjectRigidbody.drag = 2;
                hitObjectRigidbody.constraints = RigidbodyConstraints.None;
                objectIsAttached = false;
                hitObjectRigidbody.useGravity = true;
                hitObject = null;
                Destroy(bezierPoint2);
                Destroy(anglePointPrefabInstance);
                Destroy(hitPointCursorPrefabInstance);
                hitObjectRigidbody = null;
                lineRenderer.positionCount = 2;
                isDragging = false;
            }
        }
        #endregion
    }

    void FixedUpdate()
    {

        if(hitObjectRigidbody != null && enableRotation)
        {
            
            Quaternion deltaRotation = this.transform.rotation * Quaternion.Inverse(oldRotation);
            hitObjectRigidbody.rotation = deltaRotation * hitObjectRigidbody.rotation;  
            oldRotation = this.transform.rotation;
        }
    }
}

