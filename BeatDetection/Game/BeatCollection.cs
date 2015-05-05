using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Core;
using ClipperLib;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;

namespace BeatDetection.Game
{
    class BeatCollection
    {
        public readonly int Count;
        public int Index { get; private set; }

        public PolarVector[] Positions;
        public PolarVector[] Velocities;
        public double[] ImpactDistances;
        public double[] Widths;
        public PulseData[] PulseDatas; //incorrect grammar.

        public bool[] Destroy;

        public List<bool>[] Sides;
        public int[] NumberOfSides;
        public double[] AngleBetweenSides;

        public int[] ColourIndex;

        private ShaderProgram _shaderProgram;
        private VertexBuffer _vertexBuffer;
        private VertexArray _vertexArray;
        private BufferDataSpecification _dataSpecification;

        private int _sumOfSides;
        private int _evenCount;
        private int _oddCount;

        private int _currentBeatEvenSum;
        private int _currentBeatOddSum;

        public int BeatsHit { get; private set; }
        public bool BeginPulse { get; private set; }
        public bool Pulsing { get; private set; }
        public double CurrentOpeningAngle {get { return Sides[Index].FindIndex(x => !x)*AngleBetweenSides[Index]; }}

        public const int RenderAheadCount = 20;
        public readonly int MaxDrawableIndices;

        private float[] vertexList;
        private Vector2[] sideBoundsTemp;

        public BeatCollection(int beatCount, ShaderProgram geometryShaderProgram)
        {
            Count = beatCount;
            _shaderProgram = geometryShaderProgram;

            MaxDrawableIndices = RenderAheadCount*6*6;
            vertexList = new float[MaxDrawableIndices*2];
            sideBoundsTemp = new Vector2[6];

            Index = 0;
            Positions = new PolarVector[Count];
            Velocities = new PolarVector[Count];
            ImpactDistances = new double[Count];
            Widths = new double[Count];
            PulseDatas = new PulseData[Count];
            Destroy = new bool[Count];
            Sides = new List<bool>[Count];
            NumberOfSides = new int[Count];
            AngleBetweenSides = new double[Count];
            ColourIndex = new int[Count];
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
                Type = VertexAttribPointerType.Float
            };

            _vertexArray = new VertexArray { DrawPrimitiveType = PrimitiveType.Triangles };
            _vertexArray.Bind();

            for (int i = 0; i < Count; i++)
            {
                //Multiply by 6 because there are 6 vertices per quad (2 triangles with 3 vertices each)
                _sumOfSides += Sides[i].Count(b => b)*6;
            }

            _vertexBuffer = new VertexBuffer
            {
                BufferUsage = BufferUsageHint.StreamDraw,
                DrawableIndices = _sumOfSides
            };
            _vertexBuffer.AddSpec(_dataSpecification);
            _vertexBuffer.Bind();
            _vertexBuffer.Initialise();

            _vertexArray.Load(_shaderProgram, _vertexBuffer);
            _vertexArray.UnBind();
        }

        public void AddBeat(List<bool> sides, PolarVector velocity, double width, double minimumRadius, double impactTime)
        {
            Velocities[Index] = velocity;
            var initialRadius = (impactTime*velocity.Radius + minimumRadius);
            ImpactDistances[Index] = minimumRadius;
            Positions[Index] = new PolarVector(0, initialRadius);
            Widths[Index] = width;

            Sides[Index] = sides;
            NumberOfSides[Index] = sides.Count;
            AngleBetweenSides[Index] = GetAngleBetweenSides(NumberOfSides[Index]);

            ColourIndex[Index] = -1;

            PulseDatas[Index] = new PulseData {PulseDirection = 1, PulseMultiplier = 150, PulseWidth = 0, PulseWidthMax = 25, Pulsing = false};

            Index += 1;
        }

