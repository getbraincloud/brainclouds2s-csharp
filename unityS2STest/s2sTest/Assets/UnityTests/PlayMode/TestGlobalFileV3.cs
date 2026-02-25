using System;
using System.Collections;
using System.Collections.Generic;
using BrainCloud.JsonFx.Json;
using Tests.PlayMode;
using UnityEngine;
using UnityEngine.TestTools;

public class TestGlobalFileV3 : TestFixtureBase
{
    // Static so they survive NUnit's per-test class instantiation and persist
    // across the ordered test sequence.
    private static string _gfv3FolderTreeId = "";
    private static string _gfv3FileId = "";
    private static int _gfv3FileVersion = 1;

    // Shared simple callback: increments successCount and signals done.
    void OnSuccess(string response)
    {
        _tc.successCount++;
        _tc.m_done = true;
    }

    // -----------------------------------------------------------------------
    // Test 1 (parity: dotnet #8): SysGetGlobalFileList
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3GetGlobalFileList()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysGetGlobalFileList("", true, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysGetGlobalFileList failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 2 (parity: dotnet #9): SysLookupFolder
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3LookupFolder()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysLookupFolder("s2s_test_folder", OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysLookupFolder failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 3 (parity: dotnet #10): SysCreateFolder — captures treeId
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3CreateFolder()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysCreateFolder(
            "s2s_test_folder", -1, "s2s_test_folder_2",
            "S2S integration test folder", false, OnCreateFolderCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysCreateFolder failed", _tc.successCount == 2);
    }

    void OnCreateFolderCallback(string response)
    {
        try
        {
            var r = JsonReader.Deserialize<Dictionary<string, object>>(response);
            var d = (Dictionary<string, object>)r["data"];
            _gfv3FolderTreeId = (string)d["createdTreeId"];
            _tc.successCount++;
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not extract treeId from SysCreateFolder response: {e.Message}");
        }
        _tc.m_done = true;
    }

    // -----------------------------------------------------------------------
    // Test 4 (parity: dotnet #11): UploadGlobalFile — captures fileId/version
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3UploadGlobalFile()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        byte[] fileData = System.Text.Encoding.UTF8.GetBytes("Hello from brainCloud S2S file upload test!");
        _tc.context.GlobalFileV3.UploadGlobalFile(
            _gfv3FolderTreeId, "s2s_test_file.txt", true, fileData, OnUploadCallback);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("UploadGlobalFile failed", _tc.successCount == 2);
    }

    void OnUploadCallback(string response)
    {
        try
        {
            var r = JsonReader.Deserialize<Dictionary<string, object>>(response);
            var d = (Dictionary<string, object>)r["data"];
            var fd = (Dictionary<string, object>)d["fileDetails"];
            var ffd = (Dictionary<string, object>)fd["fileDetails"];
            _gfv3FileId = (string)ffd["fileId"];
            _gfv3FileVersion = (int)ffd["version"];
            _tc.successCount++;
        }
        catch (Exception e)
        {
            Debug.LogError($"Could not extract fileId/version from UploadGlobalFile response: {e.Message}");
        }
        _tc.m_done = true;
    }

    // -----------------------------------------------------------------------
    // Test 5 (parity: dotnet #12): SysGetFileInfo
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3GetFileInfo()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysGetFileInfo(_gfv3FileId, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysGetFileInfo failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 6 (parity: dotnet #13): SysGetFileInfoSimple
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3GetFileInfoSimple()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysGetFileInfoSimple(
            "s2s_test_folder/s2s_test_folder_2", "s2s_test_file.txt", OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysGetFileInfoSimple failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 7 (parity: dotnet #14): SysCheckFilenameExists
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3CheckFilenameExists()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysCheckFilenameExists(
            "s2s_test_folder/s2s_test_folder_2", "s2s_test_file.txt", OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysCheckFilenameExists failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 8 (parity: dotnet #15): SysCheckFullpathFilenameExists
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3CheckFullpathFilenameExists()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysCheckFullpathFilenameExists(
            "s2s_test_folder/s2s_test_folder_2/s2s_test_file.txt", OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysCheckFullpathFilenameExists failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 9 (parity: dotnet #16): SysGetGlobalCDNUrl
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3GetGlobalCDNUrl()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysGetGlobalCDNUrl(_gfv3FileId, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysGetGlobalCDNUrl failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 10 (parity: dotnet #17): SysCopyGlobalFile
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3CopyGlobalFile()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysCopyGlobalFile(
            _gfv3FileId, _gfv3FileVersion, _gfv3FolderTreeId, -1,
            "s2s_file_copy.txt", true, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysCopyGlobalFile failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 11 (parity: dotnet #18): SysMoveGlobalFile
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3MoveGlobalFile()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysMoveGlobalFile(
            _gfv3FileId, _gfv3FileVersion, _gfv3FolderTreeId, -1,
            "s2s_file_moved.txt", true, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysMoveGlobalFile failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 12 (parity: dotnet #19): SysRenameFolder
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3RenameFolder()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysRenameFolder(
            _gfv3FolderTreeId, -1, "s2s_test_folder_renamed", OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysRenameFolder failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 13 (parity: dotnet #20): SysDeleteGlobalFiles — cleanup files
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3DeleteGlobalFiles()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysDeleteGlobalFiles(
            _gfv3FolderTreeId, "s2s_test_folder/s2s_test_folder_renamed", -1, true, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysDeleteGlobalFiles failed", _tc.successCount == 2);
    }

    // -----------------------------------------------------------------------
    // Test 14 (parity: dotnet #21): SysDeleteFolder — cleanup folder
    // -----------------------------------------------------------------------
    [UnityTest]
    public IEnumerator TestGFV3DeleteFolder()
    {
        _tc.context.Authenticate(OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        _tc.context.GlobalFileV3.SysDeleteFolder(
            _gfv3FolderTreeId, "s2s_test_folder/s2s_test_folder_renamed", -1, true, OnSuccess);
        yield return _tc.StartCoroutine(_tc.Run());
        LogResults("SysDeleteFolder failed", _tc.successCount == 2);
    }
}
