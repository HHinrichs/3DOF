using UnityEngine;
using UnityEngine.Events;

public class VRRaycaster : MonoBehaviour
{

    [System.Serializable]
    public class Callback : UnityEvent<Ray, RaycastHit> { }

    public bool testMode = false;

    private Transform leftHandAnchor = null;
    private Transform rightHandAnchor = null;
    private Transform centerEyeAnchor = null;
    public LineRenderer lineRenderer = null;
    public float maxRayDistance = 500.0f;
    public LayerMask excludeLayers;
    public VRRaycaster.Callback raycastHitCallback;
    public int interpolationPoints;
    public bool enableRotation = false;
    private Transform hitObject;
    private BezierLineRenderer bezierRenderer;
    public GameObject anglePointPrefab;
    private GameObject anglePointPrefabInstance;
    public GameObject hitPointCursorPrefab;
    private GameObject hitPointCursorPrefabInstance;
    private GameObject bezierPoint2;
    private bool objectIsAttached = false;

    private bool tap, swipeLeft, swipeRight, swipeUp, swipeDown;
    private bool isDragging;
    private Vector2 startTouch, swipeDelta;

    private Vector2 oldPosition, newPosition;
    private Quaternion oldRotation = new Quaternion(0, 0, 0, 0);
    private Vector3 desigredRotation;
    Rigidbody hitObjectRigidbody;
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
            Color c1 = Color.white;
            Color c2 = new Color(1, 1, 1, 1);
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = c2;
            lineRenderer.endColor = c1;
            lineRenderer.startWidth = 0.017f;
            lineRenderer.endWidth = 0.017f;
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
        lineRenderer.endWidth = 0.017f;
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
                if (y < 0)
                {
                    swipeDown = true;
                    if(anglePointPrefabInstance != null)
                    {
                        Debug.Log(y+" "+ swipeDelta.magnitude);
                        Debug.Log("y < 0");
                        anglePointPrefabInstance.transform.localPosition += new Vector3(0f, 0f, y)*2;
                    }
                        
                    
                }
                else
                {
                    swipeUp = true;
                    if (anglePointPrefabInstance != null)
                    {
                        Debug.Log(y+ " " + swipeDelta.magnitude);
                        Debug.Log("y > 0");
                        anglePointPrefabInstance.transform.localPosition += new Vector3(0f, 0f, y) * 2;
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
                    hitPointCursorPrefabInstance.transform.rotation = Quaternion.LookRotation(this.transform.position);
                   // hitPointCursorPrefabInstance.transform.position = hit.point;
                    hitPointCursorPrefabInstance.transform.parent = hitObject;

                    Debug.Log("Creating spring anchor...");
                    anglePointPrefabInstance = Instantiate(anglePointPrefab, hit.point, Quaternion.identity);
                    
                    Debug.Log("Aligning Spring...");
                    SpringJoint anglePointPrefabAnglePoint = anglePointPrefabInstance.GetComponent<SpringJoint>();
                    anglePointPrefabAnglePoint.autoConfigureConnectedAnchor = false;
                    anglePointPrefabAnglePoint.connectedBody = hitObject.GetComponent<Rigidbody>();
                    anglePointPrefabAnglePoint.connectedAnchor = hitPointCursorPrefabInstance.transform.localPosition;
                    Debug.Log(anglePointPrefabInstance.transform.InverseTransformPoint(hit.point));
                    anglePointPrefabAnglePoint.anchor = new Vector3(0f, 0f, 0f);

                //           anglePointPrefabAnglePoint.anchor = anglePointPrefabInstance.transform.InverseTransformVector(hit.point);
                    anglePointPrefabInstance.transform.parent = this.transform;


                    Debug.Log("Calculating collider Box of the hitObject...");
                    Collider collider = hitObject.GetComponent<Collider>();
                    Vector3 colliderSize = collider.bounds.size;

                    Debug.Log("Creating bezierPoint2... ");
                    bezierPoint2 = Instantiate(new GameObject("bezierPoint2"), hit.point, Quaternion.identity);
                    bezierPoint2.transform.localScale = new Vector3(0.0001f, 0.0001f, 0.0001f);
                    bezierPoint2.transform.parent = this.transform;
                    bezierPoint2.transform.localPosition = bezierPoint2.transform.localPosition + new Vector3(0f, 0f, -colliderSize.z);

                    Debug.Log("Get the rigidbody component...");
                    hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                    hitObjectRigidbody.useGravity = false;
                    Debug.Log("Freezing Rotations...");
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

public class BezierLineRenderer
{

    public LineRenderer lineRenderer = null;
    bool cubic = false;

    [Header("Use 3 Points for x^2 functions")]
    private Transform point0;
    private Transform point1;
    private Transform point2;
    int interpolationPoints;
    private Vector3[] positions;
    public bool isSet = false;

    public BezierLineRenderer(LineRenderer _lineRenderer, int _interpolationPoints)
    {
        lineRenderer = _lineRenderer;
        interpolationPoints = _interpolationPoints;
        positions = new Vector3[interpolationPoints];
        lineRenderer.positionCount = interpolationPoints;
    }

    public void setPositionCounts()
    {
        lineRenderer.positionCount = interpolationPoints;
    }

    public void DrawLinearCurve(Transform _point0, Transform _point1, Transform _point2, Transform _pointer)
    {
        point0 = _point0;
        point1 = _point1;
        point2 = _point2;
        positions[0] = _pointer.position;
        for (int i = 1; i < interpolationPoints; ++i)
        {
            float t = i / (float)interpolationPoints;
            positions[i] = CalculateLinearBezierPoint(t, CalculateLinearBezierPoint(t, point0.position, point1.position), CalculateLinearBezierPoint(t, point1.position, point2.position)); 
        }
        setPositionCounts();
        lineRenderer.SetPositions(positions);
    }

    private Vector3 CalculateLinearBezierPoint(float t, Vector3 p0, Vector3 p1)
    {
        return p0 + t * (p1 - p0);
    }


}

