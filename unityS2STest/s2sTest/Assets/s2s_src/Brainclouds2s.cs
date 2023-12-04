//----------------------------------------------------
// brainCloud client source code
// Copyright 2020 bitHeads, inc.
//----------------------------------------------------
#if ((UNITY_5_3_OR_NEWER) && !UNITY_WEBPLAYER && (!UNITY_IOS || ENABLE_IL2CPP)) || UNITY_2018_3_OR_NEWER
#define USE_WEB_REQUEST //Comment out to force use of old WWW class on Unity 5.3+
#else
#define DOT_NET
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
    private static string DEFAULT_S2S_URL = "https://api.internal.braincloudservers.com/s2sdispatcher";
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

    public enum State
    {
        Authenticated,
        Authenticating,
        Disconnected
    }
    
    public enum RTTConnectionStatus
    {
        CONNECTED,
        DISCONNECTED,
        CONNECTING,
        DISCONNECTING
    }
    
    public enum WebsocketStatus
    {
        OPEN,
        CLOSED,
        MESSAGE, 
        ERROR,
        NONE
    }

    private long _packetId = 0;
    private long _heartbeatSeconds = 1800; //Default to 30 mins  
    private State _state = State.Disconnected;
    private RTTConnectionStatus _rttConnectionStatus = RTTConnectionStatus.DISCONNECTED;
    private WebsocketStatus _webSocketStatus = WebsocketStatus.NONE;
    private bool _autoAuth = false;
    public bool _channelConnected = false;
    private TimeSpan _heartbeatTimer;
    private DateTime _lastHeartbeat;
    private TimeSpan _sinceLastRTTHeartbeat;
    private TimeSpan _heartbeatRTTTime = TimeSpan.FromMilliseconds(10 * 1000);
    private ArrayList _requestQueue = new ArrayList();
    private ArrayList _waitingForAuthRequestQueue = new ArrayList();
    public delegate void S2SCallback(string responseString);
    public delegate void RTTCallback(string responseString);
    private Dictionary<string, RTTCallback> _registeredCallbacks = new Dictionary<string, RTTCallback>();
    S2SRequest activeRequest;
    private S2SRequest rttConnectionCallback;
    private BrainCloudWebSocket _webSocket;
    private Dictionary<string, object> _rttHeaders = new Dictionary<string, object>();
    private Dictionary<string, object> _endpoint = null;
    private List<RTTCommandResponse> _queuedRTTCommands = new List<RTTCommandResponse>();
    private bool _disconnectedWithReason = false;
    private Dictionary<string, object> _disconnectJson = new Dictionary<string, object>();
    public string RTTConnectionID { get; private set; }
    public string RTTEventServer { get; private set; }

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
    private struct RTTCommandResponse
    {
        public RTTCommandResponse(string in_service, string in_op, string in_msg)
        {
            Service = in_service;
            Operation = in_op;
            JsonMessage = in_msg;
        }
        public string Service { get; set; }
        public string Operation { get; set; }
        public string JsonMessage { get; set; }
    }

    /**
        * Initialize brainclouds2s context
        *
        * @param appId Application ID
        * @param serverName Server name
        * @param serverSecret Server secret key
        * @param autoAuth automatic authentication with braincloud
        */
    public void Init(string appId, string serverName, string serverSecret, bool autoAuth)
    {
        Init(appId, serverName, serverSecret, autoAuth, DEFAULT_S2S_URL);
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
    public void Init(string appId, string serverName, string serverSecret, bool autoAuth, string serverUrl)
    {
        _packetId = 0;
        IsInitialized = true;
        ServerURL = serverUrl;
        AppId = appId;
        ServerSecret = serverSecret;
        ServerName = serverName;
        _autoAuth = autoAuth;
        SessionId = null;
        activeRequest.request = null;
        _heartbeatTimer = TimeSpan.FromSeconds(_heartbeatSeconds);
    }

    /**
    * Authenticate with brainCloud
    */
    public void Authenticate()
    {
        Authenticate(OnAuthenticationCallback);
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void Request(string jsonRequestData, S2SCallback callback)
    {
        if (_autoAuth == true)
        {
            if (!(_state == State.Authenticated) && _packetId == 0) //this is an authentication request no matter what
            {
                Authenticate(OnAuthenticationCallback);
            }
        }
        if (!(_state == State.Authenticated)) // these are the requests that have been made that are awaiting authentication. We NEED to store the request so we can properly call this function back for additional requests that are made after authenitcation.
        {
            S2SRequest nonAuthRequest = new S2SRequest();
            nonAuthRequest.requestData = jsonRequestData;
            nonAuthRequest.callback = callback;

            _waitingForAuthRequestQueue.Add(nonAuthRequest);
        }
        else
        {
            QueueRequest(jsonRequestData, callback);
        }
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void Request(Dictionary<string, object> jsonRequestData, S2SCallback callback)
    {
        string jsonRequestDataString = JsonWriter.Serialize(jsonRequestData);
        if (_autoAuth == true)
        {
            if (!(_state == State.Authenticated) && _packetId == 0) //this is an authentication request no matter what
            {
                Authenticate(OnAuthenticationCallback);
            }
        }
        if (!(_state == State.Authenticated)) // these are the requests that have been made that are awaiting authentication. We NEED to store the request so we can properly call this function back for additional requests that are made after authenitcation.
        {
            S2SRequest nonAuthRequest = new S2SRequest();
            nonAuthRequest.requestData = jsonRequestDataString;
            nonAuthRequest.callback = callback;

            _waitingForAuthRequestQueue.Add(nonAuthRequest);
        }
        else
        {
            QueueRequest(jsonRequestDataString, callback);
        }
    }

    private void QueueRequest(string jsonRequestData, S2SCallback callback)
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

        //add to requestqueue
        _requestQueue.Add(req);

        SendData(req.request, req.requestData);
    }

    private string CreatePacket(string packetData)
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

        return packetDataString;
    }
#if DOT_NET
    private void SendData(HttpWebRequest request, string dataPacket)
    {
        string packet = CreatePacket(dataPacket);                   //create data packet of the data with packetId info

        byte[] byteArray = Encoding.UTF8.GetBytes(packet);          //convert data packet to byte[]

        Stream requestStream = request.GetRequestStream();          //gets a stream to send dataPacket for request
        requestStream.Write(byteArray, 0, byteArray.Length);        //writes dataPacket to stream and sends data with request. 
        request.ContentLength = byteArray.Length;
    }
#endif

#if USE_WEB_REQUEST
    private void SendData(UnityWebRequest request, string dataPacket)
    {
        string packet = CreatePacket(dataPacket);                   //create data packet of the data with packetId info

        if(LoggingEnabled)
        {
            LogString("Sending Request: " + packet);            
        }
        
        byte[] byteArray = Encoding.UTF8.GetBytes(packet);          //convert data packet to byte[]
        request.uploadHandler = new UploadHandlerRaw(byteArray);    //prepare data

        request.SendWebRequest();
    }
#endif


    private void ResetHeartbeat()
    {
        _lastHeartbeat = DateTime.Now;
    }

    public void Authenticate(S2SCallback callback)
    {
        _state = State.Authenticating;
        string jsonAuthString = "{\"service\":\"authenticationV2\",\"operation\":\"AUTHENTICATE\",\"data\":{\"appId\":\"" + AppId + "\",\"serverName\":\"" + ServerName + "\",\"serverSecret\":\"" + ServerSecret + "\"}}";
        _packetId = 0;
        QueueRequest(jsonAuthString, callback + OnAuthenticationCallback); //We need to call OnAuthenticate callback to refill the queue with requests waiting on an auth request, and handle heartbeat and sessionId data. 
    }

    public void SendHeartbeat(S2SCallback callback)
    {
        if (SessionId != null)
        {
            string jsonHeartbeatString = "{\"service\":\"heartbeat\",\"operation\":\"HEARTBEAT\"}";
            QueueRequest(jsonHeartbeatString, callback);
        }
    }
    
    public void EnableRTT(S2SCallback callback)
    {
        if(SessionId != null)
        {
            Dictionary<string, object> requstInfo = new Dictionary<string, object>();
            requstInfo = new Dictionary<string, object>();
            requstInfo.Add("service", "rttRegistration");
            requstInfo.Add("operation", "REQUEST_SYSTEM_CONNECTION");
            string contextInfo = JsonWriter.Serialize(requstInfo);
            rttConnectionCallback = new S2SRequest();
            rttConnectionCallback.callback = callback;
            rttConnectionCallback.requestData = contextInfo;
            QueueRequest(contextInfo, OnRTTConnectCallback);
        }
        else if(callback != null)
        {
            callback("Unable to request RTT connection without a session established, please authenticate and try again.");
        }
    }
    
    public void DisableRTT()
    {
        if (_rttConnectionStatus != RTTConnectionStatus.CONNECTED || _rttConnectionStatus == RTTConnectionStatus.DISCONNECTING)
        {
            return;
        }
        AddQueueRTTResponse(new RTTCommandResponse("rttRegistration", "disconnect", "DisableRTT Called"));
    }
    
    private void DisconnectFromRTT()
    {
        if (_webSocket != null) _webSocket.Close();

        RTTConnectionID = "";
        RTTEventServer = "";
        _channelConnected = false;
        _webSocket = null;

        if (_disconnectedWithReason == true)
        {
            if (LoggingEnabled)
            {
                LogString("RTT: Disconnect: " + JsonWriter.Serialize(_disconnectJson));
            }
            if (rttConnectionCallback.callback != null)
            {
                rttConnectionCallback.callback((string)_disconnectJson["reason"]);
            }
        }
        _rttConnectionStatus = RTTConnectionStatus.DISCONNECTED;
    }

#if DOT_NET
    private string ReadResponseBody(HttpWebResponse response)
    {
        Stream receiveStream = response.GetResponseStream();                        // Get the stream associated with the response.
        StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);   // Pipes the stream to a higher level stream reader with the required encoding format. 
        return readStream.ReadToEnd();
    }
