//----------------------------------------------------
// brainCloud client source code
// Copyright 2020 bitHeads, inc.
//----------------------------------------------------
#if ((UNITY_5_3_OR_NEWER) && !UNITY_WEBPLAYER && (!UNITY_IOS || ENABLE_IL2CPP)) || UNITY_2018_3_OR_NEWER
#define USE_WEB_REQUEST //Comment out to force use of old WWW class on Unity 5.3+
#endif

using System;
using System.Collections.Generic;
using System.Text;
#if DOT_NET
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
#endif
#if USE_WEB_REQUEST
using UnityEngine.Networking;
using UnityEngine;
#endif
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using BrainCloud.JsonFx.Json;

public class BrainCloudS2S
{
    private static int NO_PACKET_EXPECTED = -1;
    private static int SERVER_SESSION_EXPIRED = 40365;
    private static string DEFAULT_S2S_URL = "https://sharedprod.braincloudservers.com/s2sdispatcher";
    public string ServerURL
    {
        get; private set;
    }
    public string AppId
    {
        get; private set;
    }
    public string ServerSecret
    {
        get; private set;
    }
    public string ServerName
    {
        get; private set;
    }
    public string SessionId
    {
        get; private set;
    }
    public bool IsInitialized
    {
        get; private set;
    }
    public bool LoggingEnabled
    {
        get; set;
    }
    public bool Authenticated
    {
        get; private set;
    }
    private long _packetId = 0;
    private long _heartbeatSeconds = 1800; //Default to 30 mins  
    private TimeSpan _heartbeatTimer;
    private DateTime _lastHeartbeat;
    private ArrayList _requestQueue = new ArrayList();
    public delegate void S2SCallback(Dictionary<string, object> response);

    private struct S2SRequest
    {
#if DOT_NET
        public HttpWebRequest request;
#endif
#if USE_WEB_REQUEST
        public UnityWebRequest request;
#endif
        public string requestData;
        public S2SCallback callback;
    }

    /**
        * Initialize brainclouds2s context
        *
        * @param appId Application ID
        * @param serverName Server name
        * @param serverSecret Server secret key
        */
    public void init(string appId, string serverName, string serverSecret)
    {
        init(appId, serverName, serverSecret, DEFAULT_S2S_URL);
    }

    /**
    * Initialize brainclouds2s context
    *
    * @param appId Application ID
    * @param serverName Server name
    * @param serverSecret Server secret key
    * @param serverUrl The server URL to send the request to. Defaults to the
    * default brainCloud portal
    */
    public void init(string appId, string serverName, string serverSecret, string serverUrl)
    {
        _packetId = 0;
        IsInitialized = true;
        ServerURL = serverUrl;
        AppId = appId;
        ServerSecret = serverSecret;
        ServerName = serverName;
        SessionId = null;
        _heartbeatTimer = TimeSpan.FromSeconds(_heartbeatSeconds);
        Debug.Log("INITIALIZED");
    }

