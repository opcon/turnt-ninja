using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TurntNinja.Audio;
using TurntNinja.Core;
using TurntNinja.Game;
using ColorMine.ColorSpaces;
using OpenTK.Graphics;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;

namespace TurntNinja.Generation
{
    class StageGeometryBuilder
    {
        private StageGeometry _stageGeometry;
        private AudioFeatures _audioFeatures;
        private GeometryBuilderOptions _builderOptions;

        private OnsetCollection _onsets;
        private OnsetDrawing _onsetDrawing;
        private float[] _beatFrequencies;
        private Color4 _segmentStartColour;
        private Random _random;

        private List<float> _goodBeats;

        public StageGeometry Build(AudioFeatures audioFeatures, Random random, GeometryBuilderOptions builderOptions)
        {
            _audioFeatures = audioFeatures;
            _builderOptions = builderOptions;
            _random = random;
            _builderOptions.RandomFunction = _random;

            BuildGoodBeatsList();
            BuildBeatFrequencyList();

            BuildGeometry();
            SetStartColour();

            var backgroundPolygon = new PolarPolygon(6, new PolarVector(0.5, 0), 50000, -20, 0);
            backgroundPolygon.ShaderProgram = _builderOptions.GeometryShaderProgram;

            return new StageGeometry(_onsets, _onsetDrawing, _segmentStartColour, _random) {BackgroundPolygon = backgroundPolygon};
        }

        private void BuildGoodBeatsList()
        {
            //sort onset list by time
            var sorted = _audioFeatures.OnsetTimes.OrderBy(f => f);
            _goodBeats = new List<float>();
            float prevTime = -1.0f;

            //filter out beats that are too close
            foreach (var b in sorted)
            {
                if (b - prevTime < _builderOptions.BeatSkipDistance)
                    continue;
                _goodBeats.Add(b);
                prevTime = b;
            }
        }

        private void BuildBeatFrequencyList()
        {
            _beatFrequencies = new float[_goodBeats.Count];
            int lookAhead = 5;
            int halfFrequencySampleSize = 4;
            int forwardWeighting = 1;

            for (int i = 0; i < _beatFrequencies.Length; i++)
            {
                int weight = 0;
                float differenceSum = 0;
                int total = 0;
                //for (int j = i - halfFrequencySampleSize < 1 ? 1 : i-halfFrequencySampleSize; j <= i; j++)
                //{
                //    weight++;
                //    differenceSum += (sorted[j] - sorted[j-1]);
                //    total += 1;
                //}

                //weight = halfFrequencySampleSize + forwardWeighting;
                int count = i + halfFrequencySampleSize + 1> _beatFrequencies.Length - 1 ? _beatFrequencies.Length - 1 : i + halfFrequencySampleSize + 1;
                for (int j = i+1; j <= count; j++)
                {
                    differenceSum += (_goodBeats[j] - _goodBeats[j-1]);
                    total += 1;
                    //weight--;
                }

                _beatFrequencies[i] = 1/(differenceSum/total);
            }

            _beatFrequencies[_beatFrequencies.Length - 1] = _beatFrequencies[_beatFrequencies.Length - 2];

            //_beatFrequencies = _audioFeatures.Onsets.Select(o => o.OnsetAmplitude).ToArray();
        }