#endif

    private void LogString(string s)
    {
        if (LoggingEnabled)
        {
#if DOT_NET
            Console.WriteLine("\n#S2S " + s);
#endif
#if USE_WEB_REQUEST
            Debug.Log("\n#S2S " + s);
#endif
        }
    }

    public void RunCallbacks()
    {
        if (activeRequest.request == null) //if there is no active request, make the first in the queue the active request.
        {
            if (_requestQueue.Count != 0) //make sure the queue isn't empty
            {
                activeRequest = (S2SRequest)_requestQueue[0];
            }
        }
        else //on an update, if we have an active request we need to process it. This is VITAL for WEB_REQUEST becasue it handles requests differently than DOT_NET
        {

#if DOT_NET
            HttpWebResponse csharpResponse = null;

            try
            {
                LogString("Sending Request: " + activeRequest.requestData);
                csharpResponse = (HttpWebResponse)activeRequest.request.GetResponse();
            }
            catch (Exception e)
            {
                LogString("S2S Failed: " + e.ToString());
                activeRequest.request.Abort();
                activeRequest.request = null;
                _requestQueue.RemoveAt(0);
                return;
            }
#endif
#if USE_WEB_REQUEST
            string unityResponse = null;
            if(activeRequest.request.downloadHandler.isDone)
            {
                unityResponse = activeRequest.request.downloadHandler.text;
            }
            if(!string.IsNullOrEmpty(activeRequest.request.error))
            {
                LogString("S2S Failed: " + activeRequest.request.error);
                activeRequest.callback(activeRequest.request.error);
                activeRequest.request.Abort();
                activeRequest.request = null;
                _requestQueue.RemoveAt(0);
            }
#endif

#if DOT_NET
            if (csharpResponse != null)
            {
                //get the response body
                string responseString = ReadResponseBody(csharpResponse);
#endif
#if USE_WEB_REQUEST
            if (unityResponse != null)
            {
            //get the response body
            string responseString = unityResponse;
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
                        LogString("S2S Response: " + responseString);

                        //callback
                        if (activeRequest.callback != null)
                        {
                            activeRequest.callback(JsonWriter.Serialize((Dictionary<string, object>)messageResponses));
                        }

                        //remove the request finished request form the queue
                        _requestQueue.RemoveAt(0);
                    }
                    else //failed
                    {
                        //check if its a session expiry
                        if (responseBody.ContainsKey("reason_code"))
                        {
                            if ((int)responseBody["reason_code"] == SERVER_SESSION_EXPIRED)
                            {
                                LogString("S2S session expired");
                                activeRequest.request.Abort();
                                Disconnect();
                                return;
                            }
                        }

                        LogString("S2S Failed: " + responseString);

                        //callback
                        if (activeRequest.callback != null)
                        {
                            activeRequest.callback(JsonWriter.Serialize((Dictionary<string, object>)messageResponses));
                        }

                        activeRequest.request.Abort();

                        //remove the finished request from the queue
                        _requestQueue.RemoveAt(0);
                    }
                }
                activeRequest.request = null; //reset the active request so that it can move onto the next request. 
            }
        }
        //do a heartbeat if necessary.
        if (_state == State.Authenticated)
        {
            if (DateTime.Now.Subtract(_lastHeartbeat) >= _heartbeatTimer)
            {
                SendHeartbeat(OnHeartbeatCallback);
                ResetHeartbeat();
            }
        }
        
        if(_rttConnectionStatus != RTTConnectionStatus.DISCONNECTED || _queuedRTTCommands.Count > 0)
        {
            RTTUpdate();
        }
    }
    
    public bool IsRTTEnabled()
    {
        return _rttConnectionStatus == RTTConnectionStatus.CONNECTED;
    }
    
    private void RTTUpdate()
    {
        RTTCommandResponse toProcessResponse;
        lock (_queuedRTTCommands)
        {
            for (int i = 0; i < _queuedRTTCommands.Count; ++i)
            {
                toProcessResponse = _queuedRTTCommands[i];

                //the rtt websocket has closed and RTT needs to be re-enabled. disconnect is called to fully reset connection
                //Failure Callback 
                if (_webSocketStatus == WebsocketStatus.CLOSED)
                {
                    rttConnectionCallback.callback("RTT Connection has been closed. Re-Enable RTT to re-establish connection :" + toProcessResponse.JsonMessage);
                    _rttConnectionStatus = RTTConnectionStatus.DISCONNECTING;
                    DisconnectFromRTT();
                    break;
                }

                //the rtt websocket has closed and RTT needs to be re-enabled. disconnect is called to fully reset connection 
                //Failure callback
                if (_webSocketStatus == WebsocketStatus.CLOSED)
                {
                    rttConnectionCallback.callback("RTT Connection has been closed. Re-Enable RTT to re-establish connection : " + toProcessResponse.JsonMessage);
                    _rttConnectionStatus = RTTConnectionStatus.DISCONNECTING;
                    DisconnectFromRTT();
                    break;
                }

                // does this go to one of our registered service listeners? 
                if (_registeredCallbacks.ContainsKey(toProcessResponse.Service))
                {
                    _registeredCallbacks[toProcessResponse.Service](toProcessResponse.JsonMessage);
                }

                // are we actually connected? only pump this back, when the server says we've connected
                //Success Callback
                else if (_rttConnectionStatus == RTTConnectionStatus.CONNECTING && rttConnectionCallback.callback != null && toProcessResponse.Operation == "connect")
                {
                    _sinceLastRTTHeartbeat = DateTime.Now.TimeOfDay;
                    rttConnectionCallback.callback(toProcessResponse.JsonMessage);
                    _rttConnectionStatus = RTTConnectionStatus.CONNECTED;
                }

                //if we're connected and we get a disconnect - we disconnect the comms... 
                //Failure Callback
                else if (_rttConnectionStatus == RTTConnectionStatus.CONNECTED && rttConnectionCallback.callback != null && toProcessResponse.Operation == "disconnect")
                {
                    _rttConnectionStatus = RTTConnectionStatus.DISCONNECTING;
                    DisconnectFromRTT();
                }

                //If there's an error, we send back the error
                //Failure callback
                else if (rttConnectionCallback.callback != null && toProcessResponse.Operation == "error")
                {
                    if(toProcessResponse.JsonMessage != null)
                    {   
                        rttConnectionCallback.callback(toProcessResponse.JsonMessage);
                    }
                    else
                    {
                        rttConnectionCallback.callback("Error - No Response from Server");
                    }
                }

                //if we're not connected and we're trying to connect, then start the connection
                else if (_rttConnectionStatus == RTTConnectionStatus.DISCONNECTED && toProcessResponse.Operation == "connect")
                {
                    // first time connecting? send the server connection call
                    _rttConnectionStatus = RTTConnectionStatus.CONNECTING;
                    SendRTTRequest(buildConnectionRequest());
                }
                else
                {
                    if (LoggingEnabled)
                    {
                        LogString("WARNING no handler registered for RTT callbacks ");
                    }
                }

            }

            _queuedRTTCommands.Clear();
        }

        if (_rttConnectionStatus == RTTConnectionStatus.CONNECTED)
        {
            if ((DateTime.Now.TimeOfDay - _sinceLastRTTHeartbeat) >= _heartbeatRTTTime)
            {
                _sinceLastRTTHeartbeat = DateTime.Now.TimeOfDay;
                Dictionary<string, object> json = new Dictionary<string, object>();
                json["service"] = "rtt";
                json["operation"] = "HEARTBEAT";
                json["data"] = null;
                string heartBeatRequest = JsonWriter.Serialize(json);
                SendRTTRequest(heartBeatRequest, true);
            }
        }
    }

    public void RegisterRTTRawCallback(RTTCallback callback)
    {
        if(SessionId != null)
        {
            _registeredCallbacks["chat"] = callback;
        }
        else
        {
            LogString("Authentication is required to register callback");
        }
    }
    
    public void ConnectToChannel(string in_channelId,S2SCallback callback)
    {
        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["service"] = "chat";
        jsonData["operation"] = "SYS_CHANNEL_CONNECT";
        Dictionary<string, object> data = new Dictionary<string, object>();
        data["channelId"] = in_channelId; 
        data["maxReturn"] = 100;
        jsonData["data"] = data;
        string jsonString = JsonWriter.Serialize(jsonData);
        QueueRequest(jsonString, (response) =>
        {
            callback(response);
            _channelConnected = true;
            if(LoggingEnabled)
            {
                LogString("Channel Connected");
            }
        });
    }
    
    public void SendRawRTTPacket(string in_channelId, Dictionary<string, object> in_jsonData)
    {
        if (!_channelConnected)
        {
            LogString("Connect to channel before sending packet.");
            return;
        }
    
        Dictionary<string, object> json = new Dictionary<string, object>
        {
            ["service"] = "chat",
            ["operation"] = "SYS_POST_CHAT_MESSAGE"
        };
        Dictionary<string, object> jsonData = new Dictionary<string, object>
        {
            ["channelId"] = in_channelId,
            ["content"] = in_jsonData,
            ["recordInHistory"] = false
        };
        json["data"] = jsonData;
        string jsonDataString = JsonWriter.Serialize(json);
        QueueRequest(jsonDataString, null);
    }
    
    private bool SendRTTRequest(string in_message, bool in_bLogMessage = true)
    {
        bool bMessageSent = false;
        // early return
        if (_webSocket == null)
        {
            return bMessageSent;
        }

        try
        {
            if (in_bLogMessage)
            {
                if (LoggingEnabled)
                {
                    LogString("RTT SEND: " + in_message);
                }
            }

            // Web Socket 
            byte[] data = Encoding.ASCII.GetBytes(in_message);
            _webSocket.SendAsync(data);
        }
        catch (Exception socketException)
        {
            if (LoggingEnabled)
            {
                LogString("send exception: " + socketException);
            }
            AddQueueRTTResponse(new RTTCommandResponse("rttRegistration", "error", buildRTTRequestError(socketException.ToString())));
        }

        return bMessageSent;
    }

    /**
    * Terminate current session from server.
    * (New Session will automatically be created on next request)
    */
    public void Disconnect()
    {
        _state = State.Disconnected;
        SessionId = null;
        _packetId = 0;
    }

    public void OnAuthenticationCallback(string responseString)
    {
        Dictionary<string, object> response = null;
        try
        {
            response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
        }
        catch
        {
            return;
        }

        if (response != null)
        {
            ////check if its a failure
            if (!response.ContainsKey("reason_code"))
            {
                Dictionary<string, object> data = (Dictionary<string, object>)response["data"];
                if (data.ContainsKey("sessionId") && data.ContainsKey("heartbeatSeconds"))
                {
                    SessionId = (string)data["sessionId"];
                    if (data.ContainsKey("heartbeatSeconds"))
                    {
                        _heartbeatSeconds = (int)data["heartbeatSeconds"]; // get the heartbeat seconds from braincloud.
                    }

                    ResetHeartbeat();
                    _state = State.Authenticated;

                    for (int i = 0; i < _waitingForAuthRequestQueue.Count; i++)
                    {
                        S2SRequest req = (S2SRequest)_waitingForAuthRequestQueue[i];
                        Request(req.requestData, req.callback);
                    }
                }
            }
            //clear in case a reauthentication is needed.
            _waitingForAuthRequestQueue.Clear();
        }
    }

    public void OnHeartbeatCallback(string responseString)
    {
        Dictionary<string, object> response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
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
        Disconnect();
    }
    
    public void OnRTTConnectCallback(string responseString)
    {
        Dictionary<string, object> response = null;
        try
        {
            response = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
        }
        catch
        {
            return;
        }
        Dictionary<string, object> jsonData = (Dictionary<string, object>)response["data"];
        Array endpoints = (Array)jsonData["endpoints"];
        _rttHeaders = (Dictionary<string, object>)jsonData["auth"];
        Debug.Log("RTT Connect Callback");

        //   1st choice: websocket + ssl
        //   2nd: websocket
        _endpoint = getEndpointForType(endpoints, "ws", true);
        if (_endpoint == null)
        {
            _endpoint = getEndpointForType(endpoints, "ws", false);
        }
        if (_rttConnectionStatus == RTTConnectionStatus.DISCONNECTED)
        {
            bool sslEnabled = (bool)_endpoint["ssl"];
            string url = (sslEnabled ? "wss://" : "ws://") + _endpoint["host"] as string + ":" + (int)_endpoint["port"] + getUrlQueryParameters();
            setupWebSocket(url);
        }
    }
    
    private Dictionary<string, object> getEndpointForType(Array endpoints, string type, bool in_bWantSsl)
    {
        Dictionary<string, object> toReturn = null;
        Dictionary<string, object> tempToReturn = null;
        for (int i = 0; i < endpoints.Length; ++i)
        {
            tempToReturn = endpoints.GetValue(i) as Dictionary<string, object>;
            if (tempToReturn["protocol"] as string == type)
            {
                if (in_bWantSsl)
                {
                    if ((bool)tempToReturn["ssl"])
                    {
                        toReturn = tempToReturn;
                        break;
                    }
                }
                else
                {
                    toReturn = tempToReturn;
                    break;
                }
            }
        }

        return toReturn;
    }
    
    private string getUrlQueryParameters()
    {
        string sToReturn = "?";
        int count = 0;
        foreach (KeyValuePair<string, object> item in _rttHeaders)
        {
            if (count > 0) sToReturn += "&";
            sToReturn += item.Key + "=" + item.Value;
            ++count;
        }

        return sToReturn;
    }

    private string buildConnectionRequest()
    {
        Dictionary<string, object> system = new Dictionary<string, object>();
#if DOT_NET
        system["platform"] = "csharp";
#elif USE_WEB_REQUEST
        system["platform"] = "csharp-unity";
#endif
        system["protocol"] = "ws";

        Dictionary<string, object> jsonData = new Dictionary<string, object>();
        jsonData["appId"] = AppId;
        jsonData["sessionId"] = SessionId;
        jsonData["profileId"] = "s";
        jsonData["system"] = system;

        jsonData["auth"] = _rttHeaders;

        Dictionary<string, object> json = new Dictionary<string, object>();
        json["service"] = "rtt";
        json["operation"] = "CONNECT";
        json["data"] = jsonData;

        return JsonWriter.Serialize(json);
    }
    private void setupWebSocket(string in_url)
     {
         _webSocket = new BrainCloudWebSocket(in_url);
         _webSocket.OnClose += WebSocket_OnClose;
         _webSocket.OnOpen += Websocket_OnOpen;
         _webSocket.OnMessage += WebSocket_OnMessage;
         _webSocket.OnError += WebSocket_OnError;
     }
     
    private void WebSocket_OnClose(BrainCloudWebSocket sender, int code, string reason)
    {
        if (LoggingEnabled)
        {
            LogString("RTT: Connection closed: " + reason);
        }
        _webSocketStatus = WebsocketStatus.CLOSED;
        AddQueueRTTResponse(new RTTCommandResponse("rttRegistration", "disconnect", reason));
    }

    private void Websocket_OnOpen(BrainCloudWebSocket accepted)
    {
        if (LoggingEnabled)
        {
            LogString("RTT: WebSocket is open.");
        }
        _webSocketStatus = WebsocketStatus.OPEN;
        AddQueueRTTResponse(new RTTCommandResponse("rttRegistration", "connect", ""));
    }
    
    private void WebSocket_OnError(BrainCloudWebSocket sender, string message)
    {
        if (LoggingEnabled)
        {
            LogString("RTT Error: " + message);
        }
        _webSocketStatus = WebsocketStatus.ERROR;
        AddQueueRTTResponse(new RTTCommandResponse("rttRegistration", "error", buildRTTRequestError(message)));
    }
    private void WebSocket_OnMessage(BrainCloudWebSocket sender, byte[] data)
    {
        if (data.Length == 0) return;
        _webSocketStatus = WebsocketStatus.MESSAGE;
        string message = Encoding.UTF8.GetString(data);
        onRecv(message);
    }
    
    private void onRecv(string in_message)
    {
        if (LoggingEnabled)
        {
            LogString("RTT RECV: " + in_message);
        }

        Dictionary<string, object> response = (Dictionary<string, object>)JsonReader.Deserialize(in_message);

        string service = (string)response["service"];
        string operation = (string)response["operation"];

        Dictionary<string, object> data = null;
        if (response.ContainsKey("data"))
            data = (Dictionary<string, object>)response["data"];
        if (operation == "CONNECT")
        {
            int heartBeat = _heartbeatRTTTime.Milliseconds / 1000;
            try
            {
                heartBeat = (int)data["heartbeatSeconds"];
            }
            catch (Exception)
            {
                heartBeat = (int)data["wsHeartbeatSecs"];
            }
            if(LoggingEnabled)
            {
                LogString("RTT WebSocket Connection Established.");
            }
            _heartbeatRTTTime = TimeSpan.FromMilliseconds(heartBeat * 1000);
        }
        else if (operation == "DISCONNECT")
        {
            _disconnectedWithReason = true;
            _disconnectJson["reason_code"] = (int)data["reasonCode"];
            _disconnectJson["reason"] = (string)data["reason"];
            _disconnectJson["severity"] = "ERROR";
        }

        if (data != null)
        {
            if (data.ContainsKey("cxId")) RTTConnectionID = (string)data["cxId"];
            if (data.ContainsKey("evs")) RTTEventServer = (string)data["evs"];
        }

        if (operation != "HEARTBEAT")
        {
            AddQueueRTTResponse(new RTTCommandResponse(service.ToLower(), operation.ToLower(), in_message));
        }
    }
    
    private string buildRTTRequestError(string in_statusMessage)
    {
        Dictionary<string, object> json = new Dictionary<string, object>();
        json["status"] = 403;
        json["reason_code"] = 80300;
        json["status_message"] = in_statusMessage;
        json["severity"] = "ERROR";

        return JsonWriter.Serialize(json);
    }
    
    private void AddQueueRTTResponse(RTTCommandResponse in_command)
    {
        lock (_queuedRTTCommands)
        {
            _queuedRTTCommands.Add(in_command);
        }
    }
}