    /**
    * Authenticate with brainCloud
    */
    public void authenticate()
    {
        authenticate(onAuthenticationCallback);
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void request(string jsonRequestData, S2SCallback callback)
    {
        logString("Making Request: " + jsonRequestData);
        if (!Authenticated && _requestQueue.Count == 0) //only do this for the first request a client attempts to make, since we only want to authenticate once. 
        {
            authenticate(onAuthenticationCallback);
        }
        formRequest(jsonRequestData, callback);
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void request(Dictionary<string, object> jsonRequestData, S2SCallback callback)
    {
        logString("Making Request: " + jsonRequestData);
        string jsonString = JsonWriter.Serialize(jsonRequestData);
        if (!Authenticated)
        {
            authenticate(onAuthenticationCallback);
        }
        formRequest(jsonString, callback);
    }

    private void formRequest(string jsonRequestData, S2SCallback callback)
    {
#if DOT_NET
        //create new request
        HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(ServerURL);

        //customize request
        httpRequest.Method = "POST";
        httpRequest.ContentType = "application/json; charset=utf-8";
#endif
#if USE_WEB_REQUEST
        
        //create new request
        UnityWebRequest httpRequest = UnityWebRequest.Post(ServerURL, new Dictionary<string, string>());

        //customize request
        httpRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
#endif
        //store request info
        S2SRequest req = new S2SRequest();
        req.request = httpRequest;
        req.requestData = jsonRequestData;
        req.callback = callback;

        logString("Request: " + req.request + " Data: " + req.requestData);

        //add to requestqueue
        _requestQueue.Add(req);         
    }

    private string createPacket(string packetData)
    {
        //form the packet
        string packetDataString = "{\"packetId\":" + (int)_packetId;
        if (SessionId != null)
        {
            if (SessionId.Length != 0)
            {
                packetDataString += ",\"sessionId\":\"" + SessionId + "\"";
            }
        }
        if (AppId != null)
        {
            packetDataString += ",\"appId\":\"" + AppId + "\"";
        }
        packetDataString += ",\"messages\":[" + packetData + "]}";

        _packetId++;

        logString("Creating Packet: " + packetDataString);

        return packetDataString;
    }
#if DOT_NET
    private void sendData(HttpWebRequest request, string dataPacket)
    {
        string packet = createPacket(dataPacket);                   //create data packet of the data with packetId info

        logString("Sending Request: " + packet);

        byte[] byteArray = Encoding.UTF8.GetBytes(packet);          //convert data packet to byte[]

        Stream requestStream = request.GetRequestStream();          //gets a stream to send dataPacket for request
        requestStream.Write(byteArray, 0, byteArray.Length);        //writes dataPacket to stream and sends data with request. 
        request.ContentLength = byteArray.Length;
    }
#endif

#if USE_WEB_REQUEST
    private void sendData(UnityWebRequest request, string dataPacket)
    {
        string packet = createPacket(dataPacket);                   //create data packet of the data with packetId info

        logString("Sending Request: " + packet);

        byte[] byteArray = Encoding.UTF8.GetBytes(packet);          //convert data packet to byte[]
        request.uploadHandler = new UploadHandlerRaw(byteArray);    //prepare data

        request.SendWebRequest();
    }
#endif


    private void resetHeartbeat()
    {
        _lastHeartbeat = DateTime.Now;
    }

    public void authenticate(S2SCallback callback)
    {
        string jsonAuthString = "{\"service\":\"authenticationV2\",\"operation\":\"AUTHENTICATE\",\"data\":{\"appId\":\"" + AppId + "\",\"serverName\":\"" + ServerName + "\",\"serverSecret\":\"" + ServerSecret + "\"}}";
        _packetId = 0;
        formRequest(jsonAuthString, callback);
    }

    public void sendHeartbeat(S2SCallback callback)
    {
        if (SessionId != null)
        {
            string jsonHeartbeatString = "{\"service\":\"heartbeat\",\"operation\":\"HEARTBEAT\"}";
            formRequest(jsonHeartbeatString, callback);
        }
    }

#if DOT_NET
    private string readResponseBody(HttpWebResponse response)
    {
        Stream receiveStream = response.GetResponseStream();                        // Get the stream associated with the response.
        StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);   // Pipes the stream to a higher level stream reader with the required encoding format. 
        return readStream.ReadToEnd();
    }
#endif

    private void logString(string s)
    {
        if (LoggingEnabled)
        {
#if DOT_NET
            Console.WriteLine("\n#BCC " + s);
#endif
#if USE_WEB_REQUEST
            Debug.Log("\n#BCC " + s);
#endif
        }
    }

    public void runCallbacks()
    {
        if (_requestQueue.Count != 0)
        {
            //make first request in queue the active request
            S2SRequest activeRequest = (S2SRequest)_requestQueue[0];

            //send the request data
            //sendData(activeRequest.request, activeRequest.requestData);
#if DOT_NET
            //Send request and wait for server response
            HttpWebResponse response = null;
            try
            {
               response = (HttpWebResponse)activeRequest.request.GetResponse();
            }
            catch (Exception e)
            {
                logString("S2S Failed: " + e.ToString());
                activeRequest.request.Abort();
                _requestQueue.RemoveAt(0);
                return;
            }
#endif
#if USE_WEB_REQUEST
            string response = null;
            if(activeRequest.request.downloadHandler.isDone)
            {
                logString("IN HERE");
                response = activeRequest.request.downloadHandler.text;
                logString("Response: " + response);
            }
#endif
            if (response != null)
            {
#if DOT_NET
                //get the response body
                string responseString = readResponseBody(response);
#endif
#if USE_WEB_REQUEST
                //get the response body
                string responseString = response;
                logString("Response String: " + responseString);
#endif
                Dictionary<string, object> responseBody = (Dictionary<string, object>)JsonReader.Deserialize(responseString);

                if (responseBody.ContainsKey("messageResponses"))
                {
                    //extract the map array
                    Dictionary<string, object>[] messageArray = (Dictionary<string, object>[])responseBody["messageResponses"];
                    //extract the map from the map array
                    Dictionary<string, object> messageResponses = (Dictionary<string, object>)messageArray.GetValue(0);
                    if ((int)messageResponses["status"] == 200) //success 200
                    {
                        logString("S2S Response: " + responseString);

                        //callback
                        if (activeRequest.callback != null)
                        {
                            activeRequest.callback((Dictionary<string, object>)messageResponses);
                        }

                        //remove the request
                        _requestQueue.RemoveAt(0);
                    }
                    else //failed
                    {
                        //check if its a session expiry
                        if (responseBody.ContainsKey("reason_code"))
                        {
                            if ((int)responseBody["reason_code"] == SERVER_SESSION_EXPIRED)
                            {
                                logString("S2S session expired");
                                activeRequest.request.Abort();
                                disconnect();
                                return;
                            }
                        }

                        logString("S2S Failed: " + responseString);
                        activeRequest.request.Abort();

                        //callback
                        if (activeRequest.callback != null)
                        {
                            activeRequest.callback((Dictionary<string, object>)messageResponses);
                        }

                        //remove the request
                        _requestQueue.RemoveAt(0);
                    }
                }
            }
        }

        //do a heartbeat if necessary.
        if (Authenticated)
        {
            if (DateTime.Now.Subtract(_lastHeartbeat) >= _heartbeatTimer)
            {
                sendHeartbeat(onHeartbeatCallback);
                resetHeartbeat();
            }
        }
    }

    /**
    * Terminate current session from server.
    * (New Session will automatically be created on next request)
    */
    public void disconnect()
    {
        Authenticated = false;
        SessionId = null;
    }

    public void onAuthenticationCallback(Dictionary<string, object> response)
    {
        if (response != null)
        {
            Dictionary<string, object> data = (Dictionary<string, object>)response["data"];
            if (data.ContainsKey("sessionId") && data.ContainsKey("heartbeatSeconds"))
            {
                SessionId = (string)data["sessionId"];
                if (data.ContainsKey("heartbeatSeconds"))
                {
                    _heartbeatSeconds = (int)data["heartbeatSeconds"];
                }
                resetHeartbeat();
                Authenticated = true;
            }
        }
    }

    public void onHeartbeatCallback(Dictionary<string, object> response)
    {
        if (response != null)
        {
            if (response.ContainsKey("status"))
            {
                if ((int)response["status"] == 200)
                {
                    return;
                }
            }
        }
        disconnect();
    }
}
