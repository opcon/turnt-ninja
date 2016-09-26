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
            _sentryClient = new RavenClient(_sentryDsn, new CustomJsonPacketFactory(), null, new SentryUserGUIDFactory(_userGUID));
        }

        public string ReportError(Exception ex)
        {
            return ReportErrorAsync(ex).Result;
        }

        public string ReportMessage(string message)
        {
            return ReportMessageAsync(message).Result;
        }

        public Task<string> ReportErrorAsync(Exception ex)
        {
            var sentryEvent = new SentryEvent(ex);
            return _sentryClient.CaptureAsync(sentryEvent);
        }

        public Task<string> ReportMessageAsync(string message)
        {
            var sentryMessage = new SentryMessage(message);
            var sentryEvent = new SentryEvent(sentryMessage);
            return _sentryClient.CaptureAsync(sentryEvent);
        }
    }

    class CustomJsonPacketFactory : JsonPacketFactory
    {
        protected override JsonPacket OnCreate(JsonPacket jsonPacket)
        {
            // Scrub servername from the json packet since we don't need it
            jsonPacket.ServerName = "";
            return jsonPacket;
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
