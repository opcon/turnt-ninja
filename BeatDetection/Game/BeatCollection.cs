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
        public double CurrentOpeningAngle {get { return Sides[Index].FindIndex(x => x == false)*AngleBetweenSides[Index]; }}

        public const int RenderAheadCount = 20;
        public readonly int MaxDrawableIndices;

        public BeatCollection(int beatCount, ShaderProgram geometryShaderProgram)
        {
            Count = beatCount;
            _shaderProgram = geometryShaderProgram;

            MaxDrawableIndices = RenderAheadCount*6*6;

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
            var initialRadius = (impactTime*velocity.Radius + minimumRadius + 20);
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
                    if (Positions[i].Radius <= ImpactDistances[i]) BeatsHit++;
                }
            //Check if need to pulse center polygon
            for (int i = Index; i < Count; i++)
            {
                if (BeatWithinPulseRadius(i) && !Pulsing) BeginPulse = true;
            }

            if (_vertexArray == null) InitialiseRendering();

            _vertexBuffer.Bind();
            var data = BuildVertexList();
            _vertexBuffer.DrawableIndices = data.Count()/2;
            _vertexBuffer.Initialise();
            _vertexBuffer.SetData(data, _dataSpecification);
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

        private IEnumerable<float> BuildVertexList()
        {
            var verts = new Vector2[MaxDrawableIndices];
            int index = 0;
            _evenCount = 0;
            _oddCount = 0;
            _currentBeatEvenSum = 0;
            _currentBeatOddSum = 0;

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
                        bounds.CopyTo(verts, index);
                        //var sp = new PolarVector(Positions[i].Azimuth + j*AngleBetweenSides[i], Positions[i].Radius);
                        //verts[index] = PolarVector.ToCartesianCoordinates(sp);
                        //verts[index + 1] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[i], 0);
                        //verts[index + 2] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[i], Widths[i] + PulseDatas[i].PulseWidth);
                        //verts[index + 3] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[i], Widths[i] + PulseDatas[i].PulseWidth);
                        //verts[index + 4] = PolarVector.ToCartesianCoordinates(sp, 0, Widths[i] + PulseDatas[i].PulseWidth);
                        //verts[index + 5] = PolarVector.ToCartesianCoordinates(sp);
                        _evenCount += 6;
                        index += 6;
                    }
                }
            }

            for (int i = Index; i < Math.Min(Count, Index+RenderAheadCount); i++)
            {
                for (int j = 1; j < NumberOfSides[i]; j += 2)
                {
                    if ((Sides[i])[j])
                    {
                        var bounds = GetSideBounds(i, j);
                        bounds.CopyTo(verts, index);
                        //var sp = new PolarVector(Positions[i].Azimuth + j * AngleBetweenSides[i], Positions[i].Radius);
                        //verts[index] = PolarVector.ToCartesianCoordinates(sp);
                        //verts[index + 1] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[i], 0);
                        //verts[index + 2] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[i], Widths[i] + PulseDatas[i].PulseWidth);
                        //verts[index + 3] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[i], Widths[i] + PulseDatas[i].PulseWidth);
                        //verts[index + 4] = PolarVector.ToCartesianCoordinates(sp, 0, Widths[i] + PulseDatas[i].PulseWidth);
                        //verts[index + 5] = PolarVector.ToCartesianCoordinates(sp);
                        _oddCount+= 6;
                        index += 6;
                    }
                }
            }

            return verts.Take(index).SelectMany(v => new[] { v.X, v.Y });
        }

        public Vector2[] GetSideBounds(int beatIndex, int sideIndex)
        {
            var ret = new Vector2[6];
            var sp = new PolarVector(Positions[beatIndex].Azimuth + sideIndex*AngleBetweenSides[beatIndex], Positions[beatIndex].Radius);
            ret[0] = PolarVector.ToCartesianCoordinates(sp);
            ret[1] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[beatIndex], 0);
            ret[2] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[beatIndex], Widths[beatIndex] + PulseDatas[beatIndex].PulseWidth);
            ret[3] = PolarVector.ToCartesianCoordinates(sp, AngleBetweenSides[beatIndex], Widths[beatIndex] + PulseDatas[beatIndex].PulseWidth);
            ret[4] = PolarVector.ToCartesianCoordinates(sp, 0, Widths[beatIndex] + PulseDatas[beatIndex].PulseWidth);
            ret[5] = PolarVector.ToCartesianCoordinates(sp);

            return ret;
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
