using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Timeline.TimelinePlaybackControls;

public class RTT_Test : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField lobbyIdInputField;
    [SerializeField]
    private Button startTestButton;

    private BrainCloudS2S _s2s = new BrainCloudS2S();
    private string pathToIds;

    public string ServerUrl { get; private set; }
    public string AppId { get; private set; }
    public string Secret { get; private set; }
    public string Version { get; private set; }
    public string ChildAppId { get; private set; }
    public string ChildSecret { get; private set; }
    public string ParentLevel { get; private set; }
    public string PeerName { get; private set; }
    public string SupportsCompression { get; private set; }
    public string ServerName { get; private set; }
    public string ServerSecret { get; private set; }
    public string S2S_URL { get; private set; }

    private bool rttConnected = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        startTestButton.interactable = false;
        startTestButton.onClick.AddListener(OnStartTestClicked);
        //init s2s
        LoadIds();

        Dictionary<string, string> secretMap = new Dictionary<string, string>();
        secretMap.Add(AppId, Secret);
        secretMap.Add(ChildAppId, ChildSecret);

        _s2s = new BrainCloudS2S();
        _s2s.Init(AppId, ServerName, ServerSecret, false, S2S_URL);
        _s2s.LoggingEnabled = true;

        _s2s.Authenticate(OnAuthenticateCallback);
    }

    private void OnAuthenticateCallback(string responseString)
    {
        Debug.Log("Auth callback: " + responseString);
        _s2s.EnableRTT(OnRTTConnectCallback);
    }

    private void OnStartTestClicked()
    {
        //get lobby id input
        if (string.IsNullOrEmpty(lobbyIdInputField.text)) return;
        _s2s.RegisterRTTRawCallback(OnRTTRawCallback);
        JoinLobbyChannel(lobbyIdInputField.text);
    }

    void OnRTTRawCallback(string jsonResponse)
    {
        Debug.Log("Got a raw callback: " + jsonResponse);

    }

    void OnRTTConnectCallback(string jsonResponse)
    {
        //now RTT is enabled register callback
        Debug.Log("RTT Connected: " + jsonResponse);
        startTestButton.interactable = true;
    }

    private void JoinLobbyChannel(string lobbyId)
    {
        string channelId = AppId + ":sy:_lobbystatus_" + lobbyId;
        _s2s.ConnectToChannel(channelId, OnChannelJoined);
    }

    private void OnChannelJoined(string responseString)
    {
        Debug.Log("Channel joined! " + responseString);
    }

    // Update is called once per frame
    void Update()
    {
        if (_s2s != null && _s2s.IsInitialized)
        {
            _s2s.RunCallbacks();

            
        }
    }

    private void LoadIds()
    {
        pathToIds = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "\\ids.txt";
        using (var reader = new StreamReader(pathToIds))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("serverUrl="))
                {
                    ServerUrl = line.Substring(("serverUrl=").Length);
                    ServerUrl.Trim();
                }
                else if (line.StartsWith("appId="))
                {
                    AppId = line.Substring(("appId=").Length);
                    AppId.Trim();
                }
                else if (line.StartsWith("secret="))
                {
                    Secret = line.Substring(("secret=").Length);
                    Secret.Trim();
                }
                else if (line.StartsWith("version="))
                {
                    Version = line.Substring(("version=").Length);
                    Version.Trim();
                }
                else if (line.StartsWith("childAppId="))
                {
                    ChildAppId = line.Substring(("childAppId=").Length);
                    ChildAppId.Trim();
                }
                else if (line.StartsWith("childSecret="))
                {
                    ChildSecret = line.Substring(("childSecret=").Length);
                    ChildSecret.Trim();
                }
                else if (line.StartsWith("parentLevelName="))
                {
                    ParentLevel = line.Substring(("parentLevelName=").Length);
                    ParentLevel.Trim();
                }
                else if (line.StartsWith("peerName="))
                {
                    PeerName = line.Substring(("peerName=").Length);
                    PeerName.Trim();
                }
                else if (line.StartsWith("supportsCompression="))
                {
                    SupportsCompression = line.Substring(("supportsCompression=").Length);
                    SupportsCompression.Trim();
                }
                else if (line.StartsWith("serverName="))
                {
                    ServerName = line.Substring(("serverName=").Length);
                    ServerName.Trim();
                }
                else if (line.StartsWith("serverSecret="))
                {
                    ServerSecret = line.Substring(("serverSecret=").Length);
                    ServerSecret.Trim();
                }
                else if (line.StartsWith("s2sUrl="))
                {
                    S2S_URL = line.Substring(("s2sUrl=").Length);
                    S2S_URL.Trim();
                }
            }
        }
    }
}
