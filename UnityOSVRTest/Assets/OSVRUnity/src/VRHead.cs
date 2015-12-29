/// OSVR-Unity Connection
///
/// http://sensics.com/osvr
///
/// <copyright>
/// Copyright 2014 Sensics, Inc.
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///     http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
/// </copyright>

using UnityEngine;
using System.Collections;

namespace OSVR
{
    namespace Unity
    {
        [RequireComponent(typeof(Camera))]  
        public class VRViewer : MonoBehaviour
        {   
            #region Public Variables         
            public DisplayController DisplayController { get { return _displayController; } set { _displayController = value; } }
            public VREye[] Eyes { get { return _eyes; } }
            public uint EyeCount { get { return _eyeCount; } }
            public uint ViewerIndex { get { return _viewerIndex; } set { _viewerIndex = value; } }
            [HideInInspector]
            public Transform cachedTransform;
            public Camera Camera
            {
                get
                {
                    if (_camera == null)
                    {
                        _camera = GetComponent<Camera>();
                    }
                    return _camera;
                }
                set { _camera = value; }
            }
            #endregion

            #region Private Variables
            private DisplayController _displayController;
            private VREye[] _eyes;
            private uint _eyeCount;
            private uint _viewerIndex;
            private Camera _camera;
            private bool _disabledCamera = true;
            

            #endregion

            void Awake()
            {
                Init();
            }

            void Init()
            {
                _camera = GetComponent<Camera>();
                //cache:
                cachedTransform = transform;
                if (DisplayController == null)
                {
                    DisplayController = FindObjectOfType<DisplayController>();
                }
            }

            void OnEnable()
            {
                StartCoroutine("EndOfFrame");
            }

            void OnDisable()
            {
                StopCoroutine("EndOfFrame");
                if (DisplayController.UseRenderManager && DisplayController.RenderManager != null)
                {
                    DisplayController.ExitRenderManager();
                }
            }

            //Creates the Eyes of this Viewer
            public void CreateEyes(uint eyeCount)
            {
                _eyeCount = eyeCount; //cache the number of eyes this viewer controls
                _eyes = new VREye[_eyeCount];
                for (uint eyeIndex = 0; eyeIndex < _eyeCount; eyeIndex++)
                {
                    GameObject eyeGameObject = new GameObject("Eye" + eyeIndex); //add an eye gameobject to the scene
                    VREye eye = eyeGameObject.AddComponent<VREye>(); //add the VReye component
                    eye.Viewer = this; //ASSUME THERE IS ONLY ONE VIEWER
                    eye.EyeIndex = eyeIndex; //set the eye's index
                    eyeGameObject.transform.parent = DisplayController.transform; //child of DisplayController
                    eyeGameObject.transform.localPosition = Vector3.zero;
                    _eyes[eyeIndex] = eye;
                    uint eyeSurfaceCount = DisplayController.DisplayConfig.GetNumSurfacesForViewerEye(ViewerIndex, (byte)eyeIndex);
                    eye.CreateSurfaces(eyeSurfaceCount);
                }
            }

            //Get an updated tracker position + orientation
            public OSVR.ClientKit.Pose3 GetViewerPose(uint viewerIndex)
            {
                return DisplayController.DisplayConfig.GetViewerPose(viewerIndex);
            }

            //Updates the position and rotation of the head
            public void UpdateViewerHeadPose(OSVR.ClientKit.Pose3 headPose)
            {
                cachedTransform.localPosition = Math.ConvertPosition(headPose.translation);
                cachedTransform.localRotation = Math.ConvertOrientation(headPose.rotation);
            }

