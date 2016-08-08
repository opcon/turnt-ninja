using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;
using OpenTK;
using ClipperLib;

namespace TurntNinja.Game
{
    class OnsetDrawing : IDisposable
    {
        /// <summary>
        /// Returns the first empty side for the current onset drawing
        /// </summary>
        public double CurrentOpeningAngle { get { return _sides[DrawingIndex].FindIndex(x => !x) * _angleBetweenSides[DrawingIndex]; } }

        /// <summary>
        /// This <see cref="OnsetCollection"/> holds all the onset
        /// timing and pulsing information
        /// </summary>
        private OnsetCollection _onsetCollecton;

        /// <summary>
        /// An array of angle between sides for each onset polygon
        /// </summary>
        private double[] _angleBetweenSides;
        
        /// <summary>
        /// An array of the impact distance for each onset polygon (generally constant)
        /// </summary>
        private double[] _impactDistances;

        /// <summary>
        /// An array of the number of sides for each onset polygon
        /// </summary>
        private int[] _numberOfSides;

        /// <summary>
        /// The outline width of each onset polygon
        /// </summary>
        private double _outlineWidth = 6;

        /// <summary>
        /// An array of the position of each onset polygon
        /// </summary>
        private PolarVector[] _positions;

        /// <summary>
        /// An array of the enabled sides of each onset polygon
        /// </summary>
        private List<bool>[] _sides;

        /// <summary>
        /// An array of the velocities of each onset polygon
        /// </summary>
        private PolarVector[] _velocities;

        /// <summary>
        /// An array of the widths of each onset polygon
        /// </summary>
        private double[] _widths;

        /// <summary>
        /// The number of drawable onsets
        /// </summary>
        public int DrawingCount { get; private set; }

        /// <summary>
        /// The shader program for this <see cref="OnsetDrawing"/>
        /// </summary>
        private ShaderProgram _shaderProgram;

        /// <summary>
        /// The data specification of the vertices
        /// </summary>
        private BufferDataSpecification _dataSpecification;

        /// <summary>
        /// The <see cref="VertexArray"/> for this <see cref="OnsetDrawing"/>
        /// </summary>
        private VertexArray _vertexArray;

        /// <summary>
        /// The <see cref="VertexBuffer"/> for this <see cref="OnsetDrawing"/>
        /// </summary>
        private VertexBuffer _vertexBuffer;

        public int DrawingIndex { get; private set; } = 0;

        private int _sumOfSides;
        private int _evenCount;
        private int _oddCount;

        private int _currentBeatEvenSum;
        private int _currentBeatOddSum;

        private float[] vertexList;
        private Vector2[] sideBoundsTemp;
        private Vector2[] _outlineBoundsTemp;

        private int _outlineOffset = 0;
        private int _evenCapOutlineCount = 0;
        private int _oddCapOutlineCount = 0;

        private double _impactBuffer = 25;

        /// <summary>
        /// How many beats to render ahead
        /// </summary>
        private const int RENDER_AHEAD_COUNT = 50;

        private const int MAX_SIDE_COUNT = 10;
        private const int VERTICES_PER_LINE = 6;
        private const int MAX_LINES_PER_SIDE = 5;
        private const int MAX_DRAWABLE_INDICIES = RENDER_AHEAD_COUNT * MAX_SIDE_COUNT * MAX_LINES_PER_SIDE * VERTICES_PER_LINE;

        public OnsetDrawing(OnsetCollection onsetCollection, ShaderProgram geometryShaderProgram)
        {
            _onsetCollecton = onsetCollection;
            _shaderProgram = geometryShaderProgram;

            DrawingCount = _onsetCollecton.Count;

            _angleBetweenSides = new double[_onsetCollecton.Count];
            _impactDistances = new double[_onsetCollecton.Count];
            _numberOfSides = new int[_onsetCollecton.Count];
            _positions = new PolarVector[_onsetCollecton.Count];
            _sides = new List<bool>[_onsetCollecton.Count];
            _velocities = new PolarVector[_onsetCollecton.Count];
            _widths = new double[_onsetCollecton.Count];

            vertexList = new float[MAX_DRAWABLE_INDICIES * 2];
            sideBoundsTemp = new Vector2[6];
            _outlineBoundsTemp = new Vector2[12];
        }

