using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BrainCloud.JsonFx.Json;

public class tests2s : MonoBehaviour
{
    int successCounter = 0;
    BrainCloudS2S context = new BrainCloudS2S();
    long lastServerTime = 0;

    void Start()
    {
        context.Init("20001", "TestServer", "2ddf8355-c516-48dd-a6b0-e35bd75fac80", false, "https://internal.braincloudservers.com/s2sdispatcher");
        context.LoggingEnabled = true;

        Debug.Log("\n-----Test1 Authentication START-----");
        context.Authenticate(onTestAuthenticationCallback);
    }

    // Update is called once per frame
    void Update()
    {
        context.RunCallbacks();
    }

    void OnRunTestsButton()
    {
        //Begin Tests
        //Test 1 - Test Auth
        Debug.Log("\n-----Test1 Authentication START-----");
        context.Authenticate(onTestAuthenticationCallback);
    }

    void onTestAuthenticationCallback(string response)
    {
        successCounter++;
        if (successCounter == 1)
        {
            Debug.Log("\n-----Test1 Authentication PASS-----");
            context.Disconnect();
            successCounter = 0;

            //Test 2 - Test Multiple Auth
            Debug.Log("\n-----Test2 Multi Auth START-----");
            context.Authenticate(onTestMultiAuthCallback);
            context.Authenticate(onTestMultiAuthCallback);
        }
    }

    void onTestMultiAuthCallback(string response)
    {
        successCounter++;
        if (successCounter == 2)
        {
            Debug.Log("\n-----Test2 Multi Auth PASS-----");
            context.Disconnect();
            successCounter = 0;

            //Test 3 - Test Auth and Request
            Debug.Log("\n-----Test3 Auth and Request START-----");
            context.Authenticate(onAuthAndRequestCallback);
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onAuthAndRequestCallback);
        }
    }

    void onAuthAndRequestCallback(string response)
    {
        successCounter++;
        if (successCounter == 2)
        {
            Debug.Log("\n-----Test3 Auth and Request PASS-----");
            context.Disconnect();
            successCounter = 0;

            //Test 4 - Test Empty Auth and Request
            Debug.Log("\n-----Test4 Empty Auth and Request START-----");
            context.Authenticate();
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onEmptyAuthAndRequestCallback);
        }
    }

    void onEmptyAuthAndRequestCallback(string response)
    {
        successCounter++;
        if (successCounter == 1)
        {
            Debug.Log("\n-----Test4 Auth and Request PASS-----");
            context.Disconnect();
            successCounter = 0;

            //Test 5 - Test Queue
            Debug.Log("\n-----Test5 Queue START-----");
            context.Authenticate();
            //three different calls, that should come back in exactly the same order.
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
            context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueCallback);
            context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueCallback);
        }
    }

    void onTestQueueCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && successCounter == 0) //first call is time call
            {
                Debug.Log("1111111111111111");
                successCounter++;
            }
            if(data.ContainsKey("parentCurrencies") && successCounter == 1)//second call is parent currency
            {
                Debug.Log("22222222222222");
                successCounter++;
            }
            if(data.ContainsKey("peerCurrencies") && successCounter == 2) //third call is peer currency
            {
                Debug.Log("3333333333333333");
                successCounter++;
            }
        }

        if (successCounter == 3)
        {
            Debug.Log("\n-----Test5 Queue PASS-----");
            context.Disconnect();
            lastServerTime = 0;
            successCounter = 0;

            //Test 6 - Test Queue with fail
            Debug.Log("\n-----Test6 Queue With Fail START-----");
            context.Authenticate();
            //three different calls, that should come back in exactly the same order.
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
            context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
            context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithFailCallback);
            context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueWithFailCallback);
        }
    }

    void onTestQueueWithFailCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && successCounter == 0) //first call is time call
            {
                Debug.Log("1111111111111111");
                successCounter++;
            }
            if(data.ContainsKey("parentCurrencies") && successCounter == 1)//second call is parent currency
            {
                Debug.Log("22222222222222");
                successCounter++;
            }
            if(data.ContainsKey("peerCurrencies") && successCounter == 2) //third call is peer currency
            {
                Debug.Log("3333333333333333");
                successCounter++;
            }
        }

        if (successCounter == 3)
        {
            Debug.Log("\n-----Test6 Queue With Fail PASS-----");
            context.Disconnect();
            lastServerTime = 0;
            successCounter = 0;

            //Test 6 - Test Queue with fail
            //expect 4 pass
            Debug.Log("\n-----Test7 Queue With Random Fail START-----");
            context.Authenticate();
            //three different calls, that should come back in exactly the same order.
            context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
            context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback); //expect pass
            context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
            context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
            context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PARENT_CURRENCY_TYPES\"}", onTestQueueWithRandomFailCallback); //expect pass
            context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
            context.Request("{\"service\":\"virtualCurrency\",\"operation\":\"SYS_GET_PEER_CURRENCY_TYPES\"}", onTestQueueWithRandomFailCallback); //expect pass

        }
    }

    void onTestQueueWithRandomFailCallback(string response)
    {
        var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
        if(responseData.ContainsKey("data"))
        {
            Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
            if(data.ContainsKey("server_time") && successCounter == 0) //first call is time call
            {
                Debug.Log("1111111111111111");
                successCounter++;
            }
            if(data.ContainsKey("parentCurrencies") && successCounter == 1)//second call is parent currency
            {
                Debug.Log("22222222222222");
                successCounter++;
            }
            if(data.ContainsKey("peerCurrencies") && successCounter == 2) //third call is peer currency
            {
                Debug.Log("3333333333333333");
                successCounter++;
            }
        }

        if (successCounter == 3)
        {
            Debug.Log("\n-----Test7 Queue With Random Fail PASS-----");
            context.Disconnect();
            lastServerTime = 0;
            successCounter = 0;
        }
    }
}
