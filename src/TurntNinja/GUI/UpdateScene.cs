using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.Core;
using Substructio.GUI;
using OpenTK.Graphics;
using Squirrel;
using OpenTK.Input;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;
using System.Threading;

namespace TurntNinja.GUI
{
    class UpdateScene : Scene
    {
        private object _lock = new object();
        private string _statusString = "";
        private bool _continue = false;
        private bool _needToUpdate = false;
        private IUpdateManager _updateManager;
        private UpdateInfo _updateInfo;
        private Dictionary<ReleaseEntry, string> _releaseNotes;
        Exception _ex = null;
        CancellationTokenSource _cancellationTokenSource;

        const string LOCALPACKAGEHOST = @"D:\Patrick\Documents\Development\Game Related\turnt-ninja\Releases";
        const string GITHUBPACKAGEHOST = "https://github.com/opcon/turnt-ninja";

        public UpdateScene()
        {
            Exclusive = true;
        }

        public override void Draw(double time)
        {
            var tSize = SceneManager.DefaultFont.Font.Measure(_statusString);
            SceneManager.DrawTextLine(_statusString, new OpenTK.Vector3(0, tSize.Height/2, 0), Color4.White);
        }

        public override void Load()
        {
            _statusString = "Checking for updates...";
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Run(() =>
            {
                //Check if local package host exists first - if so then update from that
                string packageHost = (Directory.Exists(LOCALPACKAGEHOST)) ? LOCALPACKAGEHOST : GITHUBPACKAGEHOST;
                using (var upmgr = Directory.Exists(LOCALPACKAGEHOST) ? new UpdateManager(packageHost) : UpdateManager.GitHubUpdateManager(packageHost).Result)
                {
                    UpdateInfo updateInfo = null;
                    try
                    {
                        updateInfo = upmgr.CheckForUpdate().Result;
                    }
                    catch (Exception ex)
                    {
                        _ex = ex;
                    }
                    var needToUpdate = (updateInfo.ReleasesToApply.Count > 0);
                    Dictionary<ReleaseEntry, string> releaseNotes = null;

                    //if we need to update
                    if (needToUpdate)
                    {
                        releaseNotes = FetchReleaseNotes(updateInfo.ReleasesToApply, packageHost);
                    }

                    lock (_lock)
                    {
                        _updateManager = upmgr;
                        _needToUpdate = needToUpdate;
                        _updateInfo = updateInfo;
                        _releaseNotes = releaseNotes;

                        try
                        {
                            if (needToUpdate)
                            {
                                //_statusString = "Update Available";
                                var releaseNoteJoined = string.Join("\n\n", releaseNotes.Select(kvp => string.Format("{0}\n{1}", kvp.Key.Version.ToString(), kvp.Value)).Reverse());
                                _statusString = string.Format("New version {0} found\nPress Enter to update or Escape to cancel\n\n{1}",
                                    updateInfo.ReleasesToApply.Last().Version, releaseNoteJoined);
                            }
                            else
                                _continue = true;
                        }
                        catch (Exception ex)
                        {
                            _ex = ex;
                        }
                    }

                }
            }, _cancellationTokenSource.Token);

            Loaded = true;
        }

        public override void Resize(EventArgs e)
        {
        }

        public override void Update(double time, bool focused = false)
        {
            if (_ex != null) throw _ex;
            if (_needToUpdate && InputSystem.NewKeys.Contains(Key.Enter))
            {
                _statusString = "Updating...";

                Task.Run(() =>
                {
                    lock (_lock)
                    {
                        _updateManager.UpdateApp().Wait();
                    }

                }).ContinueWith((prevTask) =>
                {
                    lock (_lock)
                    {
                        _statusString = "Restarting";
                    }
                }).ContinueWith((prevTask) => Task.Delay(500).Wait()).ContinueWith((prevTask) =>
                {
                    lock (_lock)
                    {
                        _updateManager.Dispose();
                        _updateManager = null;
                    }
                    UpdateManager.RestartApp();
                    SceneManager.RemoveScene(this);
                    SceneManager.GameWindow.Exit();
                });
            }
            if (_continue || InputSystem.NewKeys.Contains(Key.Escape))
            {
                //SceneManager.AddScene(new MenuScene(), null);
                SceneManager.RemoveScene(this);
            }
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            _cancellationTokenSource.Cancel();
            if (_updateManager != null) _updateManager.Dispose();
            _updateManager = null;
        }

        public static Dictionary<ReleaseEntry, string> FetchReleaseNotes(List<ReleaseEntry> releasesToApply, string directory)
        {
            return releasesToApply
                .SelectMany(x => {
                    try
                    {
                        var releaseNotes = x.GetReleaseNotes(directory);
                        var splitCharacter = releaseNotes.Contains(Environment.NewLine) ? Environment.NewLine : "\n";
                        var split = releaseNotes.Split(new[] { splitCharacter }, StringSplitOptions.None).ToList();
                        split.RemoveRange(split.Count - 2, 2);
                        split.RemoveAt(0);
                        releaseNotes = RemoveHtmlTags(string.Join(splitCharacter, split));

                        return Return(Tuple.Create(x, releaseNotes));
                    }
                    catch (Exception ex)
                    {
                        return Enumerable.Empty<Tuple<ReleaseEntry, string>>();
                    }
                })
                .ToDictionary(k => k.Item1, v => v.Item2);
        }

        public static IEnumerable<T> Return<T>(T value)
        {
            yield return value;
        }

        public static string RemoveHtmlTags(string html)
        {
            return Regex.Replace(html, "<.+?>", string.Empty);
        }

    }
}
