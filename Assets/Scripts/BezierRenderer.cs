using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierRenderer {

    public LineRenderer lineRenderer = null;
    bool cubic = false;

    [Header("Use 3 Points for x^2 functions")]
    public Transform point0;
    public Transform point1;
    public Transform point2;
    int numPoints;
    private Vector3[] positions;

    private Vector3 lastKnownPosition;

    public bool isSet = false;

	public BezierRenderer(LineRenderer _lineRenderer) {
        lineRenderer = _lineRenderer;
        numPoints = 50;
        positions = new Vector3[numPoints];
        lineRenderer.positionCount = numPoints;
        //if ((point0 != null) && (point1 != null) && (point2 != null))
        //{
        //    cubic = true;
        //}
        
    }

    public void setPositionCounts()
    {
        lineRenderer.positionCount = numPoints;
    }

    public void DrawLinearCurve(Transform point0, Transform point1, Transform point2)
    {
    //    Debug.Log("Updating Bezier Curve ");
        for(int i = 1; i < numPoints + 1; ++i)
        {
            float t = i / (float)numPoints;

            //if (cubic == false)
            //    positions[i - 1] = CalculateLinearBezierPoint(t, point0.position, point1.position);

            //if (cubic == true)
            positions[i - 1] = CalculateLinearBezierPoint(t, CalculateLinearBezierPoint(t,point0.position, point1.position), CalculateLinearBezierPoint(t, point1.position, point2.position));
            lineRenderer.SetPositions(positions);
        }
    }

    private Vector3 CalculateLinearBezierPoint(float t, Vector3 p0, Vector3 p1)
    {
        return p0 + t * (p1 - p0);
    }


}