        public void AddOnsetDrawing(List<bool> sides, PolarVector velocity, double width, double minimumRadius, double impactTime)
        {
            _velocities[DrawingIndex] = velocity;
            var initialRadius = (impactTime * velocity.Radius + minimumRadius);
            _impactDistances[DrawingIndex] = minimumRadius;
            _positions[DrawingIndex] = new PolarVector(0, initialRadius);
            _widths[DrawingIndex] = width;

            _sides[DrawingIndex] = sides;
            _numberOfSides[DrawingIndex] = sides.Count;
            _angleBetweenSides[DrawingIndex] = GetAngleBetweenSides(_numberOfSides[DrawingIndex]);

            DrawingIndex += 1;
        }

        public void Update(double time, bool updateRadius, double azimuth)
        {
            //Set azimuth for all beats 
            for (int i = DrawingIndex; i < DrawingCount; i++)
            {
                _positions[i].Azimuth = azimuth;
            }
            //Update radius for all beats
            if (updateRadius)
                for (int i = DrawingIndex; i < DrawingCount; i++)
                {
                    _positions[i].Radius -= (time * _velocities[i].Radius);
                    if (_positions[i].Radius + _widths[i] <= _impactDistances[i] - _impactBuffer)
                        DrawingIndex++;
                }

            if (_vertexArray == null) InitialiseRendering();

            _vertexBuffer.Bind();
            _outlineOffset = BuildVertexList();
            _vertexBuffer.DrawableIndices = _outlineOffset * 2;
            _vertexBuffer.Initialise();
            _vertexBuffer.SetData(vertexList, _dataSpecification);
            _vertexBuffer.UnBind();
        }

        private void InitialiseRendering()
        {
            _dataSpecification = new BufferDataSpecification
            {
                Count = 2,
                Name = "in_position",
                Offset = 0,
                ShouldBeNormalised = false,
                Stride = 0,
                Type = VertexAttribPointerType.Float,
                SizeInBytes = sizeof(float)
            };

            _vertexArray = new VertexArray { DrawPrimitiveType = PrimitiveType.Triangles };
            _vertexArray.Bind();

            int totalVerticies = 0;
            for (int i = 0; i < DrawingCount; i++)
            {
                //Multiply by 6 because there are 6 vertices per quad (2 triangles with 3 vertices each)
                totalVerticies += _sides[i].Count(b => b) * 6;
            }

            _vertexBuffer = new VertexBuffer
            {
                BufferUsage = BufferUsageHint.StreamDraw,
                DrawableIndices = totalVerticies,
                MaxDrawableIndices = MAX_DRAWABLE_INDICIES
            };
            _vertexBuffer.AddSpec(_dataSpecification);
            _vertexBuffer.CalculateMaxSize();
            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();
            _vertexArray.Load(_shaderProgram, _vertexBuffer);
            _vertexArray.UnBind();
        }

        public void Draw(double time, int evenOrOdd)
        {
            if (_vertexArray == null) InitialiseRendering();
            if (evenOrOdd == 1)
            {
                _vertexArray.Draw(time, 0, _evenCount);
            }
            if (evenOrOdd == 2)
            {
                _vertexArray.Draw(time, _evenCount, _oddCount);
            }
        }

        public void DrawOutlines(double time, int evenOrOdd)
        {
            if (_vertexArray == null) InitialiseRendering();
            if (evenOrOdd == 1)
            {
                _vertexArray.Draw(time, _outlineOffset, _evenCount * 2);
                _vertexArray.Draw(time, _outlineOffset + (_evenCount + _oddCount) * 2, _evenCapOutlineCount);
            }
            if (evenOrOdd == 2)
            {
                _vertexArray.Draw(time, _outlineOffset + _evenCount * 2, _oddCount * 2);
                _vertexArray.Draw(time, _outlineOffset + (_evenCount + _oddCount) * 2 + _evenCapOutlineCount, _oddCapOutlineCount);
            }
        }

        public void DrawCurrentBeat(double time, int evenOrOdd)
        {
            if (evenOrOdd == 1) _vertexArray.Draw(time, 0, _currentBeatEvenSum);
            if (evenOrOdd == 2) _vertexArray.Draw(time, _evenCount, _currentBeatOddSum);
        }

