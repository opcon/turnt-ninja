using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using QuickFont;
using Substructio.Core;
using Substructio.GUI;
using BeatDetection.Game;
using System.IO;
using LiteDB;
using System.Globalization;

namespace BeatDetection.GUI
{
    class EndGameScene : Scene
    {
        private QFont _font;
        private QFontDrawing _fontDrawing;
        private SizeF _endGameTextSize;
        private string _endGameText = "Press Enter to Continue";
        private Stage _stage;
        private HighScoreEntry _highScoreEntry;
        private bool _newHighScore = false;
        private PlayerScore _newScore;
        private PlayerScore _highestScore;

        public EndGameScene(Stage stage)
        {
            _stage = stage;
            Exclusive = true;
        }

        public override void Load()
        {
            _font = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Regular);
            _fontDrawing = new QFontDrawing();
            SceneManager.RemoveScene(ParentScene);
            SceneManager.ScreenCamera.ExtraScale = 0;
            SceneManager.ScreenCamera.Scale = SceneManager.ScreenCamera.TargetScale = new Vector2(1, 1);

            //save song to db
            string dbFile = Path.Combine(SceneManager.Directories["AppData"].FullName, (string)SceneManager.GameSettings["DatabaseFile"]);

            using (var db = new LiteDatabase(dbFile))
            {
                var highSccoreCollection = db.GetCollection<HighScoreEntry>("highscores");
                long hash = (long)Utilities.FNV1aHash64(Encoding.Default.GetBytes(_stage.CurrentSong.SongBase.InternalName));

                //does this song exist in the database?
                if (highSccoreCollection.Exists(Query.And(Query.EQ("SongID", hash), Query.EQ("DifficultyLevel", _stage.CurrentDifficulty.ToString()))))
                {
                    _highScoreEntry = highSccoreCollection.FindOne(Query.EQ("SongID", hash));
                }
                else
                {
                    _highScoreEntry = new HighScoreEntry { SongID = hash, SongName = _stage.CurrentSong.SongBase.Identifier, Difficulty = _stage.CurrentDifficulty };
                }

                _highestScore = _highScoreEntry.HighScores.Count > 0 ? _highScoreEntry.HighScores.OrderByDescending(ps => ps.Score).First() : new PlayerScore();
                _newScore = new PlayerScore
                {
                    Name = (string)SceneManager.GameSettings["PlayerName"],
                    Accuracy = 100 - ((float)_stage.Hits / _stage.StageGeometry.BeatCount) * 100.0f,
                    Score = (long)_stage.StageGeometry.Player.Score
                };

                _highScoreEntry.HighScores.Add(_newScore);
                _highScoreEntry.HighScores.OrderByDescending(ps => ps.Score);

                // Save to DB
                db.BeginTrans();
                if (_highScoreEntry.Id == null || !highSccoreCollection.Update(_highScoreEntry)) highSccoreCollection.Insert(_highScoreEntry);
                //end
                db.Commit();

                if (_newScore.Score > _highestScore.Score)
                {
                    _highestScore = _newScore;
                    _newHighScore = true;
                }
            }

            UpdateText();
            Loaded = true;
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
        }

        public override void Resize(EventArgs e)
        {
            UpdateText();
        }

        private void UpdateText()
        {
            _fontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;
            _fontDrawing.DrawingPrimitives.Clear();
            _endGameTextSize = _font.Measure(_endGameText);

            float fontOffset = 0;
            float endOffset = 0;

            if (_newHighScore)
            {
                fontOffset += _fontDrawing.Print(_font, "New High Score", new Vector3(0, 2.0f * _endGameTextSize.Height, 0), QFontAlignment.Centre, Color.White).Height;
                fontOffset += _fontDrawing.Print(_font, string.Format("Score: {0}", _highestScore.Score.ToString("N0", CultureInfo.CurrentCulture)), new Vector3(0, 0, 0), QFontAlignment.Centre, Color.White).Height;
                fontOffset += _fontDrawing.Print(_font, string.Format("Accuracy: {0}%", _highestScore.Accuracy.ToString("#.##")), new Vector3(0, - _endGameTextSize.Height, 0), QFontAlignment.Centre, Color.White).Height;
                endOffset = -3.0f * _endGameTextSize.Height;
            }
            else
            {
                fontOffset += _fontDrawing.Print(_font, string.Format("High Score: {0}", _highestScore.Score), new Vector3(0, 2.0f * _endGameTextSize.Height, 0), QFontAlignment.Centre, Color.White).Height;
                fontOffset += _fontDrawing.Print(_font, string.Format("Score: {0}", _newScore.Score.ToString("N0", CultureInfo.CurrentCulture)), new Vector3(0, 0, 0), QFontAlignment.Centre, Color.White).Height;
                fontOffset += _fontDrawing.Print(_font, string.Format("Accuracy: {0}%", _newScore.Accuracy.ToString("#.##")), new Vector3(0, -_endGameTextSize.Height, 0), QFontAlignment.Centre, Color.White).Height;
                endOffset = -3.0f * _endGameTextSize.Height;
            }
            _fontDrawing.Print(_font, _endGameText, new Vector3(0, -(WindowHeight)/2.0f + _endGameTextSize.Height + 20, 0), QFontAlignment.Centre, Color.White);
            _fontDrawing.Print(_font, string.Format("{0} - {1}", _stage.CurrentSong.SongBase.Identifier, _stage.CurrentDifficulty), new Vector3(0, (WindowHeight)/2.0f - 20, 0), QFontAlignment.Centre, Color.White);
            _fontDrawing.RefreshBuffers();
        }

        public override void Update(double time, bool focused = false)
        {
            if (InputSystem.NewKeys.Contains(Key.Enter) || InputSystem.NewKeys.Contains(Key.Escape) || InputSystem.NewKeys.Contains(Key.Space))
            {
                _stage.StageGeometry.Player.Reset();
                SceneManager.RemoveScene(this);
            }
        }

        public override void Draw(double time)
        {
            _fontDrawing.Draw();
        }

        public override void Dispose()
        {
            _font.Dispose();
            _fontDrawing.Dispose();
        }
    }
}
