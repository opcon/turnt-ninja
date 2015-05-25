using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatDetection.Audio;
using BeatDetection.Core;
using BeatDetection.Game;
using ColorMine.ColorSpaces;
using OpenTK.Graphics;
using Substructio.Core.Math;
using Substructio.Graphics.OpenGL;

namespace BeatDetection.Generation
{
    class StageGeometryBuilder
    {
        private StageGeometry _stageGeometry;
        private AudioFeatures _audioFeatures;
        private GeometryBuilderOptions _builderOptions;

        private BeatCollection _beats;
        private float[] _beatFrequencies;
        private Color4[] _segmentColours;
        private SegmentInformation[] _segments;
        private Random _random;

        private int _maxID;

        public StageGeometry Build(AudioFeatures audioFeatures, Random random, GeometryBuilderOptions builderOptions)
        {
            _audioFeatures = audioFeatures;
            _builderOptions = builderOptions;
            _random = random;
            _builderOptions.RandomFunction = _random;

            BuildGeometry();
            BuildBeatFrequencyList();
            ProcessSegments();
            BuildSegmentColours();

            var backgroundPolygon = new PolarPolygon(6, new PolarVector(0.5, 0), 50000, -20, 0);
            backgroundPolygon.ShaderProgram = _builderOptions.GeometryShaderProgram;

            return new StageGeometry(_beats, _segments, _segmentColours, _random, _beatFrequencies) {BackgroundPolygon = backgroundPolygon};
        }

        private void BuildBeatFrequencyList()
        {
            var sorted = _audioFeatures.Onsets.OrderBy(f => f).ToArray();
            _beatFrequencies = new float[sorted.Length];
            int lookAhead = 5;
            int halfFrequencySampleSize = 3;

            for (int i = 0; i < _beatFrequencies.Length; i++)
            {
                //int count;
                //if (_beatFrequencies.Length - i < lookAhead) count = _beatFrequencies.Length - i;
                //else count = lookAhead;
                
                int weight = 0;
                float differenceSum = 0;
                int total = 0;
                for (int j = i - halfFrequencySampleSize < 1 ? 1 : i-halfFrequencySampleSize; j <= i; j++)
                {
                    weight++;
                    differenceSum += weight*(sorted[j] - sorted[j-1]);
                    total += weight;
                }

                //weight = halfFrequencySampleSize;
                //differenceSum += (weight+1)*(sorted[i+1] - sorted[i]);
                //total += weight;

                weight = halfFrequencySampleSize + 1;
                int count = i + halfFrequencySampleSize + 1> _beatFrequencies.Length - 1 ? _beatFrequencies.Length - 1 : i + halfFrequencySampleSize + 1;
                for (int j = i+1; j <= count; j++)
                {
                    differenceSum += weight*(sorted[j] - sorted[j-1]);
                    total += weight;
                    weight--;
                }

                //float differenceSum = 0;
                //int weight = count;
                //int total = 0;
                //for (int j = 1; j < count; j++,weight--)
                //{
                //    differenceSum += weight*(sorted[i + j] - sorted[i + j - 1]);
                //    total += weight;
                //}

                _beatFrequencies[i] = 1/(differenceSum/total);
            }

            _beatFrequencies[_beatFrequencies.Length - 1] = _beatFrequencies[_beatFrequencies.Length - 2];
        }

