using System;
using System.Net;
using System.Threading;
using Newtonsoft.Json;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace SpoffyConsole
{
    public sealed class SpoffyWebAuth
    {
        public string SpoffyEndpoint { get; private set; }
        public string ClientId { get; private set; }
        public int RedirPort { get; private set; }
        public string RedirUri { get; private set; }
        public Scope Scope { get; private set; }

        public bool AuthRequesting { get; private set; }

        public SpoffyWebAuth(string endpoint, string clientId, int redirPort, string redirUri, Scope scope)
        {
            SpoffyEndpoint = endpoint;
            ClientId = clientId;
            RedirPort = redirPort;
            RedirUri = redirUri + ":" + redirPort;
            Scope = scope;
        }

        public void RequestAuth(AutorizationCodeAuth.OnResponseReceived codeReceiveCallback)
        {
            if (AuthRequesting)
                return;

            AuthRequesting = true;

            AutorizationCodeAuth auth = new AutorizationCodeAuth();
            auth.ClientId = ClientId;
            auth.RedirectUri = RedirUri;
            auth.Scope = Scope;
            
            auth.OnResponseReceivedEvent += response =>
            {
                AuthRequesting = false;
                auth.StopHttpServer();
                codeReceiveCallback.Invoke(response);
            };

            auth.StartHttpServer(RedirPort);

            auth.DoAuth();

            int ms = 0;
            while (ms < 30000)
            {
                if (!AuthRequesting)
                    break;

                Thread.Sleep(100);
                ms += 100;
            }

            if (AuthRequesting)
            {
                auth.StopHttpServer();
                AuthRequesting = false;
            }
        }

        public Token TradeAuthForToken(string authCode)
        {
            try
            {
                WebClient wc = new WebClient();
                JsonStatus ret = JsonConvert.DeserializeObject<JsonStatus>(wc.DownloadString(SpoffyEndpoint + "Auth?authCode=" + authCode));
                if (ret.status == "200")
                    return JsonConvert.DeserializeObject<Token>(ret.data.ToString());
                return new Token() {Error = ret.data.ToString()};
            }
            catch (Exception ex)
            {
                return new Token() {Error = ex.ToString()};
            }
        }

        public Token RefreshToken(string refresh)
        {
            try
            {
                WebClient wc = new WebClient();
                JsonStatus ret = JsonConvert.DeserializeObject<JsonStatus>(wc.DownloadString(SpoffyEndpoint + "Refresh?token=" + refresh));
                if (ret.status == "200")
                    return JsonConvert.DeserializeObject<Token>(ret.data.ToString());

                return new Token() {Error = ret.data.ToString()};
            }
            catch (Exception ex)
            {
                return new Token() {Error = ex.ToString()};
            }
        }
    }

    //What the webservice returns
    public class JsonStatus
    {
        public string status { get; set; }
        public object data { get; set; }
    }
}