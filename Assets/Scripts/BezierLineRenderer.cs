using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