        private void BuildGeometry()
        {
            _beats = new BeatCollection(_audioFeatures.Onsets.Count, _builderOptions.GeometryShaderProgram);

            //intialise state variables for algorithim
            int prevStart = 0;
            int prevSkip = 0;
            //set initial previous time to -1 so that the first polygon generated is always unique and doesn't trigger 'beat too close to previous' case
            float prevTime = -1.0f;

            int index = 0;

            //sort onset list by time
            var sorted = _audioFeatures.Onsets.OrderBy(f => f);

            //traverse sorted onset list and generate geometry for each onset
            foreach (var b in sorted)
            {
                int start;

                //generate the skip pattern. Highest probablility is of obtaining a 1 skip pattern - no sides are skipped at all.
                int skip = _builderOptions.SkipFunction();
                if (b - prevTime < _builderOptions.VeryCloseDistance)
                {
                    //this beat is very close to the previous one, use the same start orientation and skip pattern
                    start = prevStart;
                    skip = prevSkip;
                }
                else if (b - prevTime < _builderOptions.CloseDistance)
                {
                    //randomly choose relative orientation difference compared to previous beat
                    var r = _random.Next(0, 2);
                    if (r == 0) r = -1;

                    //this beat is reasonably close to the previous one, use the same skip pattern but a different (+/- 1) orientation
                    start = (prevStart + 6) + r;
                    skip = prevSkip;
                }
                else
                {
                    //choose a random start position for this polygon
                    start = _random.Next(_builderOptions.MaxSides - 1);
                    while (start == prevStart && _random.NextDouble() > 0.15)
                        start = _random.Next(_builderOptions.MaxSides - 1);
                }

                bool[] sides = new bool[6];
                for (int i = 0; i < 6; i++)
                {
                    //ensure that if skip is set to 1, we still leave an opening
                    if (skip == 1 && i == start % 6) sides[i] = false;
                    //if skip is not set to 1 and this is not a side we are skipping, enable this side
                    else if ((i + start) % skip == 0) sides[i] = true;
                    //else disable sides by default
                    else sides[i] = false;
                }

                _beats.AddBeat(sides.ToList(), _builderOptions.PolygonVelocity, _builderOptions.PolygonWidth, _builderOptions.PolygonMinimumRadius, b);

                //update the variables holding the previous state of the algorithim.
                prevTime = b;
                prevStart = start;
                prevSkip = skip;

                index++;
            }
            _beats.Initialise();
        }

        private void ProcessSegments()
        {
            //order audio segments by increasing start time.
            _segments = _audioFeatures.Segments.OrderBy(x => x.StartTime).ToArray();
            //find max id number from segments
            _maxID = _audioFeatures.Segments.Max(x => x.ID);
        }

        private void BuildSegmentColours()
        {
            _segmentColours = new Color4[_maxID];

            //initialise algorithim values
            double maxStep = (double)360 / (_maxID + 1);
            double minStep = _builderOptions.MinimumColourStepMultiplier * maxStep;
            double startAngle = _random.NextDouble() * 360;
            double prevAngle = startAngle - maxStep;

            //generate all the colour values
            for (int i = 0; i < _maxID; i++)
            {
                var step = _random.NextDouble() * (maxStep - minStep) + minStep;
                double angle = prevAngle;
                //avoid certain colours
//                do
//                {
                    angle = MathUtilities.Normalise(step + angle, 0, 360);
//                } while ((angle > 250 && angle < 330) || (angle > 65 && angle < 150));
                var rgb = HUSL.ColorConverter.HUSLToRGB(new List<double>{angle, _builderOptions.Lightness, _builderOptions.Saturation});

//                var col = new Hsl { H = angle, L = _builderOptions.Lightness, S = _builderOptions.Saturation};
//                var rgb1 = col.ToRgb();

                prevAngle = angle;

                _segmentColours[i] = new Color4((byte)((rgb[0])*255), (byte)((rgb[1])*255), (byte)((rgb[2])*255), 255);
            }
        }
    }

    class GeometryBuilderOptions
    {
        public int MaxSides = 6;

        public PolarVector PolygonVelocity = new PolarVector(0, 600);
        public float PolygonWidth = 50f;
        public float PolygonMinimumRadius = 130f;

        public float VeryCloseDistance = 0.2f;
        public float CloseDistance = 0.4f;

        public Random RandomFunction;

        /// <summary>
        /// Method to use to get skip value.
        /// Default returns value between 1 and 3 inclusive
        /// </summary>
        /// <returns>Skip value</returns>
        public delegate int SkipDistributionFunction();
        public SkipDistributionFunction SkipFunction;

        public int Saturation = 40;
        public int Lightness = 40;
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
        }
    }
}
