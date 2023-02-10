using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;

public class YUVVisualizer : MonoBehaviour
{
    [SerializeField, Tooltip("The UI to show the camera capture in YUV format")]
    private RawImage _screenRendererYUV = null;

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

    public void OnCaptureDataReceived(MLCamera.CameraOutput output, MLCamera.ResultExtras extras)
    {
        if (output.Format == MLCamera.OutputFormat.YUV_420_888)
        {
            UpdateYUVTextureChannel(ref _rawVideoTexturesYuv[0], output.Planes[0],
                      SamplerNamesYuv[0], ref _yChannelBuffer);
            UpdateYUVTextureChannel(ref _rawVideoTexturesYuv[1], output.Planes[1],
                SamplerNamesYuv[1], ref _uChannelBuffer);
            UpdateYUVTextureChannel(ref _rawVideoTexturesYuv[2], output.Planes[2],
                SamplerNamesYuv[2], ref _vChannelBuffer);

            if (!_renderTexture)
            {
                // Create a render texture that will display the RGB image
                _renderTexture = new RenderTexture((int)output.Planes[0].Width, (int)(output.Planes[0].Height), 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                
                // Create a command buffer that will be used for Blitting
                _commandBuffer = new CommandBuffer();
                _commandBuffer.name = "YUV2RGB";

                // Create a Material with a shader that will combine all of our channels into a single Render Texture
                _yuvMaterial = new Material(Shader.Find("Unlit/YUV_Camera_Shader"));

                // Assign the RawImage Texture to the Render Texture
                _screenRendererYUV.texture = _renderTexture;
            }

            // Set the texture's scale based on the output image
            _yuvMaterial.mainTextureScale = new Vector2(1f / output.Planes[0].PixelStride, -1.0f);

            // Blit the resulting Material into a single render texture
            _commandBuffer.Blit(null, _renderTexture, _yuvMaterial);
            Graphics.ExecuteCommandBuffer(_commandBuffer);
            _commandBuffer.Clear();
        }
    }

    private void UpdateYUVTextureChannel(ref Texture2D channelTexture, MLCamera.PlaneInfo imagePlane,
                                               string samplerName, ref byte[] newTextureChannel)
    {
        if (channelTexture != null &&
            (channelTexture.width != imagePlane.Width || channelTexture.height != imagePlane.Height))
        {
            Destroy(channelTexture);
            channelTexture = null;
        }

        if (channelTexture == null)
        {
            if (imagePlane.PixelStride == 2)
            {
                channelTexture = new Texture2D((int)imagePlane.Width, (int)(imagePlane.Height), TextureFormat.RG16, false)
                {
                    filterMode = FilterMode.Bilinear
                };
            }
            else
            {
                channelTexture = new Texture2D((int)imagePlane.Width, (int)(imagePlane.Height), TextureFormat.Alpha8, false)
                {
                    filterMode = FilterMode.Bilinear
                };
            }
            _yuvMaterial.SetTexture(samplerName, channelTexture);
        }

        int actualWidth = (int)(imagePlane.Width * imagePlane.PixelStride);
        if (imagePlane.Stride != actualWidth)
        {
            if (newTextureChannel == null || newTextureChannel.Length != (actualWidth * imagePlane.Height))
            {
                newTextureChannel = new byte[actualWidth * imagePlane.Height];
            }

            for (int i = 0; i < imagePlane.Height; i++)
            {
                Buffer.BlockCopy(imagePlane.Data, (int)(i * imagePlane.Stride), newTextureChannel,
                    i * actualWidth, actualWidth);
            }

            channelTexture.LoadRawTextureData(newTextureChannel);
        }
        else
        {
            channelTexture.LoadRawTextureData(imagePlane.Data);
        }

        channelTexture.Apply();
    }
}