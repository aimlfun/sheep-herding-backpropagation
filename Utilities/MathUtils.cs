using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SheepHerderAI.Utilities;

/// <summary>
///    _   _ _   _ _     
///   | | | | |_(_) |___ 
///   | | | | __| | / __|
///   | |_| | |_| | \__ \
///    \___/ \__|_|_|___/
///                      
/// Maths related utility functions.
/// </summary>
internal static class MathUtils
{
    /// <summary>
    /// Logic requires radians but we track angles in degrees, this converts.
    /// </summary>
    /// <param name="angle"></param>
    /// <returns></returns>
    internal static double DegreesInRadians(double angle)
    {
        return Math.PI * angle / 180;
    }

    /// <summary>
    /// Converts radians into degrees. 
    /// One could argue, WHY not just use degrees? Preference. Degrees are more intuitive than 2*PI offset values.
    /// </summary>
    /// <param name="radians"></param>
    /// <returns></returns>
    internal static double RadiansInDegrees(double radians)
    {
        // radians = PI * angle / 180
        // radians * 180 / PI = angle
        return radians * 180F / Math.PI;
    }

    /// <summary>
    /// Ensures value is between the min and max.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="val"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    internal static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
    {
        if (val.CompareTo(min) < 0)
        {
            return min;
        }

        if (val.CompareTo(max) > 0)
        {
            return max;
        }

        return val;
    }

    /// <summary>
    /// Ensures value is between the min and max.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="val"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    internal static float Clamp360(float val) 
    {
        while (val < 0) val += 360;
        while (val > 360) val -= 360;

        return val;
    }

    /// <summary>
    /// Detects if a point is withing a triangle.
    /// </summary>
    /// <param name="pointToTestIsInTriangle"></param>
    /// <param name="triangleVertex1"></param>
    /// <param name="triangleVertex2"></param>
    /// <param name="triangleVertex3"></param>
    /// <returns>true - point is within the triangle.</returns>
    public static bool PtInTriangle(PointF pointToTestIsInTriangle, PointF triangleVertex1, PointF triangleVertex2, PointF triangleVertex3)
    {
        double x2minusx = triangleVertex2.X - triangleVertex1.X;
        double det = x2minusx * (triangleVertex3.Y - triangleVertex1.Y) - (triangleVertex2.Y - triangleVertex1.Y) * (triangleVertex3.X - triangleVertex1.X);

        return det * (x2minusx * (pointToTestIsInTriangle.Y - triangleVertex1.Y) - (triangleVertex2.Y - triangleVertex1.Y) * (pointToTestIsInTriangle.X - triangleVertex1.X)) >= 0 &&
               det * ((triangleVertex3.X - triangleVertex2.X) * (pointToTestIsInTriangle.Y - triangleVertex2.Y) - (triangleVertex3.Y - triangleVertex2.Y) * (pointToTestIsInTriangle.X - triangleVertex2.X)) >= 0 &&
               det * ((triangleVertex1.X - triangleVertex3.X) * (pointToTestIsInTriangle.Y - triangleVertex3.Y) - (triangleVertex1.Y - triangleVertex3.Y) * (pointToTestIsInTriangle.X - triangleVertex3.X)) >= 0;
    }

    /// <summary>
    /// Computes the distance between 2 points using Pythagoras's theorem a^2 = b^2 + c^2.
    /// </summary>
    /// <param name="pt1">First point.</param>
    /// <param name="pt2">Second point.</param>
    /// <returns></returns>
    public static float DistanceBetweenTwoPoints(PointF pt1, PointF pt2)
    {
        float dx = pt2.X - pt1.X;
        float dy = pt2.Y - pt1.Y;

        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
  
    /// <summary>
    /// 
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="c"></param>
    /// <param name="closest"></param>
    /// <returns></returns>
    public static bool IsOnLine(PointF p0, PointF p1, PointF c, out PointF closest)
    {
        // calc delta distance: source point to line start
        var dx = c.X - p0.X;
        var dy = c.Y - p0.Y;

        // calc delta distance: line start to end
        var dxx = p1.X - p0.X;
        var dyy = p1.Y - p0.Y;

        // Calc position on line normalized between 0.00 & 1.00
        // == dot product divided by delta line distances squared
        var t = (dx * dxx + dy * dyy) / (dxx * dxx + dyy * dyy);

        // calc nearest pt on line
        var x = p0.X + dxx * t;
        var y = p0.Y + dyy * t;

        // clamp results to being on the segment
        if (t < 0) { x = p0.X; y = p0.Y; }
        if (t > 1) { x = p1.X; y = p1.Y; }

        closest = new PointF(x, y);

        return t >= 0 && t <= 1;
    }

    /// <summary>
    /// Returns true if the lines intersect, otherwise false. 
    /// In addition, if the lines intersect the intersection point is stored in the floats i_x and i_y.
    /// </summary>
    /// <param name="p0_x"></param>
    /// <param name="p0_y"></param>
    /// <param name="p1_x"></param>
    /// <param name="p1_y"></param>
    /// <param name="p2_x"></param>
    /// <param name="p2_y"></param>
    /// <param name="p3_x"></param>
    /// <param name="p3_y"></param>
    /// <param name="i_x"></param>
    /// <param name="i_y"></param>
    /// <returns></returns>
    public static bool GetLineIntersection(PointF p0,
                                           PointF p1,
                                           PointF p2,
                                           PointF p3,
                                           out PointF intersectionPoint)
    {
        float s1_x, s1_y, s2_x, s2_y;

        s1_x = p1.X - p0.X;
        s1_y = p1.Y - p0.Y;

        s2_x = p3.X - p2.X;
        s2_y = p3.Y - p2.Y;

        float s = (-s1_y * (p0.X - p2.X) + s1_x * (p0.Y - p2.Y)) / (-s2_x * s1_y + s1_x * s2_y);
        float t = (s2_x * (p0.Y - p2.Y) - s2_y * (p0.X - p2.X)) / (-s2_x * s1_y + s1_x * s2_y);

        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
        {
            // Collision detected
            intersectionPoint = new PointF(p0.X + t * s1_x, p0.Y + t * s1_y);

            return true;
        }

        intersectionPoint = new PointF(-999, -999);

        return false; // No collision
    }
}