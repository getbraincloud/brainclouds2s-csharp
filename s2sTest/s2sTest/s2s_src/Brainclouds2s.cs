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
//using UnityEngine.Experimental.Networking;
//using UnityEngine.Networking;
//using UnityEngine;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using BrainCloud.JsonFx.Json;

public interface IS2SCallback
{
    void onAuthenticationCallback(Dictionary<string, object> responseData);
    void onHeartbeatCallback(Dictionary<string, object> responseData);
}

internal sealed class BrainClouds2s : IS2SCallback
{
    private static int NO_PACKET_EXPECTED = -1;
    private static int SERVER_SESSION_EXPIRED = 40365;
    private static string DEFAULT_S2S_URL = "https://internal.braincloudservers.com/s2sdispatcher";
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
    private static Mutex _lock = new Mutex();
    private ArrayList _requestQueue = new ArrayList();

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
        _heartbeatTimer = TimeSpan.FromSeconds(_heartbeatSeconds);
    }

    /**
    * Send an S2S request.
    *
    * @param json S2S operation to be sent as a string
    * @param callback Callback function
    */
    public void request(string jsonRequestData)
    {
        if (!Authenticated)
        {
            Console.WriteLine("\nNot Authenticated");
            authenticate();
        }
        formRequest(jsonRequestData);
    }

    private void formRequest(string jsonRequestData)
    {
        Console.WriteLine("forming new request with: " + jsonRequestData);
        //create new request
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ServerURL);
        //create data packet
        string dataPacket = createPacket(jsonRequestData);
        Console.WriteLine("Created Packet: " + dataPacket);

        //customize request
        request.Method = "POST";
        request.ContentType = "application/json; charset=utf-8";

        _requestQueue.Add(new KeyValuePair<HttpWebRequest, string>(request, dataPacket));         //store request and associated dataPacket
        Console.WriteLine("Request Queue size: " + _requestQueue.Count);

        _packetId++;
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
        packetDataString += ",\"messages\":[" + packetData + "]}";
        return packetDataString;
    }

    private void sendData(HttpWebRequest request, string dataPacket)
    {
        Console.WriteLine("Attempting to send data...");
        Console.WriteLine("Request Queue size: " + _requestQueue.Count);
        Console.WriteLine("Current data packet: " + dataPacket);
        byte[] byteArray = Encoding.UTF8.GetBytes(dataPacket);      //convert data packet to byte[]
        Stream requestStream = request.GetRequestStream();          //gets a stream to send dataPacket for request
        requestStream.Write(byteArray, 0, byteArray.Length);        //writes dataPacket to stream and sends data with request. 
        request.ContentLength = byteArray.Length;

        //StreamReader readStream = new StreamReader(requestStream, Encoding.UTF8);
        //Console.WriteLine("stream: " + readStream.ReadToEnd());
    }

    private void resetHeartbeat()
    {
        _lastHeartbeat = DateTime.Now;
    }

    //private Dictionary<string, object> generateError(int statusCode, int reasonCode, string statusMessage)
    //{
    //    Dictionary<string, object> jsonError = new Dictionary<string, object>();
    //    jsonError.Add("status", statusCode);
    //    jsonError.Add("reason_code", reasonCode);
    //    jsonError.Add("serverity", "ERROR");
    //    jsonError.Add("status_message", statusMessage);
    //    return jsonError;
    //}

    public void authenticate()
    {
        Console.WriteLine("Creating Authentication request...");
        string jsonAuthString = "{\"service\":\"authenticationV2\",\"operation\":\"AUTHENTICATE\",\"data\":{\"appId\":\"" + AppId + "\",\"serverName\":\"" + ServerName + "\",\"serverSecret\":\"" + ServerSecret + "\"}}";
        _packetId = 0;
        formRequest(jsonAuthString);
    }

    public void sendHeartbeat()
    {
        Console.WriteLine("Sending Heartbeat");
        if (SessionId != null)
        {
            string jsonHeartbeatString = "{\"service\":\"heartbeat\",\"operation\":\"HEARTBEAT\"}";
            formRequest(jsonHeartbeatString);
        }
    }

    private string readResponseBody(HttpWebResponse response)
    {
        Console.WriteLine("Reading Response Body...");
        // Get the stream associated with the response.
        Stream receiveStream = response.GetResponseStream();
        // Pipes the stream to a higher level stream reader with the required encoding format. 
        StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);
        return readStream.ReadToEnd();
    }

    private void LogString(string s)
    {
        if (LoggingEnabled)
        {
            Console.WriteLine("#BCC " + s);
        }
    }

    public void runCallbacks()
    {
        if (_requestQueue.Count != 0)
        {
            //make first request in queue the active request
            KeyValuePair<HttpWebRequest, string> requestPair = (KeyValuePair<HttpWebRequest, string>)_requestQueue[0];
            HttpWebRequest activeRequest = requestPair.Key;

            //send the request data
            sendData(activeRequest, requestPair.Value);

            //Send request and wait for server response
            //Send request and wait for server response
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)activeRequest.GetResponse();
            }
            catch (Exception e)
            {
                Console.WriteLine("S2S Failed: " + e.ToString());
                LogString("S2S Failed: " + e.ToString());
                activeRequest.Abort();
                _requestQueue.RemoveAt(0);
                return;
            }

            Console.WriteLine("good!");

            if (response != null)
            {
                Console.WriteLine("We have a response!");
                //get the response body
                string responseString = readResponseBody(response);
                Console.WriteLine(responseString);

                Dictionary<string, object> responseBody = (Dictionary<string, object>)JsonReader.Deserialize(responseString);
                Console.WriteLine(responseBody);
                if (responseBody.ContainsKey("messageResponses"))
                {
                    //extract the map array
                    Dictionary<string, object>[] messageArray = (Dictionary<string, object>[])responseBody["messageResponses"];
                    //extract the map from the map array
                    Dictionary<string, object> messageResponses = (Dictionary<string, object>)messageArray.GetValue(0);
                    if ((int)messageResponses["status"] == 200)
                    {
                        Console.WriteLine("status is 200");
                        if (LoggingEnabled)
                        {
                            LogString("S2S Response: " + responseString);
                        }

                        //we've authenticated
                        if (SessionId == null)
                        {
                            onAuthenticationCallback((Dictionary<string, object>)messageResponses["data"]);
                        }

                        //remove the request
                        _requestQueue.RemoveAt(0);
                    }
                    else
                    {
                        //check if its a session expiry
                        //if (responseBody.ContainsKey("reason_code"))
                        //{
                        //    if (responseBody.TryGetValue("reason_code", out value))
                        //    {
                        //        if ((int)value == SERVER_SESSION_EXPIRED)
                        //        {
                        //            LogString("S2S session expired");
                        //            activeRequest.Abort();
                        //            disconnect();
                        //            return;
                        //        }
                        //    }
                        //}

                        LogString("S2S Failed: " + responseString);
                        activeRequest.Abort();

                        //callback

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
                sendHeartbeat();
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

    public void onAuthenticationCallback(Dictionary<string, object> responseData)
    {
        if (responseData != null)
        {
            if (responseData.ContainsKey("sessionId") && responseData.ContainsKey("heartbeatSeconds"))
            {
                SessionId = (string)responseData["sessionId"];
                _heartbeatSeconds = (int)responseData["heartbeatSeconds"];
                resetHeartbeat();
                Authenticated = true; //Authenticated!
                Console.WriteLine("AUTHENITCATED!!!!!!!!!");
                Console.WriteLine("SessionId = " + SessionId);
            }
        }
    }

    public void onHeartbeatCallback(Dictionary<string, object> responseData)
    {
        if (responseData != null)
        {
            if (responseData.ContainsKey("status"))
            {
                if ((int)responseData["status"] == 200)
                {
                    return;
                }
            }
        }
        disconnect();
    }

}