            //Update the pose of each eye, then update and render each eye's surfaces
            public void UpdateEyes()
            {
                if (DisplayController.UseRenderManager)
                {
                    //Update RenderInfo
#if UNITY_5_2 || UNITY_5_3
                    GL.IssuePluginEvent(DisplayController.RenderManager.GetRenderEventFunction(), OsvrRenderManager.UPDATE_RENDERINFO_EVENT);
#else
                    Debug.LogError("GL.IssuePluginEvent failed. This version of Unity cannot support RenderManager.");
                    DisplayController.UseRenderManager = false;
#endif
                }
                else
                {
                    DisplayController.UpdateClient();
                }
                    
                for (uint eyeIndex = 0; eyeIndex < EyeCount; eyeIndex++)
                {                   
                    //update the eye pose
                    VREye eye = Eyes[eyeIndex];
                    //get eye pose from DisplayConfig
                    //@todo fix bug with poses coming from RenderManager
                    eye.UpdateEyePose(_displayController.DisplayConfig.GetViewerEyePose(ViewerIndex, (byte)eyeIndex));
                    /*if (DisplayController.UseRenderManager)
                    { 
                        //get eye pose from RenderManager                     
                        eye.UpdateEyePose(DisplayController.RenderManager.GetRenderManagerEyePose((byte)eyeIndex));
                    }
                    else
                    {
                        //get eye pose from DisplayConfig
                        eye.UpdateEyePose(_displayController.DisplayConfig.GetViewerEyePose(ViewerIndex, (byte)eyeIndex));
                    }*/
                        

                    // update the eye's surfaces, includes call to Render
                    eye.UpdateSurfaces();                   
                }
            }

            //helper method for updating the client context
            public void UpdateClient()
            {
                DisplayController.UpdateClient();
            }

            // Culling determines which objects are visible to the camera. OnPreCull is called just before this process.
            // This gets called because we have a camera component, but we disable the camera here so it doesn't render.
            // We have the "dummy" camera so existing Unity game code can refer to a MainCamera object.
            // We update our viewer and eye transforms here because it is as late as possible before rendering happens.
            // OnPreRender is not called because we disable the camera here.
            void OnPreCull()
            {
                
                if(!DisplayController.CheckDisplayStartup())
                {
                    //leave this preview camera enabled if there is no display config
                    _camera.enabled = true;
                }
                else
                {
                    // To save Render time, disable this camera here and re-enable after the frame
                    // OR, in DirectMode, leave it on for "mirror" mode, although this is an expensive operation
                    // The long-term solution is to provide a DirectMode preview window in RenderManager
                    //@todo enable directmode preview in RenderManager
                    _camera.enabled = DisplayController.UseRenderManager && DisplayController.showDirectModePreview;
                }

                DoRendering();

                // Flag that we disabled the camera
                _disabledCamera = true;
            }

            // The main rendering loop, should be called late in the pipeline, i.e. from OnPreCull
            // Set our viewer and eye poses and render to each surface.
            void DoRendering()
            {
                // update poses once DisplayConfig is ready
                if (DisplayController.CheckDisplayStartup())
                {
                    // update the viewer's head pose
                    // @todo Get viewer pose from RenderManager if UseRenderManager = true
                    // currently getting viewer pose from DisplayConfig always
                    UpdateViewerHeadPose(GetViewerPose(ViewerIndex));

                    // each viewer updates its eye poses, viewports, projection matrices
                    UpdateEyes();

                }
                else
                {
                    if (!DisplayController.CheckDisplayStartup())
                    {
                        //@todo do something other than not show anything
                        Debug.LogError("Display Startup failed. Check HMD connection.");
                    }
                }
            }

            // This couroutine is called every frame.
            IEnumerator EndOfFrame()
            {
                while (true)
                {
                    //if we disabled the dummy camera, enable it here
                    if (_disabledCamera)
                    {
                        Camera.enabled = true;
                        _disabledCamera = false;
                    }
                    yield return new WaitForEndOfFrame();
                    if (DisplayController.UseRenderManager && DisplayController.CheckDisplayStartup())
                    {
                        // Issue a RenderEvent, which copies Unity RenderTextures to RenderManager buffers
#if UNITY_5_2 || UNITY_5_3
                        GL.IssuePluginEvent(DisplayController.RenderManager.GetRenderEventFunction(), OsvrRenderManager.RENDER_EVENT);
#else
                        Debug.LogError("GL.IssuePluginEvent failed. This version of Unity cannot support RenderManager.");
                        DisplayController.UseRenderManager = false;
#endif
                    }

                }
            }             
        }
    }
}
