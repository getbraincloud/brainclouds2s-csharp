//----------------------------------------------------
// brainCloud client source code
// Copyright 2020 bitHeads, inc.
//----------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.Experimental.Networking;
using UnityEngine.Networking;
using UnityEngine;
using System.IO;
using System.Collections;
using System.Threading;
using System.Runtime.Serialization;
public interface IS2SCallback
{
    /**
     * The serverCallback() method returns server data back to the layer
     * interfacing with the BrainCloud library.
     *
     * @param serviceName - name of the requested service
     * @param serviceOperation - requested operation
     * @param jsonData - returned data from the server
     */
    void s2sCallback(BrainClouds2s context, Dictionary<string, object> jsonData);
    void onAuthenticationCallback(string jsonResponseData);
    void onHeartbeatCallback(string jsonResponseData);
}
internal sealed class BrainClouds2s : IS2SCallback
{
    private static int NO_PACKET_EXPECTED = -1;
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
    private TimeSpan _heartbeatTimer = TimeSpan.FromSeconds(_heartbeatSeconds);
    private DateTime _lastHeartbeat;
    private static Mutex _lock = new Mutex();
    private ArrayList<KeyValuePair<HttpWebRequest, string>> _requestQueue = new ArrayList<KeyValuePair<HttpWebRequest, string>>();
    IS2SCallback s2scallback = new IS2SCallback();
    /**
        * Initialize brainclouds2s context
        *
        * @param appId Application ID
        * @param serverName Server name
        * @param serverSecret Server secret key
        */
    public void Init(string appId, string serverName, string serverSecret)
    {
        Init(appId, serverName, serverSecret, DEFAULT_S2S_URL);
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
    public void Init(string appId, string serverName, string serverSecret, string serverUrl)
    {
        _packetId = 0;
        IsInitialized = true;
        ServerURL = serverUrl;
        AppId = appId;
        ServerSecret = serverSecret;
        ServerName = serverName;
        SessionId = null;
    }
    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void request(string jsonRequestData, IS2SCallback callback)
    {
        if (!isAuthenticated)
        {
            authenticate(callback);
        }
        //make request and add to the request queue
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ServerURL);
        _requestQueue.Add(req, jsonRequestData);
        sendRequest(req, jsonRequestData);
    }
    private void resetHeartbeat()
    {
        _lastHeartbeat = DateTime.Now();
    }
    private Dictionary<string, object> generateError(int statusCode, int reasonCode, string statusMessage)
    {
        Dictionary<string, object> jsonError = new Dictionary<string, object>();
        jsonError.Add("status", statusCode);
        jsonError.Add("reason_code", reasonCode);
        jsonError.Add("serverity", "ERROR");
        jsonError.Add("status_message", statusMessage);
        return jsonError;
    }
    public void authenticate(IS2SCallback callback)
    {
        string jsonAuthString = "{\"service\":\"authenticationV2\",\"operation\":\"AUTHENTICATE\",\"data\":{\"appId\":\"" + AppId + "\",\"serverName\":\"" + ServerName + "\",\"serverSecret\":\"" + ServerSecret + "\"}}";
        _packetId = 0;
        //Dictionary<string, object> authenticationData = new Dictionary<string, object>();
        //authenticationData.Add("appid", AppId);
        //authenticationData.Add("serverName", ServerName);
        //authenticationData.Add("serverSecret", ServerSecret);
        //Dictionary<string, object> authenticationMessage = new Dictionary<string, object>();
        //authenticationMessage.Add("service", "authenticationV2");
        //authenticationMessage.Add("operation", "AUTHENTICATE");
        //authenticationMessage.Add("data", authenticationData);
        //HttpWebRequest req = new HttpWebRequest();
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ServerURL);
        _requestQueue.Add(req, jsonAuthString);
        sendRequest(req, jsonAuthString);
    }
    public void sendHeartbeat(IS2SCallback callback)
    {
        if (SessionId != null)
        {
            string jsonHeartbeatString = "{\"service\":\"heartbeat\",\"operation\":\"HEARTBEAT\"}";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ServerURL);
            _requestQueue.Add(req, jsonHeartbeatString);
            sendRequest(req, jsonHeartbeatString);
        }
        //if (SessionID != null)
        //{
        //    Dictionary<string, object> heartbeatMessage = new Dictionary<string, object>();
        //    heartbeatMessage.Add("service", "heartbeat");
        //    heartbeatMessage.Add("operation", "HEARTBEAT");
        //    sendRequest(heartbeatMessage);
        //}
    }
    private string createPacket(string packetData)
    {
        //form the packet
        string packetDataString = "{\"packetId\":" + (int)_packetId;
        if (SessionId.Length != 0)
        {
            packetDataString += ",\"sessionId\":\"" + SessionId + "\"";
        }
        packetDataString += ",\"messages\":[" + packetData + "]}";
        return packetDataString;
        //ArrayList<string> messages = new ArrayList<Dictionary<string, object>>();
        //messages.Add(packetInfo);
        //Dictionary<string, object> allMessages = new Dictionary<string, object>();
        //allMessages.Add("packetId", PacketId);
        //allMessages.Add("sessionId", SessionID);
        //allMessages.Add("messages", messages);
        //return allMessages;
    }
    private void sendRequest(HttpWebRequest request, string jsonRequestData)
    {
        string data = createPacket(jsonRequestData);
        //need to get dictionary properly serializing to string
        //var dataAsString = string.Join("", data.Select(x => string.Format("{0}", x.Key, "", x.Value)));
        //public static string ToStringFlattened(this Dictionary<string, string> source, string keyValueSeparator = "=", string sequenceSeparator = "|")
        //{
        //    return source == null ? "" : string.Join(sequenceSeparator, source.Keys.Zip(source.Values, (k, v) => k + keyValueSeparator + v));
        //}
        //make request
        //request = (HttpWebRequest)WebRequest.Create(ServerURL);
        request.Method = "POST";
        request.ContentType = "application/json; charset=utf-8";
        //request data
        byte[] byteArray = Encoding.UTF8.GetBytes(data);
        Stream requestStream = request.GetRequestStream();
        requestStream.Write(byteArray, 0, byteArray.Length);
        request.ContentLength = byteArray.Length;
        _packetId++;
    }
    private string readResponseBody(HttpWebResponse response)
    {
        // Get the stream associated with the response.
        Stream receiveStream = response.GetResponseStream();
        // Pipes the stream to a higher level stream reader with the required encoding format. 
        StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
        return readStream.ReadToEnd();
    }
    private void LogString(string s)
    {
        if (_loggingEnabled)
        {
            Console.WriteLine("#BCC " + s);
        }
    }

    public void runCallbacks()
    {
        if (_requestQueue.Count != 0)
        {
            HttpWebRequest activeRequest = _requestQueue[0];
            //Get server response
            HttpWebResponse response = activeRequest.GetResponse();
            //Get server response async
            //HttpWebResponse response = (HttpWebResponse)await Task.Factory.FromAsync<WebResponse>(activeRequest.BeginGetResponse, activeRequest.EndGetResponse, null); 
            //non 200 status, we retry
            if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Forbidden || response.ContentLength == 0)
            {
                if (callback != null)
                {
                    callback.s2sCallback(this, generateError(response.StatusCode, 90001, "Network Error"));
                    return true;
                }
            }
            //get the response body
            string responseBody = readResponseBody(response);
            //status success?
            //200
            if (response.StatusCode == HttpStatusCode.OK)
            {
                //if log enabled...
                //callback
                //remove from the queue and process the next request
                _requestQueue.RemoveAt(0);
                if (_requestQueue.Count > 0)
                {
                    //grab the request at 0, and the data associated with it, and send the request
                    KeyValuePair<HttpWebRequest, string> pair = _requestQueue[0];
                    sendRequest(pair.Key, pair.Value);
                }
            }
            else
            {
                //will want to see about deserialising the response body into a dictionary for easier access to its data.
                //if(statusCode == 40365)
                //cancel the request, and reset it
                //disconnect
                //re-request - will try to reauth
                //return
                //else 
                //log error...
                //cancel request
                //callback
                //remove from the queue and process the next request
                _requestQueue.RemoveAt(0);
                if (_requestQueue.Count > 0)
                {
                    //grab the request at 0, and the data associated with it, and send the request
                    KeyValuePair<HttpWebRequest, string> pair = _requestQueue[0];
                    sendRequest(pair.Key, pair.Value);
                }
            }
            //status failed?
            //status code 900.
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
    public void onAuthenticationCallback(string jsonData)
    {
        //probably going to have to have string parameter for this callback and deserialize the string into a dictionary. 
        if (jsonData != null)
        {
            if (jsonData.ContainsKey("data"))
            {
                Dictionary<string, object> data = jsonData.TryGetValue("data");
                if (data.ContainsKey("heartbeatSeconds"))
                {
                    _heartbeatSeconds = data.TryGetValue("heartbeatSeconds");
                }
                if (data.ContainsKey("sessionId"))
                {
                    SessionId = data.TryGetValue("sessionId");
                }
                resetHeartbeat();
                Authenticated = true;
                //authenticated!
            }
        }
    }
    public void onHeartbeatCallback(string jsonData)
    {
        //probably going to have to have string parameter for this callback and deserialize the string into a dictionary. 
        if (jsonData != null)
        {
            if (jsonData.Contains("status"))
            {
                //Status 200
                if (jsonData.TryGetValue("status") != 200)
                {
                    return;
                }
            }
        }
        disconnect();
    }
}





