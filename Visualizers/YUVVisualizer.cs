using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;


namespace MagicLeap
{
    /// <summary>
    /// of the recording.
    /// </summary>
    
    
    
    
    public class YUVVisualizer : MonoBehaviour
    {
        [SerializeField, Tooltip("The UI to show the camera capture in YUV format")]
        private Renderer _screenRendererYUV = null;

        [Header("Visuals")]
        [SerializeField, Tooltip("Object that will show up when recording")]
        private GameObject _recordingIndicator = null;

        #pragma warning disable 414
        [SerializeField, Tooltip("Posterization levels of the frame processor")]
        private byte _posterizationLevels = 4;





        //The Image Textures for each channel Y,U,V
        private Texture2D[] _rawVideoTexturesYuv = new Texture2D[3];
        private byte[] _yChannelBuffer;
        private byte[] _uChannelBuffer;
        private byte[] _vChannelBuffer;

        private static readonly string[] SamplerNamesYuv = new string[] { "_MainTex", "_UTex", "_VTex" };

        // The texture that will display our final image
        private RenderTexture _renderTexture;
        private Material _yuvMaterial;
        private CommandBuffer _commandBuffer;








        /// <summary>
        /// Check for all required variables to be initialized.
        /// </summary>
        void Start()
        {
            if (_screenRendererYUV == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._screenRenderer is not set, disabling script.");
                enabled = false;
                return;
            }

            if (_recordingIndicator == null)
            {
                Debug.LogError("Error: RawVideoCaptureVisualizer._recordingIndicator is not set, disabling script.");
                enabled = false;
                return;
            }

            _screenRendererYUV.enabled = false;
        }

        /// <summary>
        /// Handles video capture being started.
        /// </summary>
        public void OnCaptureStarted()
        {
            // Manage canvas visuals
            _screenRendererYUV.enabled = true;
            _recordingIndicator.SetActive(true);
        }

        /// <summary>
        /// Handles video capture ending.
        /// </summary>
        public void OnCaptureEnded()
        {
            _recordingIndicator.SetActive(false);
        }




        #if PLATFORM_LUMIN
        /// <summary>
        /// Display the raw video frame on the texture object.
        /// </summary>
        public void OnCaptureDataReceived(MLCamera.ResultExtras extras, MLCamera.YUVFrameInfo imagePlane, MLCamera.FrameMetadata frameMetadata)
        
        {
            MLCamera.YUVBuffer yBuffer = imagePlane.Y;


            //if (output.Format == MLCamera.OutputFormat.YUV_420_888)        // TEST DELETE
            //{
                UpdateYUVTextureChannel(ref _rawVideoTexturesYuv[0], imagePlane,
                          SamplerNamesYuv[0], ref _yChannelBuffer);
                UpdateYUVTextureChannel(ref _rawVideoTexturesYuv[1], imagePlane,
                    SamplerNamesYuv[1], ref _uChannelBuffer);
                UpdateYUVTextureChannel(ref _rawVideoTexturesYuv[2], imagePlane,
                    SamplerNamesYuv[2], ref _vChannelBuffer);

                if (!_renderTexture)
                {
                    // Create a render texture that will display the RGB image
                    _renderTexture = new RenderTexture((int)yBuffer.Width, (int)(yBuffer.Height), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

                    // Create a command buffer that will be used for Blitting
                    _commandBuffer = new CommandBuffer();
                    _commandBuffer.name = "YUV2RGB";

                    // Create a Material with a shader that will combine all of our channels into a single Render Texture
                    _yuvMaterial = new Material(Shader.Find("Unlit/YUV_Camera_Shader"));

                    // Assign the RawImage Texture to the Render Texture
                    _screenRendererYUV.material.mainTexture = _renderTexture;
                }

                // Set the texture's scale based on the output image
                _yuvMaterial.mainTextureScale = new Vector2(1f / (int)yBuffer.Stride, -1.0f);

                // Blit the resulting Material into a single render texture
                _commandBuffer.Blit(null, _renderTexture, _yuvMaterial);
                Graphics.ExecuteCommandBuffer(_commandBuffer);
                _commandBuffer.Clear();
            //}
        }
        #endif

        /// <summary>
        /// Disables the renderer.
        /// </summary>
        public void OnRawCaptureEnded()
        {
            _recordingIndicator.SetActive(false);
            _screenRendererYUV.enabled = false;
        }

        private void UpdateYUVTextureChannel(ref Texture2D channelTexture, MLCamera.YUVFrameInfo imagePlane,
                                                   string samplerName, ref byte[] newTextureChannel) //MLCamera.PlaneInfo imagePlane
        {
            MLCamera.YUVBuffer yBuffer = imagePlane.Y;

            if (channelTexture != null &&
                (channelTexture.width != yBuffer.Width || channelTexture.height != yBuffer.Height))
            {
                Destroy(channelTexture);
                channelTexture = null;
            }

            if (channelTexture == null)
            {
                if (yBuffer.Stride == 2)
                {
                    channelTexture = new Texture2D((int)yBuffer.Width, (int)(yBuffer.Height), TextureFormat.RG16, false)
                    {
                        filterMode = FilterMode.Bilinear
                    };
                }
                else
                {
                    channelTexture = new Texture2D((int)yBuffer.Width, (int)(yBuffer.Height), TextureFormat.Alpha8, false)
                    {
                        filterMode = FilterMode.Bilinear
                    };
                }
                _yuvMaterial.SetTexture(samplerName, channelTexture);
            }

            int actualWidth = (int)(yBuffer.Width * yBuffer.Stride);
            if (yBuffer.Stride != actualWidth)
            {
                if (newTextureChannel == null || newTextureChannel.Length != (actualWidth * yBuffer.Height))
                {
                    newTextureChannel = new byte[actualWidth * yBuffer.Height];
                }

                for (int i = 0; i < yBuffer.Height; i++)
                {
                    Buffer.BlockCopy(yBuffer.Data, (int)(i * yBuffer.Stride), newTextureChannel,
                        i * actualWidth, actualWidth);
                }

                channelTexture.LoadRawTextureData(newTextureChannel);
            }
            else
            {
                channelTexture.LoadRawTextureData(yBuffer.Data);
            }

            channelTexture.Apply();
        }
    }
}