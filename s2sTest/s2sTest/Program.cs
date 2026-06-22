// [dsl] This needs to be rewritten.
// I had to hack it so it fails on the first fail. and also exit on success so it
// doesn't get stuck in infinite loop

using System;
using System.Collections;
using BrainCloud.JsonFx.Json;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace s2sTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Load ids.txt
            string s2sUrl = "";
            string appId = "";
            string serverName = "";
            string serverSecret = "";
            using (var reader = new StreamReader("ids.txt"))
            {
                Console.WriteLine("Found ids.txt");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("s2sUrl="))
                    {
                        s2sUrl = line.Substring(("s2sUrl=").Length);
                        s2sUrl.Trim();
                    }
                    else if (line.StartsWith("appId="))
                    {
                        appId = line.Substring(("appId=").Length);
                        appId.Trim();
                    }
                    else if (line.StartsWith("serverSecret="))
                    {
                        serverSecret = line.Substring(("serverSecret=").Length);
                        serverSecret.Trim();
                    }
                    else if (line.StartsWith("serverName="))
                    {
                        serverName = line.Substring(("serverName=").Length);
                        serverName.Trim();
                    }
                }
            }

            string currentTestName = "";
            int successCounter = 0;
            Int64 lastServerTime = 0;

            // Shared state for GlobalFileV3 tests
            string gfv3FileId = "";
            int gfv3FileVersion = 1;
            string gfv3FolderTreeId = "";

            BrainCloudS2S context = new BrainCloudS2S();
            context.Init(appId, serverName, serverSecret, false, s2sUrl);
            context.LoggingEnabled = true;
            Stopwatch stopwatch = new Stopwatch();


            void startTest(string testName)
            {
                currentTestName = testName;
                Console.WriteLine("\n-----" + currentTestName + " START-----");
                stopwatch.Restart();
            }


            //Begin Tests
            //Test 1 - Test Auth
            stopwatch.Start();
            startTest("TestAuthentication");
            context.Authenticate(onTestAuthenticationCallback);


            // Infinite loop, check for timeouts
            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > 20000) // 20 sec. If a call takes more than that, something is clearly wrong and should fail
                {
                    testFail("Timedout");
                }

                context.RunCallbacks();
                Thread.Sleep(16); // 60 fps
            }


            void testFail(string message)
            {
                Console.WriteLine("\nTESTS FAIL - " + currentTestName + " - " + message);
                Environment.Exit(1);
            }

            void checkIfFail(string response)
            {
                try
                {
                    var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    if ((int)responseData["status"] != 200)
                    {
                        testFail("status != 200");
                    }
                }
                catch (Exception e)
                {
                    testFail(e.Message);
                }
            }

            void checkExpectFail(string response)
            {
                try
                {
                    var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    if ((int)responseData["status"] == 200)
                    {
                        testFail("status == 200. Expected fail");
                    }
                }
                catch (Exception e)
                {
                    testFail(e.Message);
                }
            }

            // -----------------------------------------------------------------------
            // Tests 1–7: Core S2S dispatcher tests (unchanged)
            // -----------------------------------------------------------------------

            void onTestAuthenticationCallback(string response)
            {
                checkIfFail(response);

                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();
                successCounter = 0;

                //Test 2 - Test Multiple Auth
                startTest("TestMultiAuth");
                context.Authenticate(onTestMultiAuthCallback);
                context.Authenticate(onTestMultiAuthCallback);
            }

            void onTestMultiAuthCallback(string response)
            {
                checkIfFail(response);

                successCounter++;
                if (successCounter == 2)
                {
                    Console.WriteLine("\n----- PASS -----");
                    //context.Disconnect();
                    successCounter = 0;

                    //Test 3 - Test Auth and Request
                    startTest("AuthAndRequest");
                    context.Authenticate(onAuthAndRequestCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onAuthAndRequestCallback);
                }
            }

            void onAuthAndRequestCallback(string response)
            {
                checkIfFail(response);

                successCounter++;
                if (successCounter == 2)
                {
                    Console.WriteLine("\n----- PASS -----");
                    //context.Disconnect();
                    successCounter = 0;

                    //Test 4 - Test Empty Auth and Request
                    startTest("EmptyAuthAndRequest");
                    //context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onEmptyAuthAndRequestCallback);
                }
            }

            void onEmptyAuthAndRequestCallback(string response)
            {
                checkIfFail(response);

                successCounter++;
                if (successCounter == 1)
                {
                    Console.WriteLine("\n----- PASS -----");
                    //context.Disconnect();
                    successCounter = 0;

                    //Test 5 - Test Queue
                    startTest("TestQueue");
                    //context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueCallback);
                }
            }

            void onTestQueueCallback(string response)
            {
                checkIfFail(response);

                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                Int64 serverTime = (Int64)data["server_time"];

                if (serverTime > lastServerTime) //check if serverTime is greater than stored server time - this way we know later requests are coming back last in the queue
                {
                    successCounter++;
                    lastServerTime = serverTime;
                }

                if (successCounter == 3)
                {
                    Console.WriteLine("\n----- PASS -----");
                    //context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    //Test 6 - Test Queue with fail
                    startTest("TestQueueWithFail");
                    //context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithFailCallback);
                }
            }

            void onTestQueueWithFailCallback(string response)
            {
                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                if (responseData.ContainsKey("data"))
                {
                    checkIfFail(response);

                    Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                    Int64 serverTime = (Int64)data["server_time"];

                    if (serverTime > lastServerTime) //check if serverTime is greater than stored server time - this way we know later requests are coming back last in the queue
                    {
                        successCounter++;
                        lastServerTime = serverTime;
                    }
                }

                if (successCounter == 2)
                {
                    Console.WriteLine("\n----- PASS -----");
                    //context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    //Test 7 - Test Queue with random fail (expect 4 passes)
                    startTest("TestQueueWithRandomFail");
                    //context.Authenticate();
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"REAAAD\"}", onTestQueueWithRandomFailCallback);
                    context.Request("{\"service\":\"time\",\"operation\":\"READ\"}", onTestQueueWithRandomFailCallback);
                }
            }

            void onTestQueueWithRandomFailCallback(string response)
            {
                var responseData = JsonReader.Deserialize<Dictionary<string, object>>(response);
                if (responseData.ContainsKey("data"))
                {
                    checkIfFail(response);

                    Dictionary<string, object> data = (Dictionary<string, object>)responseData["data"];
                    Int64 serverTime = (Int64)data["server_time"];

                    if (serverTime > lastServerTime) //check if serverTime is greater than stored server time - this way we know later requests are coming back last in the queue
                    {
                        successCounter++;
                        lastServerTime = serverTime;
                    }
                }

                if (successCounter == 4)
                {
                    Console.WriteLine("\n----- PASS -----");
                    //context.Disconnect();
                    lastServerTime = 0;
                    successCounter = 0;

                    // -----------------------------------------------------------------------
                    // Tests 8–20: GlobalFileV3 service tests
                    // -----------------------------------------------------------------------

                    // Test 8 - SysGetGlobalFileList (via service method)
                    startTest("TestGFV3GetGlobalFileList");
                    //context.Authenticate();
                    context.GlobalFileV3.SysGetGlobalFileList("", true, onTestGFV3GetGlobalFileListCallback);
                }
            }

            // -----------------------------------------------------------------------
            // Test 8: SysGetGlobalFileList
            // -----------------------------------------------------------------------
            void onTestGFV3GetGlobalFileListCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 9 - SysLookupFolder
                startTest("TestGFV3LookupFolder");
                //context.Authenticate();
                context.GlobalFileV3.SysLookupFolder("s2s_test_folder", onTestGFV3LookupFolderCallback);
            }

            // -----------------------------------------------------------------------
            // Test 9: SysLookupFolder
            // -----------------------------------------------------------------------
            void onTestGFV3LookupFolderCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 10 - SysCreateFolder
                startTest("TestGFV3CreateFolder");
                //context.Authenticate();
                context.GlobalFileV3.SysCreateFolder("s2s_test_folder", -1, "s2s_test_folder_2", "S2S integration test folder", false, onTestGFV3CreateFolderCallback);
            }

            // -----------------------------------------------------------------------
            // Test 10: SysCreateFolder — captures treeId for subsequent tests
            // -----------------------------------------------------------------------
            void onTestGFV3CreateFolderCallback(string response)
            {
                checkIfFail(response);

                try
                {
                    var r = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    var d = (Dictionary<string, object>)r["data"];
                    gfv3FolderTreeId = (string)d["createdTreeId"];
                }
                catch (Exception e)
                {
                    testFail("Could not extract treeId from SysCreateFolder response: " + e.Message);
                }

                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 11 - UploadGlobalFile
                startTest("TestGFV3UploadGlobalFile");
                //context.Authenticate();
                byte[] testFileData = System.Text.Encoding.UTF8.GetBytes("Hello from brainCloud S2S file upload test!");
                context.GlobalFileV3.UploadGlobalFile(gfv3FolderTreeId, "s2s_test_file.txt", true, testFileData, onTestGFV3UploadCallback);
            }

            // -----------------------------------------------------------------------
            // Test 11: UploadGlobalFile — captures fileId/version for subsequent tests
            // -----------------------------------------------------------------------
            void onTestGFV3UploadCallback(string response)
            {
                checkIfFail(response);

                try
                {
                    var r = JsonReader.Deserialize<Dictionary<string, object>>(response);
                    var d = (Dictionary<string, object>)r["data"];
                    var fd = (Dictionary<string, object>)d["fileDetails"];
                    var ffd = (Dictionary<string, object>)fd["fileDetails"];
                    gfv3FileId = (string)ffd["fileId"];
                    gfv3FileVersion = (int)ffd["version"];
                }
                catch (Exception e)
                {
                    testFail("Could not extract fileId/version from UploadGlobalFile response: " + e.Message);
                }

                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 12 - SysGetFileInfo
                startTest("TestGFV3GetFileInfo");
                //context.Authenticate();
                context.GlobalFileV3.SysGetFileInfo(gfv3FileId, onTestGFV3GetFileInfoCallback);
            }

            // -----------------------------------------------------------------------
            // Test 12: SysGetFileInfo
            // -----------------------------------------------------------------------
            void onTestGFV3GetFileInfoCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 13 - SysGetFileInfoSimple
                startTest("TestGFV3GetFileInfoSimple");
                //context.Authenticate();
                context.GlobalFileV3.SysGetFileInfoSimple("s2s_test_folder/s2s_test_folder_2", "s2s_test_file.txt", onTestGFV3GetFileInfoSimpleCallback);
            }

            // -----------------------------------------------------------------------
            // Test 13: SysGetFileInfoSimple
            // -----------------------------------------------------------------------
            void onTestGFV3GetFileInfoSimpleCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 14 - SysCheckFilenameExists
                startTest("TestGFV3CheckFilenameExists");
                //context.Authenticate();
                context.GlobalFileV3.SysCheckFilenameExists("s2s_test_folder/s2s_test_folder_2", "s2s_test_file.txt", onTestGFV3CheckFilenameExistsCallback);
            }

            // -----------------------------------------------------------------------
            // Test 14: SysCheckFilenameExists
            // -----------------------------------------------------------------------
            void onTestGFV3CheckFilenameExistsCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 15 - SysCheckFullpathFilenameExists
                startTest("TestGFV3CheckFullpathFilenameExists");
                //context.Authenticate();
                context.GlobalFileV3.SysCheckFullpathFilenameExists("s2s_test_folder/s2s_test_folder_2/s2s_test_file.txt", onTestGFV3CheckFullpathFilenameExistsCallback);
            }

            // -----------------------------------------------------------------------
            // Test 15: SysCheckFullpathFilenameExists
            // -----------------------------------------------------------------------
            void onTestGFV3CheckFullpathFilenameExistsCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 16 - SysGetGlobalCDNUrl
                startTest("TestGFV3GetGlobalCDNUrl");
                //context.Authenticate();
                context.GlobalFileV3.SysGetGlobalCDNUrl(gfv3FileId, onTestGFV3GetGlobalCDNUrlCallback);
            }

            // -----------------------------------------------------------------------
            // Test 16: SysGetGlobalCDNUrl
            // -----------------------------------------------------------------------
            void onTestGFV3GetGlobalCDNUrlCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 17 - SysCopyGlobalFile
                startTest("TestGFV3CopyGlobalFile");
                //context.Authenticate();
                context.GlobalFileV3.SysCopyGlobalFile(gfv3FileId, gfv3FileVersion, gfv3FolderTreeId, -1, "s2s_file_copy.txt", true, onTestGFV3CopyGlobalFileCallback);
            }

            // -----------------------------------------------------------------------
            // Test 17: SysCopyGlobalFile — copies uploaded file into test folder
            // -----------------------------------------------------------------------
            void onTestGFV3CopyGlobalFileCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 18 - SysMoveGlobalFile — move the original into the test folder
                startTest("TestGFV3MoveGlobalFile");
                //context.Authenticate();
                context.GlobalFileV3.SysMoveGlobalFile(gfv3FileId, gfv3FileVersion, gfv3FolderTreeId, -1, "s2s_file_moved.txt", true, onTestGFV3MoveGlobalFileCallback);
            }

            // -----------------------------------------------------------------------
            // Test 18: SysMoveGlobalFile — moves uploaded file into test folder
            // -----------------------------------------------------------------------
            void onTestGFV3MoveGlobalFileCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 19 - SysRenameFolder
                startTest("TestGFV3RenameFolder");
                //context.Authenticate();
                context.GlobalFileV3.SysRenameFolder(gfv3FolderTreeId, -1, "s2s_test_folder_renamed", onTestGFV3RenameFolderCallback);
            }

            // -----------------------------------------------------------------------
            // Test 19: SysRenameFolder
            // -----------------------------------------------------------------------
            void onTestGFV3RenameFolderCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 20 - SysDeleteGlobalFiles — delete all files in test folder
                startTest("TestGFV3DeleteGlobalFiles");
                //context.Authenticate();
                context.GlobalFileV3.SysDeleteGlobalFiles(gfv3FolderTreeId, "s2s_test_folder/s2s_test_folder_renamed", -1, true, onTestGFV3DeleteGlobalFilesCallback);
            }

            // -----------------------------------------------------------------------
            // Test 20: SysDeleteGlobalFiles — removes both copy and moved original
            // -----------------------------------------------------------------------
            void onTestGFV3DeleteGlobalFilesCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");
                //context.Disconnect();

                // Test 21 - SysDeleteFolder — cleanup the (now empty) test folder
                startTest("TestGFV3DeleteFolder");
                //context.Authenticate();
                context.GlobalFileV3.SysDeleteFolder(gfv3FolderTreeId, "s2s_test_folder/s2s_test_folder_renamed", -1, true, onTestGFV3DeleteFolderCallback);
            }

            // -----------------------------------------------------------------------
            // Test 21: SysDeleteFolder — final cleanup
            // -----------------------------------------------------------------------
            void onTestGFV3DeleteFolderCallback(string response)
            {
                checkIfFail(response);
                Console.WriteLine("\n----- PASS -----");

                // Test 22+ - RTT tests
                startTest("TestRTT");
                RTT_Test.RunTests(context,
                    () => { Console.WriteLine("\nALL TESTS PASS!"); Environment.Exit(0); },
                    testFail
                );
            }
        }
    }
}