        private int BuildVertexList()
        {
            int index = 0;
            _evenCount = 0;
            _oddCount = 0;
            _evenCapOutlineCount = 0;
            _oddCapOutlineCount = 0;
            _currentBeatEvenSum = 0;
            _currentBeatOddSum = 0;

            //If there are still beats left to draw, calculate exactly how many vertices are in the current beat
            if (DrawingIndex < DrawingCount)
            {
                for (int i = 0; i < _numberOfSides[DrawingIndex]; i++)
                {
                    if ((_sides[DrawingIndex])[i])
                    {
                        if (i % 2 == 0) _currentBeatEvenSum += 6;
                        else _currentBeatOddSum += 6;
                    }
                }
            }

            //generate vertexes for the even sides of the polygons
            for (int i = DrawingIndex; i < Math.Min(DrawingCount, DrawingIndex + RENDER_AHEAD_COUNT); i++)
            {
                for (int j = 0; j < _numberOfSides[i]; j += 2)
                {
                    if ((_sides[i])[j])
                    {
                        var bounds = GetSideBounds(i, j);
                        for (int k = 0; k < 6; k++)
                        {
                            vertexList[(index + k) * 2] = bounds[k].X;
                            vertexList[(index + k) * 2 + 1] = bounds[k].Y;
                        }
                        //bounds.CopyTo(verts, index);
                        _evenCount += 6;
                        index += 6;
                    }
                }
            }

            //generate vertexes for the odd sides of the polygons
            for (int i = DrawingIndex; i < Math.Min(DrawingCount, DrawingIndex + RENDER_AHEAD_COUNT); i++)
            {
                for (int j = 1; j < _numberOfSides[i]; j += 2)
                {
                    if ((_sides[i])[j])
                    {
                        var bounds = GetSideBounds(i, j);
                        for (int k = 0; k < 6; k++)
                        {
                            vertexList[(index + k) * 2] = bounds[k].X;
                            vertexList[(index + k) * 2 + 1] = bounds[k].Y;
                        }
                        //bounds.CopyTo(verts, index);
                        _oddCount += 6;
                        index += 6;
                    }
                }
            }

            var drawCount = index;

            //generate vertexes for the outlines of the even sides of the polygons
            for (int i = DrawingIndex; i < Math.Min(DrawingCount, DrawingIndex + RENDER_AHEAD_COUNT); i++)
            {
                for (int j = 0; j < _numberOfSides[i]; j += 2)
                {
                    if ((_sides[i])[j])
                    {
                        GetOutline(i, j);
                        for (int k = 0; k < 12; k++)
                        {
                            vertexList[(index + k) * 2] = _outlineBoundsTemp[k].X;
                            vertexList[(index + k) * 2 + 1] = _outlineBoundsTemp[k].Y;
                        }
                        index += 12;
                    }
                }
            }

            //generate vertexes for the outlines of the odd sides of the polygons
            for (int i = DrawingIndex; i < Math.Min(DrawingCount, DrawingIndex + RENDER_AHEAD_COUNT); i++)
            {
                for (int j = 1; j < _numberOfSides[i]; j += 2)
                {
                    if ((_sides[i])[j])
                    {
                        GetOutline(i, j);
                        for (int k = 0; k < 12; k++)
                        {
                            vertexList[(index + k) * 2] = _outlineBoundsTemp[k].X;
                            vertexList[(index + k) * 2 + 1] = _outlineBoundsTemp[k].Y;
                        }
                        index += 12;
                    }
                }
            }

            //generate vertices for cap outlines of the even sides of the polygons
            for (int i = DrawingIndex; i < Math.Min(DrawingCount, DrawingIndex + RENDER_AHEAD_COUNT); i++)
            {
                for (int j = 0; j < _numberOfSides[i]; j += 2)
                {
                    if ((_sides[i])[j])
                    {
                        int leftOrRight = -1;
                        if (!_sides[i][(j + 1 + _numberOfSides[i]) % _numberOfSides[i]])
                        {
                            if (!_sides[i][(j - 1 + _numberOfSides[i]) % _numberOfSides[i]])
                            {
                                leftOrRight = 2;
                            }
                            else
                            {
                                leftOrRight = 1;
                            }
                        }
                        else if (!_sides[i][(j - 1 + _numberOfSides[i]) % _numberOfSides[i]])
                        {
                            leftOrRight = 0;
                        }
                        else
                            continue;

                        GetEndCaps(i, j, leftOrRight);
                        int count = 6;
                        int l = 0;
                        if (leftOrRight == 2)
                            count = 12;
                        if (leftOrRight == 1)
                            l = 6;
                        for (int k = 0; k < count; k++)
                        {
                            vertexList[(index + k) * 2] = _outlineBoundsTemp[k + l].X;
                            vertexList[(index + k) * 2 + 1] = _outlineBoundsTemp[k + l].Y;
                        }
                        _evenCapOutlineCount += count;
                        index += count;
                    }
                }
            }

            //generate vertices for cap outlines of the odd sides of the polygons
            for (int i = DrawingIndex; i < Math.Min(DrawingCount, DrawingIndex + RENDER_AHEAD_COUNT); i++)
            {
                for (int j = 1; j < _numberOfSides[i]; j += 2)
                {
                    if ((_sides[i])[j])
                    {
                        int leftOrRight = -1;
                        if (!_sides[i][(j + 1 + _numberOfSides[i]) % _numberOfSides[i]])
                        {
                            if (!_sides[i][(j - 1 + _numberOfSides[i]) % _numberOfSides[i]])
                            {
                                leftOrRight = 2;
                            }
                            else
                            {
                                leftOrRight = 1;
                            }
                        }
                        else if (!_sides[i][(j - 1 + _numberOfSides[i]) % _numberOfSides[i]])
                        {
                            leftOrRight = 0;
                        }
                        else
                            continue;

                        GetEndCaps(i, j, leftOrRight);
                        int count = 6;
                        int l = 0;
                        if (leftOrRight == 2)
                            count = 12;
                        if (leftOrRight == 1)
                            l = 6;
                        for (int k = 0; k < count; k++)
                        {
                            vertexList[(index + k) * 2] = _outlineBoundsTemp[k + l].X;
                            vertexList[(index + k) * 2 + 1] = _outlineBoundsTemp[k + l].Y;
                        }
                        _oddCapOutlineCount += count;
                        index += count;
                    }
                }
            }

            return drawCount;
        }

