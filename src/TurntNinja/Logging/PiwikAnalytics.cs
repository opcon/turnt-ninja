using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.Logging;
using Substructio.Core;
using Piwik.Tracker;

namespace TurntNinja.Logging
{
    class PiwikAnalytics : IAnalytics
    {
        readonly string _piwikURL;
        readonly int _appID;
        readonly Platform _platform;
        readonly string _platformVersion;
        readonly int _resX;
        readonly int _resY;
        readonly string _gameVersion;
        readonly string _userID;
        readonly string _baseURL;
        readonly Uri _baseUri;
        readonly string _userAgent;

        private PiwikTracker _piwikTracker;

        public PiwikAnalytics(int appID, string piwikURL, Platform platform, string platformVersion, int resX, int resY, string gameVersion, string userID, string baseURL)
        {
            _piwikURL = piwikURL;
            _appID = appID;
            _platform = platform;
            _platformVersion = platformVersion;
            _resX = resX;
            _resY = resY;
            _gameVersion = gameVersion;
            _userID = userID;
            _baseURL = baseURL;
            _baseUri = new Uri(_baseURL);

            // Initialize tracking client
            _piwikTracker = new PiwikTracker(_appID, _piwikURL);

            // Cookies don't exist without an HTTP Context,
            // but disable them explicitly for peace of mind
            _piwikTracker.disableCookieSupport();

            // Set piwik timeout
            _piwikTracker.setRequestTimeout(10);

            // Set user agent
            _userAgent = BuildUserAgent();
            _piwikTracker.setUserAgent(_userAgent);

            // Set resolution
            _piwikTracker.setResolution(_resX, _resY);

            // Set anonymous user id
            _piwikTracker.setUserId(_userID);

            // Force new visit on first tracking call, since we've just
            // started the application, i.e. a visit
            _piwikTracker.setForceNewVisit();

            // Set default base url
            _piwikTracker.setUrl(_baseURL);
        }

        public void TrackApplicationStartup()
        {
            TrackApplicationView("startup", "Application Startup");
        }

        public void TrackApplicationShutdown()
        {
            TrackApplicationView("shutdown", "Application Shutdown");
        }

        public void TrackEvent(string eventCategory, string eventAction, string eventSubjectName = "", string eventValue = "")
        {
            // Set game version custom dimension for all tracking requests
            _piwikTracker.setCustomTrackingParameter("dimension1", _gameVersion);

            // Track the event
            _piwikTracker.doTrackEvent(eventCategory, eventAction, eventSubjectName, eventValue);
        }

        public void TrackApplicationView(string relativeURL, string title="")
        {
            // Set game version custom dimension for all tracking requests
            _piwikTracker.setCustomTrackingParameter("dimension1", _gameVersion);

            // Set page "url"
            _piwikTracker.setUrl(new Uri(_baseUri, relativeURL).ToString());

            // Track the page view
            _piwikTracker.doTrackPageView(title);
        }

        public void SetCustomVariable(int variableID, string variableName, string variableValue, CustomVariableScope variableScope)
        {
            var scope = (variableScope == CustomVariableScope.ApplicationLaunch) ? CustomVar.Scopes.visit : CustomVar.Scopes.page;
            _piwikTracker.setCustomVariable(variableID, variableName, variableValue, scope);
        }

        private string BuildUserAgent()
        {
            var os = "";
            switch (_platform)
            {
                case Platform.Windows:
                    os = "Windows";
                    break;
                case Platform.Linux:
                    os = "Linux";
                    break;
                case Platform.MacOSX:
                    os = "Mac";
                    break;
            }
            return $"{os} {_platformVersion}";
        }

    }
}
