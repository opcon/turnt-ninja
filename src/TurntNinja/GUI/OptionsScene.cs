using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.GUI;
using Substructio.Core;
using TurntNinja.Game;
using OpenTK.Input;
using QuickFont;
using OpenTK;
using OpenTK.Graphics;

namespace TurntNinja.GUI
{
    class OptionsScene : Scene
    {     

        private float _audioCorrection;
        private DifficultyLevels _currentDifficulty;
        private QFontDrawing _optionsDrawing;
        private GameFont _optionsFont;
        private GameFont _valueFont;

        List<OptionBase> _options;
        int _currentlySelectedOption = 0;

        public OptionsScene()
        {
            Exclusive = true;
        }

        public override void Load()
        {
            _optionsFont = SceneManager.GameFontLibrary.GetFirstOrDefault(GameFontType.Heading);
            _valueFont = SceneManager.GameFontLibrary.GetFirstOrDefault(GameFontType.Heading);
            _optionsDrawing = new QFontDrawing();
            _optionsDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;

            _options = new List<OptionBase>();

            var audioCorrection = (float)SceneManager.GameSettings["AudioCorrection"] * 1000f;
            var vol = (float)Math.Round((float)SceneManager.GameSettings["MaxAudioVolume"] * 100);
            var currentDifficulty = (DifficultyLevels)SceneManager.GameSettings["DifficultyLevel"];
            var analytics = (bool)ServiceLocator.Settings["Analytics"];
            var windowMode = (string)ServiceLocator.Settings["WindowState"];
            var colourMode = (ColourMode)ServiceLocator.Settings["ColourMode"];

            _options.Add(new NumericOption
            {
                FriendlyName = "Volume",
                SettingName = "MaxAudioVolume",
                Minimum = 0.0f,
                Maximum = 100.0f,
                Scale = 100.0f,
                Round = true,
                Step = 1.0f,
                Value = vol
            });

            _options.Add(new NumericOption
            {
                FriendlyName = "Audio Correction (ms)",
                SettingName = "AudioCorrection",
                Minimum = -1000.0f,
                Maximum = 1000.0f,
                Scale = 1000.0f,
                Round = true,
                Step = 2.0f,
                Value = audioCorrection
            });

            _options.Add(new EnumOption<DifficultyLevels>
            {
                FriendlyName = "Difficulty",
                SettingName = "DifficultyLevel",
                Values = Enum.GetNames(typeof(DifficultyLevels)).ToList(),
                CurrentIndex = Enum.GetNames(typeof(DifficultyLevels)).ToList().IndexOf(currentDifficulty.ToString())
            });

            _options.Add(new BoolOption
            {
                FriendlyName = "Analytics",
                SettingName = "Analytics",
                Value = analytics
            });

            var windowModes = new List<string> { WindowState.Fullscreen.ToString(), WindowState.Normal.ToString() };
            _options.Add(new StringOption
            {
                FriendlyName = "Window Mode",
                SettingName = "WindowState",
                Values = windowModes,
                CurrentIndex = windowModes.IndexOf(windowMode),
                CallbackFunction = (s) => SceneManager.GameWindow.WindowState = (WindowState)Enum.Parse(typeof(WindowState), s)
            });

            _options.Add(new EnumOption<ColourMode>
            {
                FriendlyName = "Colour Mode",
                SettingName = "ColourMode",
                Values = Enum.GetNames(typeof(ColourMode)).ToList(),
                CurrentIndex = Enum.GetNames(typeof(ColourMode)).ToList().IndexOf(colourMode.ToString())
            });

            foreach (var op in _options)
            {
                op.Sanitise();
            }
            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Resize(EventArgs e)
        {
            _optionsDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Down))
            {
                _currentlySelectedOption += 1;
                if (_currentlySelectedOption >= _options.Count) _currentlySelectedOption = 0;
            }
            if (InputSystem.NewKeys.Contains(Key.Up))
            {
                _currentlySelectedOption -= 1;
                if (_currentlySelectedOption < 0) _currentlySelectedOption = _options.Count - 1;
            }

            var currentOption = _options[_currentlySelectedOption];
            if (InputSystem.NewKeys.Contains(Key.Left) && currentOption.CanMoveBackward())
            {
                currentOption.MovePrevious();
                currentOption.RaiseOptionChanged();
            }

            if (InputSystem.NewKeys.Contains(Key.Right) && currentOption.CanMoveForward())
            {
                currentOption.MoveNext();
                currentOption.RaiseOptionChanged();
            }

