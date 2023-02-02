using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using Tests.PlayMode;
using UnityEngine;
using UnityEngine.TestTools;

public class TestSessionCalls : TestFixtureBase
{
    public const int INVALID_OPERATION = 40333;
    
    [UnityTest]
    public IEnumerator TestAuthentication()
    {
        _tc.context.Authenticate(onTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Failed to Authenticate", _tc.successCount == 1);
    }
    
    void onTestAuthenticationCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
        if (_tc.successCount == 1)
        {
            _tc.context.Disconnect();
        }
    }
    
    [UnityTest]
    public IEnumerator TestMultiAuthenticate()
    {
        _tc.context.Authenticate(onTestMultiAuthCallback);
        _tc.context.Authenticate(onTestMultiAuthCallback);

        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Failed to Multi-Authentication", _tc.successCount == 2);
    }
    
    void onTestMultiAuthCallback(string response)
    {
        _tc.successCount++;
        if (_tc.successCount == 2)
        {
            _tc.m_done = true;
            _tc.context.Disconnect();
        }
    }
    
    [UnityTest]
    public IEnumerator TestAuthenticateAndRequest()
    {
        //Test 3 - Test Auth and Request
        _tc.context.Authenticate(onAuthAndRequestCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onAuthAndRequestCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Failed Authenticating or Requesting", _tc.successCount == 2);
    }
    
    void onAuthAndRequestCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
        if (_tc.successCount == 2)
        {
            _tc.context.Disconnect();
        }
    }
    
    [UnityTest]
    public IEnumerator AuthAndMultipleRequests()
    {
        //Test 5 - Test Queue
        _tc.context.Authenticate(onTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        //three different calls, that should come back in exactly the same order.
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Auth or one of the multiple requests failed..", _tc.successCount == 4);
    }
    
    void onTestQueueCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && _tc.successCount == 1) //first call is time call
            {
                Debug.Log("Server Time Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("parentCurrencies") && _tc.successCount == 2)//second call is parent currency
            {
                Debug.Log("Parent Currencies Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("peerCurrencies") && _tc.successCount == 3) //third call is peer currency
            {
                Debug.Log("Peer Currencies Request Call Passed");
                _tc.successCount++;
            }
        }

        if (_tc.successCount == 4)
        {
            _tc.context.Disconnect();
            _tc.m_done = true;
        }
    }

    [UnityTest]
    public IEnumerator AuthAndMultipleFailingRequests()
    {
        //Test 6 - Test Queue with fail
        _tc.context.Authenticate(onTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        //three different calls, that should come back in exactly the same order.
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithFailCallback);    //This should fail..
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Failed to Auth or something unexpected happened", _tc.successCount == 4 && _tc.failCount == 1);
    }
    
    void onTestQueueWithFailCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && _tc.successCount == 1) //first call is time call
            {
                Debug.Log("Server Time Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("parentCurrencies") && _tc.successCount == 2)//second call is parent currency
            {
                Debug.Log("Parent Currencies Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("peerCurrencies") && _tc.successCount == 3) //third call is peer currency
            {
                Debug.Log("Peer Currencies Request Call Passed");
                _tc.successCount++;
            }
        }
        else if (responseData.ContainsKey("reason_code"))
        {
            int reasonCode = (int) responseData["reason_code"];
            if (reasonCode == INVALID_OPERATION)
            {
                Debug.Log("Invalid Operation Response Received");
                _tc.failCount++;
            }
        }

        if (_tc.successCount == 4 && _tc.failCount == 1)
        {
            _tc.m_done = true;
            _tc.context.Disconnect();
        }
    }

    [UnityTest]
    public IEnumerator TestAuthAndMixFailingRequests()
    {
        //Test 7 - Test Queue with a mix of passes and fails
        _tc.context.Authenticate(onTestAuthenticationCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        
        //three different calls, that should come back in exactly the same order.
        //3 requests should pass while 4 should fail
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);//Fail
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback); //expect pass
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);//Fail
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);//Fail
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueWithRandomFailCallback); //expect pass
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);//Fail
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueWithRandomFailCallback); //expect pass
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Something went wrong...", _tc.successCount == 4 && _tc.failCount == 4);
    }
    
    void onTestQueueWithRandomFailCallback(string response)
    {
        Debug.Log($"Response::: {response}");
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            if(responseData.ContainsKey("data"))
            {
                Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                if(data.ContainsKey("server_time") && _tc.successCount == 1) //first call is time call
                {
                    Debug.Log("Server Time Request Call Passed");
                    _tc.successCount++;
                }
                if(data.ContainsKey("parentCurrencies") && _tc.successCount == 2)//second call is parent currency
                {
                    Debug.Log("Parent Currencies Request Call Passed");
                    _tc.successCount++;
                }
                if(data.ContainsKey("peerCurrencies") && _tc.successCount == 3) //third call is peer currency
                {
                    Debug.Log("Peer Currencies Request Call Passed");
                    _tc.successCount++;
                }
            }
        }
        else if (responseData.ContainsKey("reason_code"))
        {
            int reasonCode = (int) responseData["reason_code"];
            if (reasonCode == INVALID_OPERATION)
            {
                Debug.Log("Invalid Operation Response Received");
                _tc.failCount++;
            }
        }

        if (_tc.successCount == 4 && _tc.failCount == 4)
        {
            _tc.m_done = true;
            _tc.context.Disconnect();
        }
    }
}
