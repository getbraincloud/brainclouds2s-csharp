using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tests.PlayMode
{
    public class TestUser : MonoBehaviour
    {
        public string Id = "";
        public string Password = "";
        public string ProfileId = "";
        public string Email = "";

        BrainCloudS2S _bc;
        public bool IsRunning;
        private TestContainer _tc;
        
        public TestUser(BrainCloudS2S bc, string idPrefix, int suffix)
        {
            _bc = bc;
            Id = idPrefix + suffix;
            Password = Id;
            Email = Id + "@bctestuser.com";
        }

        public IEnumerator SetUp(BrainCloudS2S bc, string idPrefix, int suffix, TestContainer testContainer)
        {
            _bc = bc;
            _tc = testContainer;
            Id = idPrefix + suffix;
            Password = Id;
            Email = Id + "@bctestuser.com";
            IsRunning = true;
            
            _bc.Authenticate(OnAuthenticateCallback);

            yield return StartCoroutine(_tc.Run());
            
            
        }

        public void OnAuthenticateCallback(string response)
        {
            //ProfileId = _bc.ProfileId;
            Debug.Log(response);
            if(_tc.m_response == null ||
               _tc.m_response.Count == 0)
            {
                Debug.Log("Got no response from Authentication");
            }
            IsRunning = false;
        }
    }
}

