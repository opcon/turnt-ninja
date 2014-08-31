using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace Substructio.Core
{
    public class Polygon
    {
        private readonly List<Vector2> m_Edges = new List<Vector2>();
        private readonly List<Vector2> m_Points = new List<Vector2>();

        public float Height;
        public Vector2 Max;
        public Vector2 Min;
        public float Width;

        public Polygon(Polygon p)
        {
            foreach (Vector2 point in p.Points)
            {
                m_Points.Add(point);
            }
            BuildEdges();
            CalculateMaxMin();
        }

        public Polygon()
        {
            Min = new Vector2();
            Max = new Vector2();
        }

        public Polygon(List<Vector2> points)
        {
            m_Points = new List<Vector2>(points);
            BuildEdges();
            CalculateMaxMin();
        }

        public Polygon(Vector2 position, int width, int height)
        {
            float x = position.X;
            float y = position.Y;
            var points = new List<Vector2>();
            points.Add(position);
            points.Add(new Vector2(x, height + y));
            points.Add(new Vector2(x + width, height + y));
            points.Add(new Vector2(x + width, y));
            m_Points = points;
            BuildEdges();
            CalculateMaxMin();
        }

        public List<Vector2> Edges
        {
            get { return m_Edges; }
        }

        public List<Vector2> Points
        {
            get { return m_Points; }
        }

        public Vector2 Center
        {
            get
            {
                float totalX = 0;
                float totalY = 0;
                for (int i = 0; i < m_Points.Count; i++)
                {
                    totalX += m_Points[i].X;
                    totalY += m_Points[i].Y;
                }

                return new Vector2(totalX/m_Points.Count, totalY/m_Points.Count);
            }
        }

        public void Clear()
        {
            m_Points.Clear();
            m_Edges.Clear();
            Min = Max = Vector2.Zero;
        }

        public void CalculateMaxMin()
        {
            if (Points.Count == 0) return;
            Min = Max = Points[0];
            foreach (Vector2 point in Points)
            {
                if (point.X < Min.X) Min.X = point.X;
                if (point.X > Max.X) Max.X = point.X;
                if (point.Y < Min.Y) Min.Y = point.Y;
                if (point.Y > Max.Y) Max.Y = point.Y;
                Width = Max.X - Min.X;
                Height = Max.Y - Min.Y;
            }
        }

        public void AddPoint(Vector2 v)
        {
            m_Points.Add(v);
            if (m_Points.Count > 0)
            {
                BuildEdges();
            }
        }

        public void Scale(float x, float y)
        {
            Scale(new Vector2(x, y));
        }

        public void Scale(Vector2 scale)
        {
            Vector2 cent = Center;
            for (int i = 0; i < m_Points.Count; i++)
            {
                Vector2 p = m_Points[i];
                p -= cent;
                p = Vector2.Multiply(p, scale);
                p += cent;
                m_Points[i] = p;
            }
        }

        public bool IsPointInsidePolygon(Vector2 point)
        {
            //  The function will return YES if the point x,y is inside the polygon, or
            //  NO if it is not.  If the point is exactly on the edge of the polygon,
            //  then the function may return YES or NO.
            //
            //  Note Fucking stolen from here fags http://alienryderflex.com/polygon/
            //
            //  Note that division by zero is avoided because the division is protected
            // ote lol fags
            //  by the "if" clause which surrounds it.

            int i, j = m_Points.Count - 1;
            bool oddNodes = false;

            for (i = 0; i < m_Points.Count; i++)
            {
                if ((m_Points[i].Y < point.Y && m_Points[j].Y >= point.Y
                     || m_Points[j].Y < point.Y && m_Points[i].Y >= point.Y)
                    && (m_Points[i].X <= point.X || m_Points[j].X <= point.X))
                {
                    oddNodes ^= (m_Points[i].X +
                                 (point.Y - m_Points[i].Y)/(m_Points[j].Y - m_Points[i].Y)*
                                 (m_Points[j].X - m_Points[i].X) < point.X);
                }
                j = i;
            }

            return oddNodes;
        }

        public void Rebuild()
        {
            BuildEdges();
            CalculateMaxMin();
        }

        public void BuildEdges()
        {
            Vector2 p1;
            Vector2 p2;
            m_Edges.Clear();
            for (int i = 0; i < m_Points.Count; i++)
            {
                p1 = m_Points[i];
                if (i + 1 >= m_Points.Count)
                {
                    p2 = m_Points[0];
                }
                else
                {
                    p2 = m_Points[i + 1];
                }
                m_Edges.Add(p2 - p1);
            }
        }

        public void Offset(Vector2 v)
        {
            Offset(v.X, v.Y);
        }

        public void Offset(float x, float y)
        {
            for (int i = 0; i < m_Points.Count; i++)
            {
                Vector2 p = m_Points[i];
                m_Points[i] = new Vector2(p.X + x, p.Y + y);
            }
        }

        public override string ToString()
        {
            string result = "";

            foreach (Vector2 t in m_Points)
            {
                if (result != "") result += " ";
                result += "{" + t + "}";
            }

            return result;
        }

        public bool IsIntersecting(Polygon p)
        {
            PolygonCollisionResult polygonCollisionResult = PolygonCollision(this, p);
            return polygonCollisionResult.Intersect;
        }

        public static PolygonCollisionResult PolygonCollision(Polygon polygonA, Polygon polygonB)
        {
            return PolygonCollision(polygonA, polygonB, Vector2.Zero);
        }

        // Structure that stores the results of the PolygonCollision function

        // Check if polygon A is going to collide with polygon B for the given velocity
        public static PolygonCollisionResult PolygonCollision(Polygon polygonA, Polygon polygonB, Vector2 velocity)
        {
            var result = new PolygonCollisionResult {Intersect = true, WillIntersect = true};

            int edgeCountA = polygonA.Edges.Count;
            int edgeCountB = polygonB.Edges.Count;
            float minIntervalDistance = float.PositiveInfinity;
            var translationAxis = new Vector2();
            Vector2 edge;

            // Loop through all the edges of both polygons
            for (int edgeIndex = 0; edgeIndex < edgeCountA + edgeCountB; edgeIndex++)
            {
                edge = edgeIndex < edgeCountA ? polygonA.Edges[edgeIndex] : polygonB.Edges[edgeIndex - edgeCountA];

                // ===== 1. Find if the polygons are currently intersecting =====

                // Find the axis perpendicular to the current edge
                var axis = new Vector2(-edge.Y, edge.X);
                axis.Normalize();

                // Find the projection of the polygon on the current axis
                float minA = 0;
                float minB = 0;
                float maxA = 0;
                float maxB = 0;
                ProjectPolygon(axis, polygonA, ref minA, ref maxA);
                ProjectPolygon(axis, polygonB, ref minB, ref maxB);

                // Check if the polygon projections are currentlty intersecting
                if (IntervalDistance(minA, maxA, minB, maxB) > 0) result.Intersect = false;

                // ===== 2. Now find if the polygons *will* intersect =====

                // Project the velocity on the current axis
                float velocityProjection = Vector2.Dot(axis, velocity);

                // Get the projection of polygon A during the movement
                if (velocityProjection < 0)
                {
                    minA += velocityProjection;
                }
                else
                {
                    maxA += velocityProjection;
                }

                // Do the same test as above for the new projection
                float intervalDistance = IntervalDistance(minA, maxA, minB, maxB);
                if (intervalDistance > 0) result.WillIntersect = false;

                // If the polygons are not intersecting and won't intersect, exit the loop
                if (!result.Intersect && !result.WillIntersect) break;

                // Check if the current interval distance is the minimum one. If so store
                // the interval distance and the current distance.
                // This will be used to calculate the minimum translation vector
                intervalDistance = Math.Abs(intervalDistance);
                if (intervalDistance < minIntervalDistance)
                {
                    minIntervalDistance = intervalDistance;
                    translationAxis = axis;

                    Vector2 d = polygonA.Center - polygonB.Center;
                    if (Vector2.Dot(d, translationAxis) < 0) translationAxis = -translationAxis;
                }
            }

            // The minimum translation vector can be used to push the polygons appart.
            // First moves the polygons by their velocity
            // then move polygonA by MinimumTranslationVector.
            if (result.WillIntersect) result.MinimumTranslationVector = translationAxis*minIntervalDistance;
            result.IntervalDistance = minIntervalDistance;

            return result;
        }

        // Calculate the distance between [minA, maxA] and [minB, maxB]
        // The distance will be negative if the intervals overlap
        public static float IntervalDistance(float minA, float maxA, float minB, float maxB)
        {
            if (minA < minB)
            {
                return minB - maxA;
            }
            return minA - maxB;
        }

        // Calculate the projection of a polygon on an axis and returns it as a [min, max] interval
        public static void ProjectPolygon(Vector2 axis, Polygon polygon, ref float min, ref float max)
        {
            // To project a point on an axis use the dot product
            float d = Vector2.Dot(axis, polygon.Points[0]);
            min = d;
            max = d;
            foreach (Vector2 t in polygon.Points)
            {
                d = Vector2.Dot(t, axis);
                if (d < min)
                {
                    min = d;
                }
                else
                {
                    if (d > max)
                    {
                        max = d;
                    }
                }
            }
        }

        public void Draw()
        {
            if (Points == null || Points.Count == 0) return;
            //Begin((int)SpritePosition.X, (int)SpritePosition.Y, Width, Height, false);
            GL.Disable(EnableCap.Texture2D);
            GL.Begin(BeginMode.LineStrip);
            Color4 cl = Color4.Black;
            GL.Color4(cl);

            foreach (Vector2 point in Points)
            {
                GL.Vertex2(point);
            }
            GL.Vertex2(Points[0]);
            GL.End();
            GL.Enable(EnableCap.Texture2D);
        }

        #region Nested type: PolygonCollisionResult

        public struct PolygonCollisionResult
        {
            public bool Intersect; // Are the polygons currently intersecting
            public float IntervalDistance;

            public Vector2 MinimumTranslationVector;
            // The translation to apply to polygon A to push the polygons appart.

            public bool WillIntersect; // Are the polygons going to intersect forward in time?
        }

        #endregion
    }
}