            if (InputSystem.NewKeys.Contains(Key.Escape))
            {
                foreach (var op in _options)
                {
                    op.Sanitise();
                    op.SaveSettings();
                }
                SceneManager.RemoveScene(this, true);
            }
        }

        public override void Draw(double time)
        {
            _optionsDrawing.DrawingPrimitives.Clear();

            float lineStep = Math.Max(_optionsFont.MaxLineHeight, _valueFont.MaxLineHeight);
            float height = lineStep * _options.Count;

            float currentY = height / 2.0f;
            float unselectedValueScale = 0.8f;

            foreach (var op in _options)
            {
                var settingColour = Color4.White;
                if (_options[_currentlySelectedOption] != op)
                {
                    settingColour = Color.Black;
                    settingColour.A = 0.90f;
                }

                var dp = new QFontDrawingPrimitive(_optionsFont.Font, new QFontRenderOptions { Colour = (Color)settingColour });
                dp.Print(op.FriendlyName + ":", Vector3.Zero, QFontAlignment.Centre);
                dp.ModelViewMatrix = Matrix4.CreateTranslation(0, _optionsFont.MaxLineHeight * 0.5f, 0)
                                        * Matrix4.CreateTranslation(-WindowWidth * 0.15f, currentY, 0);
                _optionsDrawing.DrawingPrimitives.Add(dp);

                dp = new QFontDrawingPrimitive(_valueFont.Font, new QFontRenderOptions { Colour = (Color)settingColour });
                var valueSize = dp.Print(op.GetValue(), Vector3.Zero, QFontAlignment.Centre);
                dp.ModelViewMatrix = Matrix4.CreateTranslation(0, _valueFont.MaxLineHeight * 0.5f, 0)
                                        * Matrix4.CreateTranslation(WindowWidth * 0.15f, currentY, 0);
                _optionsDrawing.DrawingPrimitives.Add(dp);

                if (op.CanMoveForward())
                {
                    dp = new QFontDrawingPrimitive(_valueFont.Font, new QFontRenderOptions { Colour = (Color)settingColour });
                    dp.Print(op.GetNextValue(), Vector3.Zero, QFontAlignment.Left);
                    dp.ModelViewMatrix = Matrix4.CreateScale(unselectedValueScale)
                                            * Matrix4.CreateTranslation(WindowWidth * 0.15f + valueSize.Width * 1.25f, currentY + _valueFont.Font.MaxLineHeight * 0.5f * unselectedValueScale, 0);
                    _optionsDrawing.DrawingPrimitives.Add(dp);
                }
                if (op.CanMoveBackward())
                {
                    dp = new QFontDrawingPrimitive(_valueFont.Font, new QFontRenderOptions { Colour = (Color)settingColour });
                    dp.Print(op.GetPrevValue(), Vector3.Zero, QFontAlignment.Right);
                    dp.ModelViewMatrix = Matrix4.CreateScale(unselectedValueScale)
                                            * Matrix4.CreateTranslation(WindowWidth * 0.15f - valueSize.Width * 1.25f, currentY + _valueFont.Font.MaxLineHeight * 0.5f * unselectedValueScale, 0);
                    _optionsDrawing.DrawingPrimitives.Add(dp);
                }

                currentY -= lineStep;
            }


            _optionsDrawing.RefreshBuffers();
            _optionsDrawing.Draw();
        }

        public override void Dispose()
        {
            _optionsDrawing.Dispose();
        }

        public override void EnterFocus()
        {
            OpenTK.Graphics.OpenGL4.GL.ClearColor(Color.White);
            InputSystem.RepeatingKeys.Add(Key.Left, KeyRepeatSettings.Default);
            InputSystem.RepeatingKeys.Add(Key.Right, KeyRepeatSettings.Default);
            InputSystem.RepeatingKeys.Add(Key.Up, KeyRepeatSettings.Default);
            InputSystem.RepeatingKeys.Add(Key.Down, KeyRepeatSettings.Default);
        }

