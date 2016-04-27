using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BeatDetection.Audio;
using BeatDetection.Core;
using BeatDetection.Generation;
using OpenTK;
using OpenTK.Input;
using QuickFont;
using Substructio.Core;
using Substructio.Graphics.OpenGL;
using Substructio.GUI;
using OnsetDetection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Linq;
using System.Net;
using CSCore;

namespace BeatDetection.Game
{
    internal class Stage : IDisposable
    {
        public double TotalTime { get; private set; }
        public double EndTime { get; private set; }
        private AudioFeatures _audioFeatures;

        private DifficultyOptions _difficultyOptions;

        private Random _random;

        public double Overlap { get; set; }

        private double _warmupTime = 2.0f;
        private double _elapsedWarmupTime;
        private double _easeInTime = 2.0f;
        public bool Running;
        public bool Ended;

        public bool AI { get; private set; }

        public ShaderProgram ShaderProgram { get; set; }
        public SceneManager SceneManager { get; set; }

        public QFont MultiplierFont;
        public QFontDrawing MultiplierFontDrawing;
        private string _centerText = "";

        private MemoryStream ms;

        public bool Loaded { get; private set; }

        public int Hits
        {
            get { return StageGeometry.Player.Hits; }
        }

        public int Multiplier { get; set; }

        public int CurrentPolygon
        {
            get {return StageGeometry.CurrentBeat;}
        }

        public int PolygonCount
        {
            get { return StageGeometry.BeatCount; }
        }

        public float ScoreMultiplier
        {
            get { return _difficultyOptions.GetScoreMultiplier(); }
        }

        public bool FinishedEaseIn { get; private set; }

        public StageGeometry StageGeometry;
        public StageAudio _stageAudio;

        private const float WADSWORTH = 0.30f;

        public Stage(SceneManager sceneManager)
        {
            SceneManager = sceneManager;

            MultiplierFont = new QFont(SceneManager.FontPath, 50, new QFontBuilderConfiguration(true), FontStyle.Regular);
            MultiplierFontDrawing = new QFontDrawing();
            MultiplierFontDrawing.ProjectionMatrix = SceneManager.ScreenCamera.ScreenProjectionMatrix;

            _stageAudio = new StageAudio();
        }