        public Vector2[] GetSideBounds(int beatIndex, int sideIndex)
        {
            var sp = new PolarVector(_positions[beatIndex].Azimuth + sideIndex * _angleBetweenSides[beatIndex], Math.Max(_positions[beatIndex].Radius, _impactDistances[beatIndex]));
            var deltaWidth = Math.Max(_impactDistances[beatIndex] - _positions[beatIndex].Radius, 0);
            sideBoundsTemp[0] = PolarVector.ToCartesianCoordinates(sp);
            sideBoundsTemp[1] = PolarVector.ToCartesianCoordinates(sp, _angleBetweenSides[beatIndex], 0);
            sideBoundsTemp[2] = PolarVector.ToCartesianCoordinates(sp, _angleBetweenSides[beatIndex], _widths[beatIndex] - deltaWidth);
            sideBoundsTemp[3] = PolarVector.ToCartesianCoordinates(sp, _angleBetweenSides[beatIndex], _widths[beatIndex] - deltaWidth);
            sideBoundsTemp[4] = PolarVector.ToCartesianCoordinates(sp, 0, _widths[beatIndex] - deltaWidth);
            sideBoundsTemp[5] = PolarVector.ToCartesianCoordinates(sp);

            return sideBoundsTemp;
        }

        public void GetOutline(int beatIndex, int sideIndex)
        {
            var deltaWidth = Math.Max(_impactDistances[beatIndex] - _positions[beatIndex].Radius, 0);
            var pOuter = new PolarVector(_positions[beatIndex].Azimuth + sideIndex * _angleBetweenSides[beatIndex], Math.Max(_positions[beatIndex].Radius, _impactDistances[beatIndex] + _outlineWidth) + _widths[beatIndex] - deltaWidth);
            var pInner = new PolarVector(_positions[beatIndex].Azimuth + sideIndex * _angleBetweenSides[beatIndex], Math.Max(_positions[beatIndex].Radius, _impactDistances[beatIndex] + _outlineWidth));
            _outlineBoundsTemp[0] = PolarVector.ToCartesianCoordinates(pOuter);
            _outlineBoundsTemp[1] = PolarVector.ToCartesianCoordinates(pOuter, _angleBetweenSides[beatIndex], 0);
            _outlineBoundsTemp[2] = PolarVector.ToCartesianCoordinates(pOuter, _angleBetweenSides[beatIndex], _outlineWidth);
            _outlineBoundsTemp[3] = PolarVector.ToCartesianCoordinates(pOuter, _angleBetweenSides[beatIndex], _outlineWidth);
            _outlineBoundsTemp[4] = PolarVector.ToCartesianCoordinates(pOuter, 0, _outlineWidth);
            _outlineBoundsTemp[5] = PolarVector.ToCartesianCoordinates(pOuter);
            _outlineBoundsTemp[6] = PolarVector.ToCartesianCoordinates(pInner);
            _outlineBoundsTemp[7] = PolarVector.ToCartesianCoordinates(pInner, _angleBetweenSides[beatIndex], 0);
            _outlineBoundsTemp[8] = PolarVector.ToCartesianCoordinates(pInner, _angleBetweenSides[beatIndex], -_outlineWidth);
            _outlineBoundsTemp[9] = PolarVector.ToCartesianCoordinates(pInner, _angleBetweenSides[beatIndex], -_outlineWidth);
            _outlineBoundsTemp[10] = PolarVector.ToCartesianCoordinates(pInner, 0, -_outlineWidth);
            _outlineBoundsTemp[11] = PolarVector.ToCartesianCoordinates(pInner);
        }

