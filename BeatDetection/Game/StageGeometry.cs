using System;
using System.Collections.Generic;
using System.Linq;
using BeatDetection.Audio;
using BeatDetection.Core;
using ClipperLib;
using ColorMine.ColorSpaces;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Substructio.Core;
using Substructio.Core.Math;
using HUSL;
using OpenTK.Input;

namespace BeatDetection.Game
{
    class StageGeometry : IDisposable
    {
        private StageColours _colours;
        private BeatCollection _beats;
        private Color4 _segmentStartColour;
        private double _initialHue;
        private double _extraHue = 0.0;
        private double _hueWobbleAmount = 30;
        private HUSLColor _baseColour;
        public Stage ParentStage;

        private Random _random;

        public Player Player;
        public PolarPolygon CenterPolygon;
        public PolarPolygon BackgroundPolygon;
        private int _direction = 1;
        public float RotationSpeed = 1.0f;
        private double _rotationMultiplier = 0.0f;
        private float _extraRotation = 0;

        private int _collidedBeatIndex = -1;
        public float[] BeatFrequencies;
        public float MaxBeatFrequency;
        public float MinBeatFrequency;

        private double _elapsedTime = 0;
        private int frameCount = 0;

        private int _previousBeat = -1000;

        public int BeatCount {get { return _beats.Count; }}

        public StageColourModifiers ColourModifiers = StageColourModifiers.Default;

        private bool Collided
        {
            get { return _collidedBeatIndex == _beats.Index; }
        }

        public float Amplitude
        {
            get { return BeatFrequencies[_beats.Index]; }
            //get { return ((BeatFrequencies[_beats.Index] - MinBeatFrequency) / (MaxBeatFrequency - MinBeatFrequency)); }
        }

        public int CurrentBeat
        {
            get { return _beats.Index; }
        }

        public float CurrentBeatFrequency
        {
            get { return OutOfBeats ? BeatFrequencies.Last() : BeatFrequencies[CurrentBeat]; }
        }

        public bool OutOfBeats
        {
            get { return _beats.Index == _beats.Count; }
        }

        internal StageGeometry (BeatCollection beats, Color4 segmentStartColour, Random random, float[] beatFrequencies)
        {
            _beats = beats;
            _segmentStartColour = segmentStartColour;
            _random = random;
            BeatFrequencies = beatFrequencies;
            MaxBeatFrequency = BeatFrequencies.Max();
            MinBeatFrequency = BeatFrequencies.Min();
            _baseColour = HUSLColor.FromColor4(_segmentStartColour);
            _initialHue = _baseColour.H;
        }

        public void Update(double time)
        {
            frameCount++;
            if (frameCount > 10) frameCount = 0;

            _elapsedTime += time;
            _rotationMultiplier = 0.5*Math.Min(((!OutOfBeats ? BeatFrequencies[_beats.Index] : MaxBeatFrequency)/MaxBeatFrequency)*2, 1);
            var rotate = time * RotationSpeed * _rotationMultiplier + _extraRotation * (1+_rotationMultiplier/3.0f);
            rotate = Math.Abs(rotate) * _direction;
            _extraRotation = 0;
            ParentStage.SceneManager.ScreenCamera.ExtraScale = 0;

            var azimuth = CenterPolygon.Position.Azimuth + rotate;

            _beats.Update(time, ParentStage.Running, azimuth);

            if (!OutOfBeats)
            {
                CenterPolygon.PulseMultiplier = Math.Pow(BeatFrequencies[CurrentBeat] * 60,1) + 70;
                ParentStage.SceneManager.ScreenCamera.ExtraScale = CenterPolygon.Pulsing ?  (float)Math.Pow(BeatFrequencies[CurrentBeat],3) * 0.2f : 0;

                if (_beats.Positions[CurrentBeat].Radius - _beats.ImpactDistances[CurrentBeat] < 60 && _beats.Positions[CurrentBeat].Radius - _beats.ImpactDistances[CurrentBeat] > 40)
                    _extraRotation = 0.005f;

                if (ParentStage.AI)
                {
                    var t = _beats.CurrentOpeningAngle + rotate * _direction + CenterPolygon.Position.Azimuth;
                    t += MathHelper.DegreesToRadians(30);
                    Player.DoAI(t);
                }
            }

            if (_beats.BeginPulse)
                CenterPolygon.BeginPulse();
 
            UpdatePlayerOverlap();

            if (_beats.BeatsHit > 0)
            {
                var d = _random.NextDouble();
                _direction = d > 0.95 ? -_direction : _direction;
                ParentStage.Multiplier += _beats.BeatsHit;
                Player.Score += (ParentStage.Multiplier*ParentStage.ScoreMultiplier + 1)*10;
            }

            Player.Direction = _direction;

            //update center polygon colour if finished colliding 
            if (CenterPolygon.EvenColour == _colours.EvenCollisionColour && !Collided)
                CenterPolygon.SetColour(_colours.EvenOpposingColour, _colours.EvenOutlineColour, _colours.OddOpposingColour, _colours.OddOutlineColour);

            //Player.Position.Azimuth += rotate;
            Player.Position = new PolarVector(Player.Position.Azimuth + rotate, Player.Position.Radius);
            Player.Update(time, ParentStage.AI);

            CenterPolygon.Update(time, false);
            CenterPolygon.Position.Azimuth += rotate;

            BackgroundPolygon.Position.Azimuth = CenterPolygon.Position.Azimuth + rotate;
            BackgroundPolygon.Update(time, false);

            if (frameCount == 10)
                UpdateColours(time);

        }

