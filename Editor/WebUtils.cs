using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using SimpleJSON;
using System.Collections.Specialized;
using UnityEditor;
using UnityEngine;

namespace UnityUtils.UnityPM
{
    static class WebUtils
    {
        private const string userAgent = "Mozilla/5.0 (X11; Linux x86_64; rv:91.0) Gecko/20100101 Firefox/91.0";

        public static JSONObject APIPostForm(Uri apiURI, string relativeUri, NameValueCollection formParams)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = userAgent;
                client.Headers[HttpRequestHeader.Accept] = "application/json";

#if UNITY_UTILS_DEBUG
                string msg = $"APIPostForm: {apiURI}\n\n{DebugWebClient(client)}\n\nForm:";
                foreach (string f in formParams)
                    msg += $"\n    {f}: {formParams[f]}";
                Debug.Log(msg);
#endif

                byte[] responsebytes = client.UploadValues(new Uri(apiURI, "oauth/token"), "POST", formParams);
                string responsebody = Encoding.UTF8.GetString(responsebytes);

#if UNITY_UTILS_DEBUG
                Debug.Log($"Response body:\n\n{responsebody}");
#endif

                JSONObject responseJSON = (JSONObject)JSON.Parse(responsebody);

                return responseJSON;
            }
        }
        public static JSONObject APIGet(Uri apiURI, string relativeUri = null, string bearer = null, NameValueCollection queryParams = null)
        {
            using (WebClient client = new WebClient())
            {
                if (!(bearer is null))
                    client.Headers[HttpRequestHeader.Authorization] = $"Bearer {bearer}";

                client.Headers[HttpRequestHeader.UserAgent] = userAgent;
                client.Headers[HttpRequestHeader.Accept] = "application/json";

                client.QueryString = queryParams;

                Uri uri;
                if (relativeUri is null)
                    uri = apiURI;
                else
                    uri = new Uri(apiURI, relativeUri);

#if UNITY_UTILS_DEBUG
                Debug.Log($"APIGet: {uri}\n\n{DebugWebClient(client)}");
#endif

                string responsebody = client.DownloadString(uri);

#if UNITY_UTILS_DEBUG
                Debug.Log($"Response body:\n\n{responsebody}");
#endif

                JSONObject responseJSON = (JSONObject)JSON.Parse(responsebody);

                return responseJSON;
            }
        }
        public static JSONObject APIGet(string apiURI, string relativeUri = null, string bearer = null, NameValueCollection queryParams = null)
        {
            return APIGet(new Uri(apiURI), relativeUri, bearer, queryParams);
        }

        private static string DebugWebClient(WebClient client)
        {
            string msg = $"Headers:";
            foreach (string h in client.Headers)
                msg += $"\n    {h}: {client.Headers[h]}";
            msg += $"\n\nQuery:";
            foreach (string q in client.QueryString)
                msg += $"\n    {q}={client.QueryString[q]}";
            return msg;
        }
    }
}
