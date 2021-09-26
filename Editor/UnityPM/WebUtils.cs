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
        public static JSONObject APIGet(Uri apiURI, string relativeUri, string bearer, NameValueCollection queryParams)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.Authorization] = $"Bearer {bearer}";
                client.QueryString = queryParams;

                string responsebody = client.DownloadString(new Uri(apiURI, relativeUri));
                Debug.Log(responsebody);

                JSONObject responseJSON = (JSONObject)JSON.Parse(responsebody);

                return responseJSON;
            }
        }
    }
}