        public void GetEndCaps(int beatIndex, int sideIndex, int leftOrRight)
        {
            var deltaWidth = Math.Max(_impactDistances[beatIndex] - _positions[beatIndex].Radius, 0);
            var pLeft = new PolarVector(_positions[beatIndex].Azimuth + sideIndex * _angleBetweenSides[beatIndex], Math.Max(_positions[beatIndex].Radius - _outlineWidth, _impactDistances[beatIndex]));
            var pRight = new PolarVector(_positions[beatIndex].Azimuth + sideIndex * _angleBetweenSides[beatIndex] + _angleBetweenSides[beatIndex], Math.Max(_positions[beatIndex].Radius - _outlineWidth, _impactDistances[beatIndex]));
            double dR = _outlineWidth * Math.Tan(MathHelper.DegreesToRadians(30));
            double dThetaInner = _outlineWidth / (pLeft.Radius + dR);
            double dThetaOuter = _outlineWidth / (pLeft.Radius + _widths[beatIndex] + dR);
            //left
            if (leftOrRight == 0 || leftOrRight == 2)
            {
                _outlineBoundsTemp[0] = PolarVector.ToCartesianCoordinates(pLeft);
                _outlineBoundsTemp[1] = PolarVector.ToCartesianCoordinates(pLeft, -dThetaInner, dR);
                _outlineBoundsTemp[2] = PolarVector.ToCartesianCoordinates(pLeft, -dThetaOuter, _widths[beatIndex] - deltaWidth + 2 * _outlineWidth + dR);
                _outlineBoundsTemp[3] = PolarVector.ToCartesianCoordinates(pLeft, -dThetaOuter, _widths[beatIndex] - deltaWidth + 2 * _outlineWidth + dR);
                _outlineBoundsTemp[4] = PolarVector.ToCartesianCoordinates(pLeft, 0, _widths[beatIndex] - deltaWidth + 2 * _outlineWidth);
                _outlineBoundsTemp[5] = PolarVector.ToCartesianCoordinates(pLeft);
            }
            //right
            if (leftOrRight == 1 || leftOrRight == 2)
            {
                _outlineBoundsTemp[6] = PolarVector.ToCartesianCoordinates(pRight);
                _outlineBoundsTemp[7] = PolarVector.ToCartesianCoordinates(pRight, dThetaInner, dR);
                _outlineBoundsTemp[8] = PolarVector.ToCartesianCoordinates(pRight, dThetaOuter, _widths[beatIndex] - deltaWidth + 2 * _outlineWidth + dR);
                _outlineBoundsTemp[9] = PolarVector.ToCartesianCoordinates(pRight, dThetaOuter, _widths[beatIndex] - deltaWidth + 2 * _outlineWidth + dR);
                _outlineBoundsTemp[10] = PolarVector.ToCartesianCoordinates(pRight, 0, _widths[beatIndex] - deltaWidth + 2 * _outlineWidth);
                _outlineBoundsTemp[11] = PolarVector.ToCartesianCoordinates(pRight);
            }
        }

        public List<List<IntPoint>> GetPolygonBounds(int beatIndex)
        {
            var polys = new List<List<IntPoint>>();
            for (int i = 0; i < _numberOfSides[beatIndex]; i++)
            {
                if ((_sides[beatIndex])[i]) polys.Add(SideBoundsToIntPoints(GetSideBounds(beatIndex, i)));
            }
            return polys;
        }


        public static double GetAngleBetweenSides(int numberOfSides)
        {
            return MathHelper.DegreesToRadians(360.0 / numberOfSides);
        }

        public static List<IntPoint> SideBoundsToIntPoints(Vector2[] bounds)
        {
            var ret = new List<IntPoint>();
            for (int i = 0; i < bounds.Length; i++)
            {
                if (i == 3 || i == 5) continue;
                ret.Add(new IntPoint(bounds[i].X, bounds[i].Y));
            }
            return ret;
        }

        public void Initialise()
        {
            DrawingIndex = 0;
        }

        public void Dispose()
        {
            _vertexArray.Dispose();
            _vertexBuffer.Dispose();
        }
    }
}
