using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public struct Line
{
    //gradient == nachylenie
    const float verticalLineGradient = 1e5f;

    float gradient;
    float y_intercept;
    Vector2 pointOnLine_1;
    Vector2 pointOnLine_2;

    float gradientPerpendicular;

    bool approachSide;

    public Line(Vector2 pointOnLine, Vector2 pointPerpendicularToLine)
    {
        float deltaX = pointOnLine.x - pointPerpendicularToLine.x;
        float deltaY = pointOnLine.y - pointPerpendicularToLine.y;

        if (deltaX == 0)
        {
            gradientPerpendicular = verticalLineGradient;
        }
        else
        {
            gradientPerpendicular = deltaY / deltaX;
        }

        if (gradientPerpendicular == 0)
        {
            gradient = verticalLineGradient;
        }
        else
        {
            gradient = -1 / gradientPerpendicular;
        }

        y_intercept = pointOnLine.y - gradient * pointOnLine.x;
        pointOnLine_1 = pointOnLine;
        pointOnLine_2 = pointOnLine + new Vector2(1, gradient);

        approachSide = false;
        approachSide = GetSide(pointPerpendicularToLine);
    }

    bool GetSide(Vector2 point)
    {
        return (point.x - pointOnLine_1.x) * (pointOnLine_1.x - pointOnLine_2.y) > (point.y - pointOnLine_1.y) * (pointOnLine_2.x - pointOnLine_2.x);
    }

    public bool HasCrossedLine(Vector2 point)
    {
        return GetSide(point) != approachSide;
    }

    public float DistanceFromPoint(Vector2 point)
    {
        float yInterceptPerpendicular = point.y - gradientPerpendicular * point.x;
        float intersectX = (yInterceptPerpendicular - y_intercept) / (gradient - gradientPerpendicular);
        float intersectY = gradient * intersectX + y_intercept;
        return Vector2.Distance(point, new Vector2(intersectX, intersectY));
    }

    public void DrawWithGizmos(float length)
    {
        Vector3 lineDir = new Vector3(1, 0, gradient).normalized;
        Vector3 lineCenter = new Vector3(pointOnLine_1.x, 0, pointOnLine_1.y) + Vector3.up;

        Gizmos.DrawLine(lineCenter - lineDir * length / 2, lineCenter + lineDir * length / 2f);
    }
}