        private void BuildGeometry()
        {
            _onsets = new OnsetCollection(_goodBeats.Count);
            _onsets.AddOnsets(_goodBeats.ToArray(), _beatFrequencies);
            _onsetDrawing = new OnsetDrawing(_onsets, _builderOptions.GeometryShaderProgram);

            //intialise state variables for algorithim
            int prevStart = 0;
            int prevSkip = 0;
            //set initial previous time to -1 so that the first polygon generated is always unique and doesn't trigger 'beat too close to previous' case
            double prevTime = -1.0;
            float samePatternChance = 0.90f;
            float onsetStart = 0.0f;
            float onsetEnd = 0.0f;
            float veryCloseJoinChance = 0.5f;
            float joinFunctionMultiplier = 20.0f;

            bool[] sides;

            var structureList = new List<List<int>>();

            List<OnsetStructure> structures = new List<OnsetStructure>();

            int currentStructureIndex = -1;
            //first pass to look for structures
            foreach (var b in _goodBeats)
            {
                // are we extending an existing structure?
                double t = Math.Exp(-joinFunctionMultiplier*(b - prevTime - _builderOptions.VeryCloseDistance) + Math.Log(veryCloseJoinChance));
                Console.WriteLine(t);
                if (_random.NextDouble() < t)
                {
                    structures[currentStructureIndex].End = b;
                }

                // else create new structure
                else
                {
                    structures.Add(new OnsetStructure { Start = b, End = b });
                    currentStructureIndex = structures.Count - 1;
                }

                // update previous onset time
                prevTime = b;
            }

            prevTime = -1;

            //traverse sorted onset list and generate geometry for each onset
            foreach (var s in structures)
            {
                int start;

                //generate the skip pattern. Highest probablility is of obtaining a 1 skip pattern - no sides are skipped at all.
                int skip = _builderOptions.SkipFunction();
                if (s.Start - prevTime < _builderOptions.VeryCloseDistance)
                {
                    //this beat is very close to the previous one, use the same start orientation and skip pattern
                    start = prevStart;
                    skip = prevSkip;
                }
                else if (s.Start - prevTime < _builderOptions.CloseDistance)
                {
                    //randomly choose relative orientation difference compared to previous beat
                    var r = _random.Next(0, 2);
                    if (r == 0) r = -1;

                    //this beat is reasonably close to the previous one, use the same skip pattern but a different (+/- 1) orientation
                    start = (prevStart + 6) + r;
                    if (_random.NextDouble() < samePatternChance)
                        skip = prevSkip;
                }
                else
                {
                    //choose a random start position for this polygon
                    start = _random.Next(_builderOptions.MaxSides - 1);
                    while (start == prevStart && _random.NextDouble() > 0.15)
                        start = _random.Next(_builderOptions.MaxSides - 1);
                }

                sides = new bool[6];
                for (int i = 0; i < 6; i++)
                {
                    //ensure that if skip is set to 1, we still leave an opening
                    if (skip == 1 && i == start % 6) sides[i] = false;
                    //if skip is not set to 1 and this is not a side we are skipping, enable this side
                    else if ((i + start) % skip == 0) sides[i] = true;
                    //else disable sides by default
                    else sides[i] = false;
                }

                _onsetDrawing.AddOnsetDrawing(sides.ToList(), _builderOptions.PolygonVelocity, _builderOptions.PolygonWidth + (s.End - s.Start) * _builderOptions.PolygonVelocity.Radius, _builderOptions.PolygonMinimumRadius, s.Start);

                //update the variables holding the previous state of the algorithim.
                prevTime = s.End;
                prevStart = start;
                prevSkip = skip;
            }
            _onsetDrawing.Initialise();
            _onsets.Initialise();
        }

        private void SetStartColour()
        {
            //initialise algorithim values
            double maxStep = (double)360 / (20);
            double minStep = _builderOptions.MinimumColourStepMultiplier * maxStep;
            double startAngle = _random.NextDouble() * 360;
            double prevAngle = startAngle - maxStep;

            var step = _random.NextDouble() * (maxStep - minStep) + minStep;
            double angle = prevAngle;
            angle = MathUtilities.Normalise(step + angle, 0, 360);
            var rgb = HUSL.ColorConverter.HUSLToRGB(new List<double>{angle, _builderOptions.Saturation, _builderOptions.Lightness});

            prevAngle = angle;

            _segmentStartColour = new Color4((byte)((rgb[0])*255), (byte)((rgb[1])*255), (byte)((rgb[2])*255), 255);
        }
    }

    class OnsetStructure
    {
        public double Start;
        public double End;
    }

    class GeometryBuilderOptions
    {
        public int MaxSides = 6;

        public PolarVector PolygonVelocity = new PolarVector(0, 600);
        public float PolygonWidth = 40f;
        public float PolygonMinimumRadius = 130f;

        public float VeryCloseDistance = 0.2f;
        public float CloseDistance = 0.4f;
        public float BeatSkipDistance = 0.0f;

        public Random RandomFunction;

        /// <summary>
        /// Method to use to get skip value.
        /// Default returns value between 1 and 3 inclusive
        /// </summary>
        /// <returns>Skip value</returns>
        public delegate int SkipDistributionFunction();
        public SkipDistributionFunction SkipFunction;

        public int Saturation = 50;
        public int Lightness = 30;
        public double MinimumColourStepMultiplier = 0.25;

        public ShaderProgram GeometryShaderProgram;

        public GeometryBuilderOptions(ShaderProgram geometryShaderProgram)
        {
            GeometryShaderProgram = geometryShaderProgram;
            SkipFunction = () => (int) (3*Math.Pow(RandomFunction.NextDouble(), 4)) + 1;
        }

        public void ApplyDifficulty(DifficultyOptions options)
        {
            PolygonVelocity.Radius = options.Speed;
            VeryCloseDistance = options.VeryCloseDistance;
            CloseDistance = options.CloseDistance;
            BeatSkipDistance = options.BeatSkipDistance;
        }
    }
}
