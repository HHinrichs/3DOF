using UnityEngine;
using UnityEngine.Events;

public class VRBezierRaycasterOculus : MonoBehaviour
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

    private Transform leftHandAnchor = null;
    private Transform rightHandAnchor = null;
    private Transform centerEyeAnchor = null;
    private Transform hitObject;
    private BezierLineRenderer bezierRenderer;
    public LayerMask excludeLayers;
    public VRBezierRaycasterOculus.Callback raycastHitCallback;
    
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

        tap = swipeLeft = swipeRight = swipeUp = swipeDown = false;

        if (OVRInput.Get(OVRInput.Touch.One))
        {
            startTouch = OVRInput.Get(OVRInput.Axis2D.PrimaryTouchpad);
            if (!isDragging)
            {
                oldPosition = startTouch;
            }
            isDragging = true;
        }
        if (OVRInput.GetUp(OVRInput.Touch.One))
        {
            isDragging = false;
            Reset();
        }

        swipeDelta = new Vector2(0f, 0f);

        // Distance Calculation
        if(isDragging)
        {
            if (OVRInput.Get(OVRInput.Touch.One))
            {
                swipeDelta = startTouch - oldPosition;
            }
        }
        
        // Deadzone Crossing
        if (swipeDelta.magnitude > 0f && hitObjectRigidbody != null)
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

                 if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) == true && (hit.rigidbody != null) || (Input.GetKeyDown("e") && testMode ) )
                {

                    Debug.Log("Object hit, creating dependencies ...");
                    objectIsAttached = true;
                    hitObject = hit.transform;

                    Debug.Log("Instantiate hitPointCursor and align it right...");
                    hitPointCursorPrefabInstance = Instantiate(hitPointCursorPrefab, hit.point, Quaternion.identity);
                    hitPointCursorPrefabInstance.transform.rotation = Quaternion.LookRotation(this.transform.position - hitPointCursorPrefab.transform.position);
                    hitPointCursorPrefabInstance.transform.parent = hitObject;

                    Debug.Log("Creating spring anchor...");
                    anglePointPrefabInstance = Instantiate(anglePointPrefab, hit.point, Quaternion.identity);
                    
                    Debug.Log("Aligning Spring...");
                    SpringJoint anglePointPrefabAnglePoint = anglePointPrefabInstance.GetComponent<SpringJoint>();
                    anglePointPrefabAnglePoint.autoConfigureConnectedAnchor = false;
                    anglePointPrefabAnglePoint.connectedBody = hitObject.GetComponent<Rigidbody>();
                    anglePointPrefabAnglePoint.connectedAnchor = hitPointCursorPrefabInstance.transform.localPosition;
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
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) == true && (hitObject != null) || (Input.GetKeyDown("e") && testMode))
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

