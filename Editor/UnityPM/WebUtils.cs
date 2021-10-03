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
        public static JSONObject APIPostForm(Uri apiURI, string relativeUri, NameValueCollection formParams)
        {
            using (WebClient client = new WebClient())
            {
                byte[] responsebytes = client.UploadValues(new Uri(apiURI, "oauth/token"), "POST", formParams);
                string responsebody = Encoding.UTF8.GetString(responsebytes);
                Debug.Log(responsebody);

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

                client.Headers[HttpRequestHeader.UserAgent] = "Mozilla/5.0 (X11; Linux x86_64; rv:91.0) Gecko/20100101 Firefox/91.0";
                client.Headers[HttpRequestHeader.Accept] = "application/json";

                client.QueryString = queryParams;

                Uri uri;
                if (relativeUri is null)
                    uri = apiURI;
                else
                    uri = new Uri(apiURI, relativeUri);

                string responsebody = client.DownloadString(uri);
                Debug.Log(responsebody);

                JSONObject responseJSON = (JSONObject)JSON.Parse(responsebody);

                return responseJSON;
            }
        }
        public static JSONObject APIGet(string apiURI, string relativeUri = null, string bearer = null, NameValueCollection queryParams = null)
        {
            return APIGet(new Uri(apiURI), relativeUri, bearer, queryParams);
        }
     }
}
