using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using Tests.PlayMode;
using UnityEngine;
using UnityEngine.TestTools;

public class TestSessionLessCalls : TestFixtureBase
{
    [UnityTest]
    public IEnumerator TestEmptyAuthAndRequest()
    {
        //Test 4 - Test Empty Auth and Request
        _tc.context.Authenticate();
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onEmptyAuthAndRequestCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Couldn't Send the request", _tc.successCount == 1);
    }
    
    void onEmptyAuthAndRequestCallback(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
        _tc.context.Disconnect();
    }

    [UnityTest]
    public IEnumerator TestEmptyAuthAndMultipleRequests()
    {
        //Test 5 - Test Queue
        //three different calls, that should come back in exactly the same order.
        _tc.context.Authenticate();
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("One of the requests failed", _tc.successCount == 3);
    }
    
    void onTestQueueCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && _tc.successCount == 0) //first call is time call
            {
                Debug.Log("Server Time Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("parentCurrencies") && _tc.successCount == 1)//second call is parent currency
            {
                Debug.Log("Parent Currencies Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("peerCurrencies") && _tc.successCount == 2) //third call is peer currency
            {
                Debug.Log("Peer Currencies Request Call Passed");
                _tc.successCount++;
            }
        }

        if (_tc.successCount == 3)
        {
            _tc.m_done = true;
            _tc.context.Disconnect();
        }
    }

    [UnityTest]
    public IEnumerator TestEmptyAuthAndQueueFailingRequests()
    {
        //three different calls, that should come back in exactly the same order.
        _tc.context.Authenticate();
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Something went wrong, check the logs for more clues.", _tc.successCount == 3);
    }
    
    [UnityTest]
    public IEnumerator TestQueueFailingRequestsThenEmptyAuth()
    {
        //three different calls, that should come back in exactly the same order.
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithFailCallback);
        _tc.context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        _tc.context.Authenticate();
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("Something went wrong, check the logs for more clues.", _tc.successCount == 3);
    }
    
    void onTestQueueWithFailCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && _tc.successCount == 0) //first call is time call
            {
                Debug.Log("Server Time Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("parentCurrencies") && _tc.successCount == 1)//second call is parent currency
            {
                Debug.Log("Parent Currencies Request Call Passed");
                _tc.successCount++;
            }
            if(data.ContainsKey("peerCurrencies") && _tc.successCount == 2) //third call is peer currency
            {
                Debug.Log("Peer Currencies Request Call Passed");
                _tc.successCount++;
            }
        }

        if (_tc.successCount == 3)
        {
            _tc.m_done = true;
            _tc.context.Disconnect();
        }
    }
}
