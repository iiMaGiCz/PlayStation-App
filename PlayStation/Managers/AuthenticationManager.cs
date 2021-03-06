﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using PlayStation.Entities.User;
using PlayStation.Entities.Web;
using PlayStation.Interfaces;
using PlayStation.Tools;

namespace PlayStation.Managers
{
    public class AuthenticationManager
    {
        private readonly IWebManager _webManager;

        public AuthenticationManager(IWebManager webManager)
        {
            _webManager = webManager;
        }

        public AuthenticationManager()
            : this(new WebManager())
        {
        }

        public async Task<Result> GetUserEntity(UserAuthenticationEntity entity, string lang)
        {
            return await _webManager.GetData(new Uri(EndPoints.VerifyUser), entity, lang);
        }

        public async Task<string> RequestAccessToken(string code)
        {
            try
            {
                var dic = new Dictionary<String, String>();
                dic["grant_type"] = "authorization_code";
                dic["client_id"] = EndPoints.ConsumerKey;
                dic["client_secret"] = EndPoints.ConsumerSecret;
                dic["redirect_uri"] = "com.playstation.PlayStationApp://redirect";
                dic["state"] = "x";
                dic["scope"] = "psn:sceapp";
                dic["code"] = code;
                var theAuthClient = new HttpClient();
                var header = new FormUrlEncodedContent(dic);
                var response = await theAuthClient.PostAsync(EndPoints.OauthToken, header);
                string responseContent = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(responseContent))
                {
                    throw new Exception("Failed to get access token");
                }
                return responseContent;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get access token", ex);
            }
        }

        public async Task<Result> RefreshAccessToken(string refreshToken)
        {
            try
            {
                var dic = new Dictionary<String, String>();
                dic["grant_type"] = "refresh_token";
                dic["client_id"] = EndPoints.ConsumerKey;
                dic["client_secret"] = EndPoints.ConsumerSecret;
                dic["refresh_token"] = refreshToken;
                dic["scope"] = "psn:sceapp";
                var theAuthClient = new HttpClient();
                HttpContent header = new FormUrlEncodedContent(dic);
                var response = await theAuthClient.PostAsync(EndPoints.OauthToken, header);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(responseContent))
                    {
                        throw new Exception("Could not refresh the user token, no response data");
                    }
                    return !string.IsNullOrEmpty(responseContent) ? new Result(true, null, responseContent) : new Result(false, null, null);
                }

                return new Result(false, null, null);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not refresh the user token", ex);
            }
        }

        public async Task<Result> SendLoginData(string userName, string password)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (iPhone; CPU iPhone OS 8_1_2 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Version/8.0 Mobile/12B440 Safari/600.1.4");
            httpClient.DefaultRequestHeaders.Add("Accept-Language", "ja-jp");
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            var ohNoTest = await httpClient.GetAsync(new Uri(EndPoints.Login));

            httpClient.DefaultRequestHeaders.Referrer = new Uri("https://auth.api.sonyentertainmentnetwork.com/login.jsp?service_entity=psn&request_theme=liquid");
            httpClient.DefaultRequestHeaders.Add("Origin", "https://auth.api.sonyentertainmentnetwork.com");
            var nameValueCollection = new Dictionary<string, string>
                {
                { "params", "c2VydmljZV9lbnRpdHk9cHNuJnJlcXVlc3RfdGhlbWU9bGlxdWlkJmF1dGhlbnRpY2F0aW9uX2Vycm9yPXRydWU=" },
                { "rememberSignIn", "On" },
                { "j_username", userName },
                { "j_password", password },
            };

            var form = new FormUrlEncodedContent(nameValueCollection);
            var response = await httpClient.PostAsync(EndPoints.LoginPost, form);
            if (!response.IsSuccessStatusCode)
            {
                return new Result(false, null, null);
            }

            ohNoTest = await httpClient.GetAsync(new Uri(EndPoints.Login));
            var codeUrl = ohNoTest.RequestMessage.RequestUri;
            var queryString = UriExtensions.ParseQueryString(codeUrl.ToString());
            if (queryString.ContainsKey("authentication_error"))
            {
                return new Result(false, null, null);
            }
            if (!queryString.ContainsKey("targetUrl")) return new Result(false, null, null);
            queryString = UriExtensions.ParseQueryString(WebUtility.UrlDecode(queryString["targetUrl"]));
            if (!queryString.ContainsKey("code")) return null;

            var authManager = new AuthenticationManager();
            var authEntity = await authManager.RequestAccessToken(queryString["code"]);
            return !string.IsNullOrEmpty(authEntity) ? new Result(true, null, authEntity) : new Result(false, null, null);
        }
    }
}
