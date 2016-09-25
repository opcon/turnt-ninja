using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.Logging;
using GameAnalyticsSDK.Net;

namespace TurntNinja.Logging
{
    class GameAnalytics : IAnalytics
    {
        public GameAnalytics(string version, string user)
        {
            GameAnalyticsSDK.Net.GameAnalytics.SetEnabledInfoLog(true);
            GameAnalyticsSDK.Net.GameAnalytics.SetEnabledVerboseLog(true);
            GameAnalyticsSDK.Net.GameAnalytics.ConfigureBuild(version);
            GameAnalyticsSDK.Net.GameAnalytics.ConfigureUserId(user);

            GameAnalyticsSDK.Net.GameAnalytics.Initialize("684fb5703bd0f1ce5dbbbebdf2d7bfa6", "04e267c364e6fe4cc21d18366ca0703612b2e264");
        }
        public void SetCustomVariable(int variableID, string variableName, string variableValue, CustomVariableScope variableScope)
        {
        }

        public void TrackApplicationShutdown()
        {
            GameAnalyticsSDK.Net.GameAnalytics.EndSession();
        }

        public void TrackApplicationStartup()
        {
            GameAnalyticsSDK.Net.GameAnalytics.StartSession();
        }

        public void TrackApplicationView(string relativeURL, string title = "")
        {
            GameAnalyticsSDK.Net.GameAnalytics.AddProgressionEvent(EGAProgressionStatus.Undefined, relativeURL, title);
        }

        public void TrackEvent(string eventCategory, string eventAction, string eventSubjectName = "", string eventValue = "")
        {
            GameAnalyticsSDK.Net.GameAnalytics.AddProgressionEvent(EGAProgressionStatus.Undefined, eventCategory, eventAction, eventSubjectName);
        }
    }
}