        public void Update(double time, bool updateRadius, double azimuth)
        {
            Index += BeatsHit;
            if (BeginPulse)
            {
                BeginPulse = false;
                Pulsing = true;
            }
            if (BeatsHit > 0)
            {
                BeginPulse = false;
                Pulsing = false;
            }
            BeatsHit = 0;
            BeginPulse = false;

            //Set azimuth for all beats 
            for (int i = Index; i < Count; i++)
            {
                Positions[i].Azimuth = azimuth;
            }
            //Update radius for all beats
            if (updateRadius)
                for (int i = Index; i < Count; i++)
                {
                    Positions[i].Radius -= (time*Velocities[i].Radius);
                }
            //Check for any 'hit' beats
            if (updateRadius)
                for (int i = Index; i < Count; i++)
                {
                    if (Positions[i].Radius <= ImpactDistances[i] - 25) BeatsHit++;
                }
            //Check if need to pulse center polygon
            for (int i = Index; i < Count; i++)
            {
                if (BeatWithinPulseRadius(i) && !Pulsing) BeginPulse = true;
            }

            if (_vertexArray == null) InitialiseRendering();

            _vertexBuffer.Bind();
            _vertexBuffer.DrawableIndices = BuildVertexList();
            _vertexBuffer.Initialise();
            _vertexBuffer.SetData(vertexList, _dataSpecification);
            _vertexBuffer.UnBind();
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

        public void DrawCurrentBeat(double time, int evenOrOdd)
        {
            if (evenOrOdd == 1) _vertexArray.Draw(time, 0, _currentBeatEvenSum);
            if (evenOrOdd == 2) _vertexArray.Draw(time, _evenCount, _currentBeatOddSum);
        }

        private int BuildVertexList()
        {
            var verts = new Vector2[MaxDrawableIndices];
            int index = 0;
            _evenCount = 0;
            _oddCount = 0;
            _currentBeatEvenSum = 0;
            _currentBeatOddSum = 0;

            //If there are still beats left to draw, calculate exactly how many vertices are in the current beat
            if (Index < Count)
            {
                for (int i = 0; i < NumberOfSides[Index]; i++)
                {
                    if ((Sides[Index])[i])
                    {
                        if (i % 2 == 0) _currentBeatEvenSum += 6;
                        else _currentBeatOddSum += 6;
                    }
                }
            }

            for (int i = Index; i < Math.Min(Count, Index+RenderAheadCount); i++)
            {
                for (int j = 0; j < NumberOfSides[i]; j += 2)
                {
                    if ((Sides[i])[j])
                    {
                        var bounds = GetSideBounds(i, j);
                        for (int k = 0; k < 6; k++)
                        {
                            vertexList[(index + k)*2] = bounds[k].X;
                            vertexList[(index + k)*2 + 1] = bounds[k].Y;
                        }
                        //bounds.CopyTo(verts, index);
                        _evenCount += 6;
                        index += 6;
                    }
                }


            }

            for (int i = Index; i < Math.Min(Count, Index + RenderAheadCount); i++)
            {
                for (int j = 1; j < NumberOfSides[i]; j += 2)
                {
                    if ((Sides[i])[j])
                    {
                        var bounds = GetSideBounds(i, j);
                        for (int k = 0; k < 6; k++)
                        {
                            vertexList[(index + k)*2] = bounds[k].X;
                            vertexList[(index + k)*2 + 1] = bounds[k].Y;
                        }
                        //bounds.CopyTo(verts, index);
                        _oddCount += 6;
                        index += 6;
                    }
                }
            }

            return index;

            //return verts.Take(index).SelectMany(v => new[] { v.X, v.Y });
        }

        public Vector2[] GetSideBounds(int beatIndex, int sideIndex)
        {
            var sp = new PolarVector(Positions[beatIndex].Azimuth + sideIndex*AngleBetweenSides[beatIndex], Positions[beatIndex].Radius);
            sideBoundsTemp[0] = PolarVector.ToCartesianCoordinates(sp);
            sideBoundsTemp[1] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[beatIndex], 0);
            sideBoundsTemp[2] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[beatIndex], Widths[beatIndex] + PulseDatas[beatIndex].PulseWidth);
            sideBoundsTemp[3] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[beatIndex], Widths[beatIndex] + PulseDatas[beatIndex].PulseWidth);
            sideBoundsTemp[4] = PolarVector.ToCartesianCoordinates(sp, 0, Widths[beatIndex] + PulseDatas[beatIndex].PulseWidth);
            sideBoundsTemp[5] = PolarVector.ToCartesianCoordinates(sp);

            return sideBoundsTemp;
        }

        public List<List<IntPoint>> GetPolygonBounds(int beatIndex)
        {
            var polys = new List<List<IntPoint>>();
            for (int i = 0; i < NumberOfSides[beatIndex]; i++)
            {
                if ((Sides[beatIndex])[i]) polys.Add(SideBoundsToIntPoints(GetSideBounds(beatIndex, i)));
            }
            return polys;
        }

        private bool BeatWithinPulseRadius(int index)
        {
            return ((Positions[index].Radius - ImpactDistances[index])/Velocities[index].Radius < (PulseDatas[index].PulseWidthMax/PulseDatas[index].PulseMultiplier));
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
            Index = 0;
        }
    }
}