        public void LoadAsync(Song song, float audioCorrection, float maxAudioVolume, IProgress<string> progress, PolarPolygon centerPolygon, Player player, DifficultyOptions difficultyOptions)
        {
            progress.Report("Loading audio");
            bool soundcloud = false;
            if (false)
            {
                soundcloud = true;
                string clientID = "74e6e3acb28021e21eb32ef4bc10e995";
                string clientSecret = "";

                SoundCloud.API.Client.ISoundCloudConnector sconnector = new SoundCloud.API.Client.SoundCloudConnector();
                var scclient = sconnector.UnauthorizedConnect(clientID, clientSecret);
                List<SoundCloud.API.Client.Objects.SCTrack> tracks = new List<SoundCloud.API.Client.Objects.SCTrack>();

                //try
                //{
                //    for (int i = 0; i < 10; i++)
                //    {
                //        tracks.AddRange(scclient.Tracks.BeginSearch(SoundCloud.API.Client.Objects.TrackPieces.SCFilter.All).Query("Adele - Hello (Paul Damixie Remix)").Exec(SoundCloud.API.Client.Objects.TrackPieces.SCOrder.CreatedAt, (i * 200), 200));
                //    }
                //}
                //catch (Exception ex)
                //{
                //    Debug.WriteLine(ex.Message);
                //    tracks = null;
                //}

                //tracks.Add(scclient.Resolve.GetTrack("https://soundcloud.com/pauldamixie/adele-hello-paul-damixie-remix/s-RiAlW"));


                //var titles = new List<string>();
                //var orderedTracks = tracks.OrderByDescending(t => t.FavoritingsCount + t.PlaybackCount);

                ////take first track
                //var wantedTrack = orderedTracks.First();

                //var url = wantedTrack.StreamUrl + "?client_id=" + clientID;

                var categories = scclient.Explore.GetExploreCategories();
                var testTrackList = scclient.Explore.GetTracks(categories[5]);

                tracks.AddRange(scclient.Tracks.BeginSearch(SoundCloud.API.Client.Objects.TrackPieces.SCFilter.All, false).Query("antidote").Exec());
                tracks.Clear();
                tracks.AddRange(scclient.Tracks.BeginSearch(SoundCloud.API.Client.Objects.TrackPieces.SCFilter.All).Query("antidote").Exec());

                var url = @"https://r3---sn-ppoxu-hxal.googlevideo.com/videoplayback?initcwndbps=1757500&pl=22&signature=8364C68FED0C92706975C7030FA38C5182D0D6E7.B923CB06C8E2A7934DB599C1CB76331F3433D19E&ratebypass=yes&mv=m&source=youtube&ms=au&sver=3&expire=1446389777&requiressl=yes&mn=sn-ppoxu-hxal&mm=31&ipbits=0&itag=22&id=o-ABRcd0vN3sNaA0g1UxvR9mEEH3KfifE2KZKjNoLIVNEf&mt=1446368144&key=yt6&ip=203.173.35.120&lmt=1441521152868758&dur=434.189&fexp=3300130%2C3310839%2C3310848%2C3311908%2C3312231%2C3312381%2C3312701%2C3312923%2C9407116%2C9407535%2C9408353%2C9408710%2C9414764%2C9415556%2C9416007%2C9416126%2C9416778%2C9417707%2C9418203%2C9419290%2C9421084%2C9421174%2C9421408%2C9421605%2C9422596%2C9423171%2C9423306&pcm2cms=yes&mime=video%2Fmp4&upn=GYPx3Qvi-lg&sparams=dur%2Cid%2Cinitcwndbps%2Cip%2Cipbits%2Citag%2Clmt%2Cmime%2Cmm%2Cmn%2Cms%2Cmv%2Cpcm2cms%2Cpl%2Cratebypass%2Crequiressl%2Csource%2Cupn%2Cexpire&title=Travis%20Scott%20-%20Maria%20I%27m%20Drunk%20feat.%20Justin%20Bieber%20%26%20Young%20Thug%20(Audio)&cpn=nucQTOKLTdWzMvUG";
                url = @"https://r3---sn-ppoxu-hxal.googlevideo.com/videoplayback?initcwndbps=1757500&gir=yes&pl=22&source=youtube&mv=m&signature=AFD42531CEEE6FBF71A40C5CBD7EFD07B1690676.257319B03DCE9910DA6B188E00BBA6BD5572C704&ms=au&clen=6896905&sver=3&expire=1446389777&requiressl=yes&mn=sn-ppoxu-hxal&mm=31&ipbits=0&itag=140&id=o-ABRcd0vN3sNaA0g1UxvR9mEEH3KfifE2KZKjNoLIVNEf&mt=1446368144&key=yt6&ip=203.173.35.120&lmt=1441521095252379&dur=434.189&fexp=3300130%2C3310839%2C3310848%2C3311908%2C3312231%2C3312381%2C3312701%2C3312923%2C9407116%2C9407535%2C9408353%2C9408710%2C9414764%2C9415556%2C9416007%2C9416126%2C9416778%2C9417707%2C9418203%2C9419290%2C9421084%2C9421174%2C9421408%2C9421605%2C9422596%2C9423171%2C9423306&pcm2cms=yes&mime=audio%2Fmp4&upn=GYPx3Qvi-lg&sparams=clen%2Cdur%2Cgir%2Cid%2Cinitcwndbps%2Cip%2Cipbits%2Citag%2Ckeepalive%2Clmt%2Cmime%2Cmm%2Cmn%2Cms%2Cmv%2Cpcm2cms%2Cpl%2Crequiressl%2Csource%2Cupn%2Cexpire&keepalive=yes&title=Travis%20Scott%20-%20Maria%20I%27m%20Drunk%20feat.%20Justin%20Bieber%20%26%20Young%20Thug%20(Audio)&cpn=b3as55_fEKFe4Df-";

                var wr = WebRequest.Create(url);
                var response = wr.GetResponse();
                var ws = response.GetResponseStream();

                //byte[] buffer = new byte[short.MaxValue];
                //int read = 0;
                //while ((read = ws.Read(buffer, 0, buffer.Length)) > 0)
                //{
                //    ms.Write(buffer, 0, read);
                //}

                //ms.Position = 0;
                ////var fs = new FileStream(@"D:\Patrick\Desktop\dl.mp3", FileMode.Create);
                ////ms.CopyTo(fs);
                ////fs.Dispose();
                //ws.Dispose();

                song.SongAudio = CSCore.Codecs.CodecFactory.Instance.GetCodec(response.ResponseUri);

                _stageAudio.Load(song.SongAudio);

                //foreach (var track in orderedTracks)
                //{
                //    titles.Add(track.Title);
                //}

                //Debug.WriteLine(titles.ToString());
            }

            if (!song.SongAudioLoaded)
                song.LoadSongAudio();
            _stageAudio.Load(song.SongAudio);

            var tempStream = new MemoryStream();
            song.SongAudio.WriteToStream(tempStream);
            tempStream.Position = 0;
            IWaveSource detectionSource = new CSCore.Codecs.RAW.RawDataReader(tempStream, song.SongAudio.WaveFormat);

            _stageAudio.MaxVolume = maxAudioVolume;
            _random = new Random(_stageAudio.AudioHashCode);

            _stageAudio.Volume = 0f;
            _stageAudio.Seek(WADSWORTH);
            _stageAudio.Play();

            _stageAudio.FadeIn(1000, _stageAudio.MaxVolume * 0.5f, 0.01f, 0);

            LoadAudioFeatures(detectionSource, audioCorrection, progress, song);

            progress.Report("Building stage geometry");
            //Apply difficulty options to builder options
            var bOptions = new GeometryBuilderOptions(ShaderProgram);
            bOptions.ApplyDifficulty(difficultyOptions);
            _difficultyOptions = difficultyOptions;

            //Build stage geometry
            StageGeometry = new StageGeometryBuilder().Build(_audioFeatures, _random, bOptions);
            StageGeometry.ParentStage = this;

            StageGeometry.CenterPolygon = centerPolygon;
            StageGeometry.Player = player;
            StageGeometry.RotationSpeed = _difficultyOptions.RotationSpeed;

            progress.Report("Load complete");
            Thread.Sleep(1000);

            _stageAudio.CancelAudioFades();
            _stageAudio.FadeOut(500, 0.0f, 0.01f, 2);

            Loaded = true;
        }