        public void Draw(double time)
        {
            ParentStage.ShaderProgram.SetUniform("in_color", _colours.EvenBackgroundColour);
            BackgroundPolygon.Draw(time, 1);
            ParentStage.ShaderProgram.SetUniform("in_color", _colours.OddBackgroundColour);
            BackgroundPolygon.Draw(time, 2);

            ParentStage.ShaderProgram.SetUniform("in_color", _colours.EvenOutlineColour);
            _beats.DrawOutlines(time, 1);
            ParentStage.ShaderProgram.SetUniform("in_color", _colours.OddOutlineColour);
            _beats.DrawOutlines(time, 2);

            ParentStage.ShaderProgram.SetUniform("in_color", _colours.EvenOpposingColour);

            _beats.Draw(time, 1);

            if (_beats.Index < _beats.Count && Collided)
            {
                ParentStage.ShaderProgram.SetUniform("in_color", _colours.EvenCollisionColour);
                _beats.DrawCurrentBeat(time, 1);
            }
            CenterPolygon.Draw(time, 1);
            ParentStage.ShaderProgram.SetUniform("in_color", _colours.OddOpposingColour);

            _beats.Draw(time, 2);

            if (_beats.Index < _beats.Count && Collided)
            {
                ParentStage.ShaderProgram.SetUniform("in_color", _colours.OddCollisionColour);
                _beats.DrawCurrentBeat(time, 2);
            }

            CenterPolygon.Draw(time, 2);

            ParentStage.ShaderProgram.SetUniform("in_color", _colours.EvenOutlineColour);
            CenterPolygon.DrawOutline(time, 1);
            ParentStage.ShaderProgram.SetUniform("in_color", _colours.OddOutlineColour);
            CenterPolygon.DrawOutline(time, 2);

            ParentStage.ShaderProgram.SetUniform("in_color", Color4.White);
            Player.Draw(time);
        }

        private void UpdatePlayerOverlap()
        {
            if (_beats.Index == _collidedBeatIndex || _beats.Index >= _beats.Count)
            {
                ParentStage.Overlap = 0;
                return;
            }
            var c = new Clipper();
            c.AddPaths(_beats.GetPolygonBounds(_beats.Index), PolyType.ptSubject, true);
            if (_beats.Index < _beats.Count - 1)
                c.AddPaths(_beats.GetPolygonBounds(_beats.Index + 1), PolyType.ptSubject, true);
            c.AddPath(Player.GetBounds(), PolyType.ptClip, true);

            var soln = new List<List<IntPoint>>();
            c.Execute(ClipType.ctIntersection, soln);

            //ParentStage.Overlap = soln.Count > 0 ? (int)((Clipper.Area(soln[0]) / Clipper.Area(Player.GetBounds())) * 100) : 0;
            ParentStage.Overlap = 0;
            foreach (var a in soln)
            {
                ParentStage.Overlap += (int) ((Clipper.Area(a)/Clipper.Area(Player.GetBounds()))*(100.0f));
            }
            if (ParentStage.Overlap > 80)
            {
                ParentStage.Multiplier = -1;
                Player.Hits++;
                _collidedBeatIndex = _beats.Index;
                //_polygons[_collidedBeatIndex].SetColour(_colours.EvenCollisionColour, _colours.EvenCollisionOutlineColour, _colours.OddCollisionColour, _colours.OddCollisionOutlienColour);
                CenterPolygon.SetColour(_colours.EvenCollisionColour, _colours.EvenCollisionOutlineColour, _colours.OddCollisionColour, _colours.OddCollisionOutlienColour);
            }
        }

