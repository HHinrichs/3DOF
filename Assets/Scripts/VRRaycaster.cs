using UnityEngine;
using UnityEngine.Events;

public class VRRaycaster : MonoBehaviour
{

    [System.Serializable]
    public class Callback : UnityEvent<Ray, RaycastHit> { }

    private Transform leftHandAnchor = null;
    private Transform rightHandAnchor = null;
    private Transform centerEyeAnchor = null;
    public LineRenderer lineRenderer = null;
    public float maxRayDistance = 500.0f;
    public LayerMask excludeLayers;
    public VRRaycaster.Callback raycastHitCallback;
    public int interpolationPoints;

    private Transform hitObject;
    private BezierLineRenderer bezierRenderer;
    public GameObject anglePointPrefab;
    private GameObject anglePointPrefabInstance;
    public GameObject hitPointCursorPrefab;
    private GameObject hitPointCursorPrefabInstance;
    private bool objectIsAttached = false;

    private bool tap, swipeLeft, swipeRight, swipeUp, swipeDown;
    private bool isDragging;
    private Vector2 startTouch, swipeDelta;

    private Vector2 oldPosition, newPosition;

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
        bezierRenderer = new BezierLineRenderer(lineRenderer, interpolationPoints, pointer);
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

    bool tached = false;
    void Update()
    {
        Transform pointer = Pointer;
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

             //           if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) == true && (hit.rigidbody != null))
                if (tached == false)
                {
                    Debug.Log("Creating sprint anchor...");
                    objectIsAttached = true;
                    hitObject = hit.transform;
                    anglePointPrefabInstance = Instantiate(anglePointPrefab, hit.transform.position, Quaternion.identity);
                    anglePointPrefabInstance.transform.parent = this.transform;
                    anglePointPrefabInstance.GetComponent<SpringJoint>().connectedBody = hitObject.GetComponent<Rigidbody>();

                    Debug.Log("Instantiate hitPointCursor and align it right...");
                    hitPointCursorPrefabInstance = Instantiate(hitPointCursorPrefab, hit.point, Quaternion.identity);
                    hitPointCursorPrefabInstance.transform.rotation = Quaternion.LookRotation(this.transform.position);
                    hitPointCursorPrefabInstance.transform.parent = hitObject;
                    hitPointCursorPrefabInstance.transform.localPosition = new Vector3(hitPointCursorPrefabInstance.transform.localPosition.x, 
                                                                                        hitPointCursorPrefabInstance.transform.localPosition.y, 
                                                                                        -(hitObject.position.z*hitObject.localScale.z)/2);
                    Debug.Log(anglePointPrefabInstance.)

                    Debug.Log("Creating bezier Line point 2 ");
                    Debug.Log("Creating bezier Point in direction of the GameObject and the controller");
           //         Instantiate(new GameObject("BezierPoint2"), )
                    // maxRayDistance = hit.distance;
                    
                    lineRenderer.SetPosition(0, new Vector3(0f, 0f, 0f));
                    lineRenderer.SetPosition(1, new Vector3(0f, 0f, 0f));
                    tached = true;
                    bezierRenderer.setPositionCounts();
                    hitObjectRigidbody = hitObject.GetComponent<Rigidbody>();
                }

                if (raycastHitCallback != null)
                {
                    raycastHitCallback.Invoke(laserPointer, hit);
                }
            }
        }
        else
        {
            bezierRenderer.DrawLinearCurve(transform, anglePointPrefabInstance.transform, hitPointCursorPrefabInstance.transform);
      //      hitObjectRigidbody.rotation = this.transform.rotation;
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) == true && (hitObject != null))
            {
                Debug.Log("Removing anchor from object...");
                objectIsAttached = false;
                hitObject = null;
                anglePointPrefabInstance.GetComponent<SpringJoint>().connectedBody = null;
                anglePointPrefabInstance.transform.parent = null;
                anglePointPrefabInstance = null;
                hitPointCursorPrefabInstance.transform.parent = null;
                Destroy(hitPointCursorPrefabInstance);
                lineRenderer.positionCount = 2;
            }
        }
        #endregion
    }
}

public class BezierLineRenderer
{

    public LineRenderer lineRenderer = null;
    bool cubic = false;

    [Header("Use 3 Points for x^2 functions")]
    public Transform point0;
    public Transform point1;
    public Transform point2;
    int interpolationPoints;
    private Vector3[] positions;
    Transform pointer;
    public bool isSet = false;

    public BezierLineRenderer(LineRenderer _lineRenderer, int _interpolationPoints, Transform _pointer)
    {
        pointer = _pointer;
        lineRenderer = _lineRenderer;
        interpolationPoints = _interpolationPoints;
        positions = new Vector3[interpolationPoints];
        lineRenderer.positionCount = interpolationPoints;
    }

    public void setPositionCounts()
    {
        lineRenderer.positionCount = interpolationPoints;
    }

    public void DrawLinearCurve(Transform point0, Transform point1, Transform point2)
    {
        positions[0] = pointer.position;
        //    Debug.Log("Updating Bezier Curve ");
        for (int i = 1; i < interpolationPoints; ++i)
        {
            float t = i / (float)interpolationPoints;
            positions[i] = CalculateLinearBezierPoint(t, CalculateLinearBezierPoint(t, point0.position, point1.position), CalculateLinearBezierPoint(t, point1.position, point2.position));
            lineRenderer.SetPositions(positions);
        }
    }

    private Vector3 CalculateLinearBezierPoint(float t, Vector3 p0, Vector3 p1)
    {
        return p0 + t * (p1 - p0);
    }


}

