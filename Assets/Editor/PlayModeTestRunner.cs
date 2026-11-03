using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Unity.AI.Assistant.PlayModeTest
{
    [InitializeOnLoad]
    internal static class PlayModeTestRunner
    {
        private const string StateKey = "PlayModeTest.State";
        private const string ResultKey = "PlayModeTest.Result";
        private const string ScriptPathKey = "PlayModeTest.ScriptPath";

        static PlayModeTestRunner()
        {
            string state = SessionState.GetString(StateKey, "Idle");
            if (state == "WaitingForCompile")
            {
                EditorApplication.delayCall += () => {
                    SessionState.SetString(StateKey, "EnteringPlayMode");
                    EditorApplication.isPlaying = true;
                };
            }
            else if (state == "EnteringPlayMode" && EditorApplication.isPlaying)
            {
                SessionState.SetString(StateKey, "InPlayMode");
                EditorApplication.update += RunTest;
            }
        }

        private static int frameCount = 0;
        private static void RunTest()
        {
            frameCount++;
            if (frameCount < 10) return;
            EditorApplication.update -= RunTest;

            TestResult result = new TestResult();
            try
            {
                CubeWorld world = Object.FindAnyObjectByType<CubeWorld>();
                GameObject voxelRoot = GameObject.Find("VoxelRoot");
                
                if (world != null && voxelRoot != null)
                {
                    int startCount = voxelRoot.transform.childCount;
                    result.startCount = startCount;

                    // Simulate a ray through the center of the cube
                    Ray ray = new Ray(new Vector3(0, 0, -10), Vector3.forward);
                    world.TryRemoveVoxel(ray, 1.0f); // Large brush

                    int endCount = voxelRoot.transform.childCount;
                    result.endCount = endCount;
                    result.success = endCount < startCount;
                    
                    if (!result.success) {
                        // Diagnostic: Check if raycast hits anything at all
                        if (Physics.Raycast(ray, out RaycastHit hit, 100f)) {
                            result.hitName = hit.collider.name;
                        } else {
                            result.hitName = "MISS";
                        }
                    }
                }
                else
                {
                    result.error = "CubeWorld or VoxelRoot not found";
                }
            }
            catch (System.Exception e)
            {
                result.error = e.Message;
            }

            SessionState.SetString(ResultKey, JsonUtility.ToJson(result));
            SessionState.SetString(StateKey, "Done");
            EditorApplication.isPlaying = false;
        }

        [System.Serializable]
        public class TestResult
        {
            public bool success;
            public string error;
            public int startCount;
            public int endCount;
            public string hitName;
        }
    }
}
