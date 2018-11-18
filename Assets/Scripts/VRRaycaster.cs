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
    public GameObject cursorHitPointPrefab;
    private GameObject cursorHitPointPrefabInstance;
    private bool objectIsAttached = false;

    private bool tap, swipeLeft, swipeRight, swipeUp, swipeDown;
    private bool isDragging;
    private Vector2 startTouch, swipeDelta;

    private Vector2 oldPosition, newPosition;
    private float startTime, oldTime;
    Rigidbody hitObjectRigidbody;
    public float speed;
    private float journeyLength;
    private float fracJourney;
    private float hitObjectRigidbodyTmpMass;

    private Vector3 vel;
    private Vector3 oldRigidbodyPosition;
    private float baseSpeed;
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

                //Instantiating Objects needed
                 if(OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) == true && (hit.rigidbody != null) || (Input.GetKeyDown("e") && testMode ) )
                {
                    Debug.Log("Creating spring anchor...");
                    objectIsAttached = true;
                    hitObject = hit.transform;
                    anglePointPrefabInstance = Instantiate(anglePointPrefab, hit.transform.position, Quaternion.identity);
                    anglePointPrefabInstance.transform.parent = this.transform;

                    Debug.Log("Instantiate hitPointCursor and align it right...");
                    cursorHitPointPrefabInstance = Instantiate(cursorHitPointPrefab, hit.point, Quaternion.identity);
                    cursorHitPointPrefabInstance.transform.rotation = Quaternion.LookRotation(this.transform.position);

                    // Redundant?
                    cursorHitPointPrefabInstance.transform.position = hit.point;

                    cursorHitPointPrefabInstance.transform.parent = hitObject;
                    
                    //Removing Gravity for the moment..
                    hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                    oldRigidbodyPosition = hitObjectRigidbody.position;
                    hitObjectRigidbody.useGravity = false;
                    speed = calculateSpeedLevel(hitObjectRigidbody);
                }

                if (raycastHitCallback != null)
                {
                    raycastHitCallback.Invoke(laserPointer, hit);
                }
            }
        }
        else
        {
            bezierRenderer.DrawLinearCurve(transform, anglePointPrefabInstance.transform, cursorHitPointPrefabInstance.transform, pointer);

            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) == true && (hitObject != null) || (Input.GetKeyDown("e") && testMode))
            {
                Debug.Log("Removing anchor from object...");
                objectIsAttached = false;
                hitObjectRigidbody.useGravity = true;

                hitObjectRigidbody.AddForce(vel, ForceMode.Impulse);
                
                hitObjectRigidbody = null;
                hitObject = null;
                Destroy(anglePointPrefabInstance);
                Destroy(cursorHitPointPrefabInstance);
                journeyLength = 0f;
                fracJourney = 0f;
                lineRenderer.positionCount = 2;
            }

        }
        #endregion
    }

    float inverse_smoothstep(float x)
    {

        return Mathf.Sin(Mathf.Asin(x * 2.0f - 1.0f) / 3.0f) + 0.5f;
        // Hard Version
        // float a = Mathf.Acos(1.0f - 2.0f * x) / 3.0f;
        // return (1.0f + Mathf.Sin(a) * Mathf.Sqrt(3.0f) - Mathf.Cos(a)) / 2.0f;
    }

    void FixedUpdate()
    {
        if (hitObjectRigidbody != null)
        {
            journeyLength = Vector3.Distance(hitObject.position, anglePointPrefabInstance.transform.position);
            Debug.Log(journeyLength);
            if (journeyLength != 0) { 
                float distCovered = (Time.time - oldTime);
                fracJourney = distCovered / journeyLength;
                oldTime = Time.time;
                vel = (hitObjectRigidbody.position - oldRigidbodyPosition) / Time.deltaTime;
                oldRigidbodyPosition = hitObjectRigidbody.position;
                if (!System.Single.IsNaN(hitObjectRigidbody.position.x) && !System.Single.IsNaN(hitObjectRigidbody.position.y) && !System.Single.IsNaN(hitObjectRigidbody.position.z))
                    hitObjectRigidbody.position = Vector3.Lerp(hitObject.position, anglePointPrefabInstance.transform.position, inverse_smoothstep(fracJourney)* speed);
            }
        }
        if (hitObjectRigidbody != null && enableRotation)
        {
            hitObjectRigidbody.rotation = this.transform.rotation;
        }
    }

    private float calculateSpeedLevel(Rigidbody hitObject)
    {
        if (hitObject.mass <= 0.1)
            speed = 10;
        else if (hitObject.mass > 0.1 && hitObject.mass <= 0.3)
            speed = 8;
        else if (hitObject.mass > 0.3 && hitObject.mass <= 0.6)
            speed = 6;
        else if (hitObject.mass > 0.6 && hitObject.mass <= 0.8)
            speed = 4;
        else if (hitObject.mass > 0.8 && hitObject.mass <= 1)
            speed = 2;
        else
            speed = 1;
        return speed;
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

