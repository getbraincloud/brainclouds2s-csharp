using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class tests2s : MonoBehaviour
{
    BrainCloudS2S context;
    // Start is called before the first frame update
    void Start()
    {
        context = new BrainCloudS2S();
        context.Init("", "TestServer", "", "https://internal.braincloudservers.com/s2sdispatcher");
        //context.Init("", "", "");
        context.LoggingEnabled = true;

        //SEND AS DICTIONARY
        context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", testCallback);
        context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", testCallback);
    }

    // Update is called once per frame
    void Update()
    {
        context.RunCallbacks();
    }

    void testCallback(string responseString)
    {
        Debug.Log("Callback Success!");
    }
}
