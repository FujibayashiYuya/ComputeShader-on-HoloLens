using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity.InputModule;
using System;
using System.IO;
using System.Linq;
using UnityEngine.XR.WSA.WebCam;

public class AirTapBehavior : MonoBehaviour, IInputClickHandler
{
    //Unity側で指定
    //targetObjectいらんかも、formatMaterialはComputeShaderにするかも
    public GameObject targetObject = null;
    //public Material formatMaterial = null;
    public ComputeShader shader;

    private PhotoCapture photoCaptureObject = null;
    private Material changeMaterial = null;
    private RenderTexture destTexture;
    // Use this for initialization
    void Start()
    {
        // AirTap時のイベントを設定する
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    //オブジェクトの保存・写真モード開始
    void OnPhotoCaptureCreated(PhotoCapture captureObject)
    {
        Debug.Log("OnPhotoCaptureCreated");
        photoCaptureObject = captureObject;

        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

        CameraParameters c = new CameraParameters();
        c.hologramOpacity = 0.0f;
        c.cameraResolutionWidth = cameraResolution.width;
        c.cameraResolutionHeight = cameraResolution.height;
        c.pixelFormat = CapturePixelFormat.BGRA32;

        captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
    }

    //クリーンアップ
    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.Log("OnStoppedPhotoMode");
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
    }

    private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
    {
        Debug.Log("OnPhotoModeStarted");
        if (result.success)
        {
            Debug.Log("OnPhotoModeStarted: success");
            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
        }
        else
        {
            Debug.LogError("OnPhotoModeStarted: Unable to start photo mode!");
        }
    }

    //Texture2D
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        Debug.Log("OnCapturedPhotoToMemory");
        if (result.success)
        {
            Debug.Log("OnCapturedPhotoToMemory: success");
            // 使用するTexture2Dを作成し、正しい解像度を設定する
            Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
            // 画像データをターゲットテクスチャにコピーする
            photoCaptureFrame.UploadImageDataToTexture(targetTexture);

            destTexture = new RenderTexture(cameraResolution.width, cameraResolution.height, 0, RenderTextureFormat.ARGBFloat);
            destTexture.enableRandomWrite = true;
            destTexture.Create();

            /*         var rTex = new RenderTexture(cameraResolution.width, cameraResolution.height, 0, RenderTextureFormat.ARGBFloat);
                     rTex.enableRandomWrite = true;
                     rTex.Create();
                     dstImg.texture = rTex;*/

            //ComputeShaderにデータ送信
            shader.SetTexture(0, "Src_Tex", targetTexture);
            shader.SetTexture(0, "Res_Tex", destTexture);
            shader.Dispatch(0, cameraResolution.width / 16, cameraResolution.height / 16, 1);
            
            // テクスチャをマテリアルに適用する
            //changeMaterial = new Material(formatMaterial);
            changeMaterial.SetTexture("_MainTex", destTexture);
            //表示しなくていいからこれはいらんかも
            targetObject.GetComponent<Image>().material = changeMaterial;
        }
        // クリーンアップ
        // Clean up
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    private void OnDestroy()
    {
        
    }

    /// <summary>
    /// クリックイベント
    /// </summary>
    public void OnInputClicked(InputClickedEventData eventData)
    {
        Debug.Log("capture");
        // キャプチャを開始する
        PhotoCapture.CreateAsync(true, OnPhotoCaptureCreated);
    }

    // Update is called once per frame
    void Update()
    {
    }
}