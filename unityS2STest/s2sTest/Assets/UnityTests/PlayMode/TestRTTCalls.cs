using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Tests.PlayMode;
using UnityEngine;
using UnityEngine.TestTools;

public class TestRTTCalls: TestFixtureBase
{
    
    [UnityTest]
    public IEnumerator TestGetRTTConnectInfo()
    {
        _tc.context.Authenticate(onTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.EnableRTT(onRTTConnectCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Failed to get RTT Info", _tc.successCount == 2);
    }

    void onTestAuthenticationCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
    }
    
    void onRTTConnectCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
    }
}