        public override void ExitFocus()
        {
            OpenTK.Graphics.OpenGL4.GL.ClearColor(Color.Black);
            InputSystem.RepeatingKeys.Remove(Key.Left);
            InputSystem.RepeatingKeys.Remove(Key.Right);
            InputSystem.RepeatingKeys.Remove(Key.Up);
            InputSystem.RepeatingKeys.Remove(Key.Down);
        }
    }

    public abstract class OptionBase
    {
        public string FriendlyName { get; set; }
        public string SettingName { get; set; }

        public abstract string GetValue();
        public abstract string GetNextValue();
        public abstract string GetPrevValue();

        public abstract bool CanMoveForward();
        public abstract bool CanMoveBackward();

        public abstract void SaveSettings();

        public abstract void MoveNext();
        public abstract void MovePrevious();
        public abstract void Sanitise();

        public void RaiseOptionChanged()
        {
            OptionCallback.Invoke(this);
        }

        public delegate void OptionCallbackEvent(OptionBase sender);

        public event OptionCallbackEvent OptionCallback;
    }

    public class BoolOption : OptionBase
    {
        public bool Value { get; set; }

        public BoolOption()
        {
            OptionCallback += (s) => CallbackFunction(Value);
        }

        public override bool CanMoveBackward()
        {
            return Value;
        }

        public override bool CanMoveForward()
        {
            return !Value;
        }

        public override string GetNextValue()
        {
            return (!Value).ToString();
        }

        public override string GetPrevValue()
        {
            return (!Value).ToString();
        }

        public override string GetValue()
        {
            return Value.ToString();
        }

        public override void MoveNext()
        {
            Value = !Value;
        }

        public override void MovePrevious()
        {
            Value = !Value;
        }

        public override void Sanitise()
        {
        }

        public override void SaveSettings()
        {
            ServiceLocator.Settings[SettingName] = Value;
        }

        public Action<bool> CallbackFunction { get; set; } = (_) => { };
    }

    public class StringOption : OptionBase
    {
        public string CurrentValue { get { return Values[CurrentIndex]; } }
        public int CurrentIndex { get; set; } = 0;
        public List<string> Values { get; set; }

        public StringOption()
        {
            OptionCallback += (_) => CallbackFunction(CurrentValue);
        }

        public override bool CanMoveBackward()
        {
            return CurrentIndex > 0;
        }

        public override bool CanMoveForward()
        {
            return CurrentIndex < Values.Count - 1;
        }

        public override string GetNextValue()
        {
            return Values[CurrentIndex + 1];
        }

        public override string GetPrevValue()
        {
            return Values[CurrentIndex - 1];
        }

        public override string GetValue()
        {
            return CurrentValue;
        }

        public override void MoveNext()
        {
            CurrentIndex += 1;
        }

        public override void MovePrevious()
        {
            CurrentIndex -= 1;
        }

        public override void Sanitise()
        {
            MathHelper.Clamp(CurrentIndex, 0, Values.Count - 1);
        }

        public override void SaveSettings()
        {
            ServiceLocator.Settings[SettingName] = CurrentValue;
        }

        public Action<string> CallbackFunction { get; set; } = (_) => { };
    }

    public class EnumOption<T> : StringOption
    {
        public override void SaveSettings()
        {
            ServiceLocator.Settings[SettingName] = (int)Enum.Parse(typeof(T), CurrentValue);
        }
    }

    public class NumericOption : OptionBase
    {
        public float Value { get; set; }
        public float Step { get; set; }
        public float Maximum { get; set; }
        public float Minimum { get; set; }
        public bool Round { get; set; }
        public float Scale { get; set; } = 0.0f;

        public NumericOption()
        {
            OptionCallback += (_) => CallbackFunction(Value);
        }

        public override bool CanMoveBackward()
        {
            return Value >= (Minimum + Step);
        }

        public override bool CanMoveForward()
        {
            return Value <= (Maximum - Step);
        }

        public override string GetNextValue()
        {
            return GetNext().ToString();
        }

        public override string GetPrevValue()
        {
            return GetPrevious().ToString();
        }

        public override string GetValue()
        {
            return Value.ToString();
        }

        public override void MoveNext()
        {
            Value = GetNext();
        }

        public override void MovePrevious()
        {
            Value = GetPrevious();
        }

        public override void Sanitise()
        {
            Value = MathHelper.Clamp(Value, Minimum, Maximum);
        }

        public override void SaveSettings()
        {
            ServiceLocator.Settings[SettingName] = Value / Scale;
        }

        private float GetNext()
        {
            var n = Value + Step;
            n = MathHelper.Clamp(n, Minimum, Maximum);
            if (Round) n = (float)Math.Round(n);
            return n;
        }

        private float GetPrevious()
        {
            var n = Value - Step;
            n = MathHelper.Clamp(n, Minimum, Maximum);
            if (Round) n = (float)Math.Round(n);
            return n;
        }

        public Action<float> CallbackFunction { get; set; } = (_) => { };
    }
}
