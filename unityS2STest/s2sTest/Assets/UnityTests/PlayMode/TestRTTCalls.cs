using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using NUnit.Framework;
using Tests.PlayMode;
using UnityEngine;
using UnityEngine.TestTools;

public class TestRTTCalls: TestFixtureBase
{
    
    [UnityTest]
    public IEnumerator TestGetRTTConnectInfo()
    {
        _tc.context.Authenticate(OnTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        
        _tc.context.EnableRTT(OnRTTConnectCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        
        _tc.context.RegisterRTTRawCallback(OnRTTRawCallback);
        string channelId = AppId + ":sy:mysyschannel";
        _tc.context.ConnectToChannel(channelId, OnTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        
        Dictionary<string, object> jsonInfo = new Dictionary<string, object>();
        var mockPlayerName = "braincloudTester";
        jsonInfo["playerName"] = mockPlayerName;
        _tc.context.SendRawRTTPacket(jsonInfo);
        yield return _tc.StartCoroutine(_tc.Run());
        
        _tc.context.DisableRTT();
        _tc.context.DeregisterRTTRawCallback();
        _tc.context.RunCallbacks();
        yield return new WaitForSeconds(3);
        if(!_tc.context.IsRTTEnabled())
        {
            _tc.successCount++;
        }
        
        LogResults("Failed to get RTT Info", _tc.successCount == 5);
    }
    
    void OnRTTRawCallback(string jsonResponse)
    {
        Debug.Log("Got a raw callback: " + jsonResponse);
        _tc.successCount++;
        _tc.m_done = true;
    }

    void OnTestAuthenticationCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
    }
    
    void OnRTTConnectCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
    }
}