        public void UpdateColours(double time)
        {
            if (InputSystem.CurrentKeys.Contains(Key.Number1))
            {
                if (InputSystem.CurrentKeys.Contains(Key.ShiftLeft))
                    ColourModifiers.baseLightness -= 3;
                else
                    ColourModifiers.baseLightness += 3;
            }
            if (InputSystem.CurrentKeys.Contains(Key.Number2))
            {
                if (InputSystem.CurrentKeys.Contains(Key.ShiftLeft))
                    ColourModifiers.baseSaturation -= 3;
                else
                    ColourModifiers.baseSaturation += 3;
            }
            if (InputSystem.CurrentKeys.Contains(Key.Number3))
            {
                if (InputSystem.CurrentKeys.Contains(Key.ShiftLeft))
                    ColourModifiers.foregroundLightnessDelta -= 3;
                else
                    ColourModifiers.foregroundLightnessDelta += 3;
            }
            if (InputSystem.CurrentKeys.Contains(Key.Number4))
            {
                if (InputSystem.CurrentKeys.Contains(Key.ShiftLeft))
                    ColourModifiers.foregroundSaturationDelta -= 3;
                else
                    ColourModifiers.foregroundSaturationDelta += 3;
            }
            if (InputSystem.CurrentKeys.Contains(Key.Number5))
            {
                if (InputSystem.CurrentKeys.Contains(Key.ShiftLeft))
                    ColourModifiers.outlineLightness -= 3;
                else
                    ColourModifiers.outlineLightness += 3;
            }
            if (InputSystem.CurrentKeys.Contains(Key.Number6))
            {
                if (InputSystem.CurrentKeys.Contains(Key.ShiftLeft))
                    ColourModifiers.outlineSaturation -= 3;
                else
                    ColourModifiers.outlineSaturation += 3;
            }

            //_baseColour.H += time*50f*(!OutOfBeats ? BeatFrequencies[_beats.Index] : 1);
            if (CurrentBeat - _previousBeat > 3)
            {
                _previousBeat = CurrentBeat;
                _extraHue = ((_random.NextDouble() > 0.5) ? -1 : 1) * (90 + (_hueWobbleAmount * _random.NextDouble() - _hueWobbleAmount / 2));

            }
            _baseColour.H = _initialHue + (CurrentBeat) * 5;
            _baseColour.L = ColourModifiers.baseLightness;
            _baseColour.S = ColourModifiers.baseSaturation;

            var evenBackground = _baseColour;

            //find odd background
            var oddBackground = evenBackground;
            oddBackground.S += 5;
            oddBackground.L += 5;

            //set the background colours
            _colours.EvenBackgroundColour = HUSLColor.ToColor4(evenBackground);
            _colours.OddBackgroundColour = HUSLColor.ToColor4(oddBackground);

            //find the collision colours
            var opp = evenBackground;
            opp.S += 30;
            opp.L += 20;

            _colours.EvenCollisionColour = HUSLColor.ToColor4(opp);
            _colours.EvenCollisionOutlineColour = HUSLColor.ToColor4(GetOutlineColour(opp));

            opp = oddBackground;
            opp.S += 30;
            opp.L += 20;

            _colours.OddCollisionColour = HUSLColor.ToColor4(opp);
            _colours.OddCollisionOutlienColour = HUSLColor.ToColor4(GetOutlineColour(opp));

            //find the foreground colours
            var fEven = evenBackground;
            var fOdd = oddBackground;
            fEven.H = MathUtilities.Normalise(fEven.H + _extraHue, 0, 360);
            fEven.S += ColourModifiers.foregroundSaturationDelta;
            fEven.L += ColourModifiers.foregroundLightnessDelta;
            fOdd.H = MathUtilities.Normalise(fOdd.H + _extraHue, 0, 360);
            fOdd.S += ColourModifiers.foregroundSaturationDelta;
            fOdd.L += ColourModifiers.foregroundLightnessDelta;

            //set the foreground colours
            _colours.EvenOpposingColour = HUSLColor.ToColor4(fEven);
            _colours.EvenOutlineColour = HUSLColor.ToColor4(GetOutlineColour(fEven));
            _colours.OddOpposingColour = HUSLColor.ToColor4(fOdd);
            _colours.OddOutlineColour = HUSLColor.ToColor4(GetOutlineColour(fOdd));
        }


        private HUSLColor GetOutlineColour(HUSLColor col)
        {
            col.L = ColourModifiers.outlineLightness;
            col.S = ColourModifiers.outlineSaturation;
            return col;
        }

        public void Dispose()
        {
            _beats.Dispose();
        }
    }

    struct StageColours
    {
        public Color4 EvenBackgroundColour;
        public Color4 EvenOpposingColour;
        public Color4 EvenCollisionColour;
        public Color4 EvenCollisionOutlineColour;
        public Color4 EvenOutlineColour;

        public Color4 OddBackgroundColour;
        public Color4 OddOpposingColour;
        public Color4 OddCollisionColour;
        public Color4 OddOutlineColour;
        public Color4 OddCollisionOutlienColour;
    }

    struct StageColourModifiers
    {
        public double outlineLightness;
        public double outlineSaturation;
        public double foregroundLightnessDelta;
        public double foregroundSaturationDelta;
        public double baseLightness;
        public double baseSaturation;

        public static StageColourModifiers Default
        {
            get
            {
                return new StageColourModifiers { outlineLightness = 80, outlineSaturation = 50, foregroundLightnessDelta = 5, foregroundSaturationDelta = 0, baseLightness = 30, baseSaturation = 50};
            }
        }

        public override string ToString()
        {
            return string.Format("Base Lightness: {0}\nBase Saturation: {1}\nForeground Lightness Delta: {2}\nForeground Saturation Delta: {3}\nOutline Lightness: {4}\nOutline Saturation: {5}",
                baseLightness, baseSaturation, foregroundLightnessDelta, foregroundSaturationDelta, outlineLightness, outlineSaturation);
        }

    }
}
