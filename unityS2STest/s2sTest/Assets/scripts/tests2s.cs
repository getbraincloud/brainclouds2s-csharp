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
        context.init("20001", "TestServer", "2ddf8355-c516-48dd-a6b0-e35bd75fac80");
        context.LoggingEnabled = true;

        //SEND AS DICTIONARY
        Dictionary<string, object> request = new Dictionary<string, object>();
        request.Add("service", "log");
        request.Add("operation", "LOG_INFO");
        Dictionary<string, object> data = new Dictionary<string, object>();
        data.Add("errorMessage", "test");
        data.Add("context", "test");
        request.Add("data", data);
        context.request(request, null);
    }

    // Update is called once per frame
    void Update()
    {
        context.runCallbacks();
    }
}
