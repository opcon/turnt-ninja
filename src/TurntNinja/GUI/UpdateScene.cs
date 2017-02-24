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
using System.Net.Http;
using System.Diagnostics;

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
        GameFont _updateFont;
        System.Drawing.SizeF _fontBounds;
        QuickFont.QFontRenderOptions _fontRenderOptions = new QuickFont.QFontRenderOptions { Colour = System.Drawing.Color.White };

        const string LOCALPACKAGEHOST = @"D:\Patrick\Documents\Development\Game Related\turnt-ninja\Releases";
        const string GITHUBPACKAGEHOST = "https://github.com/opcon/turnt-ninja";
        const string ITCHRELEASEAPI = @"https://itch.io/api/1/x/wharf/latest";
        const string ITCHTARGET = @"opcon/turnt-ninja";
        const string ITCHALPHACHANNEL = "-ci";
        const string ALPHATAGSUFFIX = "alpha";

        private bool _isSquirrel
        {
            get
            {
                return DistributionMethod.GetDistributionMethod() == Distribution.Squirrel;
            }
        }

        public UpdateScene()
        {
            Exclusive = true;
        }

        public override void Draw(double time)
        {
            var tSize = _updateFont.Font.Measure(_statusString, _fontBounds, QuickFont.QFontAlignment.Centre);
            SceneManager.FontDrawing.Print(_updateFont.Font, _statusString, new OpenTK.Vector3(0, tSize.Height / 2, 0), _fontBounds, QuickFont.QFontAlignment.Centre, _fontRenderOptions);
        }

        public override void Load()
        {
            _fontBounds = new System.Drawing.SizeF(WindowWidth * 0.75f, -1f);
            _updateFont = SceneManager.GameFontLibrary.GetFirstOrDefault(GameFontType.Heading);
            _cancellationTokenSource = new CancellationTokenSource();
            _statusString = "Checking for updates...";

            if (_isSquirrel)
            {
                _statusString = "Checking for updates...";
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
            }
            else
            {
                Task.Run(() =>
                {
                    var informationalVersionAttribute = System.Reflection.Assembly.GetExecutingAssembly().CustomAttributes.FirstOrDefault(cad => cad.AttributeType == typeof(System.Reflection.AssemblyInformationalVersionAttribute));
                    string channelBase = "";
                    switch (PlatformDetection.RunningPlatform())
                    {
                        case Platform.Windows:
                            channelBase = "win";
                            break;
                        case Platform.Linux:
                            channelBase = "linux";
                            break;
                        case Platform.MacOSX:
                            channelBase = "mac";
                            break;
                    }
                    string tag = ((string)informationalVersionAttribute.ConstructorArguments.First().Value).Split(' ')[0].Split(':')[1];
                    string channelName = tag.Contains(ALPHATAGSUFFIX) ? channelBase + ITCHALPHACHANNEL : channelBase;

                    UriBuilder ub = new UriBuilder(ITCHRELEASEAPI);
                    ub.Query = $"target={ITCHTARGET}&channel_name={channelName}";

                    HttpClient hc = new HttpClient();
                    var resp = hc.GetAsync(ub.Uri).Result;
                    var content = Newtonsoft.Json.Linq.JObject.Parse(resp.Content.ReadAsStringAsync().Result);
                    var versionString = content.Value<string>("latest").Split('-');

                    var ver = Version.Parse(versionString[0]);
                    var currentVer = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    var newVersionAvailable = ver > currentVer;

                    // Check tag if we don't need to update based purely on version string
                    if (!newVersionAvailable)
                    {
                        var ctp = tag.Split('-');
                        if (ctp.Length > 1 || (bool)ServiceLocator.Settings["GetAlphaReleases"])
                        {
                            if (versionString.Length > 1)
                            {
                                var currentTag = ctp[1];
                                var serverTag = versionString[1];
                                newVersionAvailable = serverTag.CompareTo(currentTag) == 1;
                            }
                            else
                                newVersionAvailable = true;
                        }
                    }

                    if (newVersionAvailable)
                        _statusString = $"Version {string.Join("-", versionString)} is available.\nYou can update through the Itch.io app, or download the latest release from {@"https://opcon.itch.io/turnt-ninja"}.";
                    else
                        _statusString = "You have the latest version!";
                }, _cancellationTokenSource.Token);
            }

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
                    SceneManager.RemoveScene(this, true);
                    SceneManager.GameWindow.Exit();
                });
            }
            if (_continue || InputSystem.NewKeys.Contains(Key.Escape))
            {
                SceneManager.RemoveScene(this, true);
            }
        }

        public override void CallBack(GUICallbackEventArgs e)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            if (_updateManager != null) _updateManager.Dispose();

            _cancellationTokenSource = null;
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

        public override void EnterFocus()
        {
        }

        public override void ExitFocus()
        {
        }
    }
}
