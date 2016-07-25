using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Substructio.Logging;
using SharpRaven;
using SharpRaven.Data;
using Substructio.Core;

namespace TurntNinja.Logging
{
    class SentryErrorReporting : IErrorReporting
    {
        readonly Dsn _sentryDsn;
        readonly string _userGUID;

        RavenClient _sentryClient;

        public SentryErrorReporting(string sentryURL, string environment, string version, string userGUID, Platform platform, string platformVersion, bool scrubUserName = false)
        {
            _sentryDsn = new Dsn(sentryURL);
            _userGUID = userGUID;
            InitialiseSentryClient();

            _sentryClient.Environment = environment;
            _sentryClient.Release = version;
            _sentryClient.Tags["OS"] = $"{platform} {platformVersion}";

            if (scrubUserName) _sentryClient.LogScrubber = new SentryUserScrubber();
        }

        private void InitialiseSentryClient()
        {
            _sentryClient = new RavenClient(_sentryDsn, null, null, new SentryUserGUIDFactory(_userGUID));
        }

        public void ReportError(Exception ex)
        {
            var sentryEvent = new SentryEvent(ex);
            _sentryClient.Capture(sentryEvent);
        }

        public void ReportMessage(string message)
        {
            var sentryMessage = new SentryMessage(message);
            var sentryEvent = new SentryEvent(sentryMessage);
            _sentryClient.Capture(sentryEvent);
        }
    }

    class SentryUserGUIDFactory : SentryUserFactory
    {
        readonly string _username;

        public SentryUserGUIDFactory(string username)
        {
            _username = username;
        }
        protected override SentryUser OnCreate(SentryUser user)
        {
            return new SentryUser(_username);
        }
    }

    class SentryUserScrubber : SharpRaven.Logging.IScrubber
    {
        public string Scrub(string input)
        {
            var search = "user\":{";
            var start = input.IndexOf(search, StringComparison.Ordinal) + search.Length;
            var end = input.IndexOf("}", start, StringComparison.Ordinal);
            var ret = input.Substring(0, start) + input.Substring(end, input.Length - end);
            return ret;
        }
    }
}