        private void LoadAudioFeatures(CSCore.IWaveSource audioSource, float correction, IProgress<string> progress, Song s)
        {
            var options = DetectorOptions.Default;
            options.ActivationThreshold = (float)SceneManager.GameSettings["OnsetActivationThreshold"];
            options.AdaptiveWhitening = (bool)SceneManager.GameSettings["OnsetAdaptiveWhitening"];
            _audioFeatures = new AudioFeatures(options, "../../Processed Songs/", correction + (float)_easeInTime, progress);

            progress.Report("Extracting audio features");
            _audioFeatures.Extract(audioSource, s);
        }

        public void Update(double time)
        {
            if (!Running && !Ended)
            {
                SceneManager.ScreenCamera.TargetScale = new Vector2(1.3f);
                _elapsedWarmupTime += time;
                _centerText = (Math.Ceiling(_easeInTime + _warmupTime - _elapsedWarmupTime)).ToString();
                if (_elapsedWarmupTime > _warmupTime)
                {
                    Running = true;
                    time = _elapsedWarmupTime - _warmupTime;
                }
            }

            if (Running || Ended)
            {
                TotalTime += time;

                if (Running)
                {
                    if (StageGeometry.CurrentBeat == StageGeometry.BeatCount && _stageAudio.IsStopped)
                    {
                        EndTime = TotalTime;
                        Ended = true;
                        Running = false;
                    }
                    _centerText = string.Format("{0}x", Multiplier == -1 ? 0 : Multiplier);

                    if (!FinishedEaseIn)
                    {
                        _centerText = (Math.Ceiling(_easeInTime - TotalTime)).ToString();
                        if (TotalTime > _easeInTime)
                        {
                            _stageAudio.Volume = _stageAudio.MaxVolume;
                            _stageAudio.Play();
                            FinishedEaseIn = true;
                        }
                    }
                }
            }

            if (StageGeometry.CurrentBeat < StageGeometry.BeatCount)
            {
                SceneManager.ScreenCamera.TargetScale =
                    new Vector2(0.9f*
                                (0.80f +
                                 Math.Min(1,
                                     ((StageGeometry.BeatFrequencies[StageGeometry.CurrentBeat] - StageGeometry.MinBeatFrequency)/(StageGeometry.MaxBeatFrequency - StageGeometry.MinBeatFrequency))*
                                     0.5f)));
                SceneManager.ScreenCamera.ScaleChangeMultiplier = Math.Min(StageGeometry.BeatFrequencies[StageGeometry.CurrentBeat], 2)*2;
            }

            if (!InputSystem.CurrentKeys.Contains(Key.F3))
                StageGeometry.Update(time);

            if (InputSystem.NewKeys.Contains(Key.F2)) AI = !AI;

            //Scale multiplier font with beat
            MultiplierFontDrawing.ProjectionMatrix = Matrix4.Mult(Matrix4.CreateScale((float)(0.75 + 0.24f * StageGeometry.CenterPolygon.PulseWidth / StageGeometry.CenterPolygon.PulseWidthMax)), SceneManager.ScreenCamera.ScreenProjectionMatrix);

        }

        public void Draw(double time)
        {
            StageGeometry.Draw(time);

            MultiplierFontDrawing.DrawingPrimitives.Clear();
            MultiplierFontDrawing.Print(MultiplierFont, _centerText, new Vector3(0, MultiplierFont.Measure("0", QFontAlignment.Centre).Height * 0.5f, 0),
                QFontAlignment.Centre);
            MultiplierFontDrawing.RefreshBuffers();
            MultiplierFontDrawing.Draw();
        }

        public void Dispose()
        {
            MultiplierFont.Dispose();
            MultiplierFontDrawing.Dispose();
            StageGeometry.Dispose();
        }

        public void Reset()
        {
            _stageAudio.FadeOut(1000, 0, 0.01f, 2);
            StageGeometry.CenterPolygon.Position.Azimuth = 0;
            //reset hit hexagons
            StageGeometry.Player.Hits = 0;
        }
    }
}
