// Copyright (c) 2019 yotiky
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;

namespace CompactPhotoCamera_Unity.Model.Device
{
    public class CompactPhotoCamera
    {
        public class ShootingPlan
        {
            public Resolution CameraResolution { get; set; }
            public Vector3 CameraPosition { get; set; }
            public Matrix4x4 CameraToWorldMatrix { get; set; }
            public Matrix4x4 PixelToCameraMatrix { get; set; }
            public byte[] ImageBuffer { get; set; }
        }

        private Resolution cameraResolution;
        private PhotoCapture photoCaptureObject = null;
        private CameraParameters cameraParam;
        private bool canTakePhoto;
        private CapturePixelFormat format;
        private bool showHolograms;

        public ShootingPlan Plan { get; private set; }
        public bool CanTakePhoto => canTakePhoto;

        private Subject<Unit> onTaking = new Subject<Unit>();
        public IObservable<Unit> OnTaking => onTaking;

        private Subject<Unit> onCaptured = new Subject<Unit>();
        public IObservable<Unit> OnCaptured => onCaptured;

        public CompactPhotoCamera(bool showHolograms = false, bool saveToDisk = false)
        {
            cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            canTakePhoto = true;
            this.showHolograms = showHolograms;

            if (saveToDisk)
                format = CapturePixelFormat.BGRA32; // on disk
            else
                format = CapturePixelFormat.JPEG; // on memory
        }
        public void TakePhoto()
        {
            Debug.Log("Take photo ...");
            Plan = null;
            canTakePhoto = false;
            PhotoCapture.CreateAsync(showHolograms, OnPhotoCaptureCreated);
        }

        private void OnPhotoCaptureCreated(PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;

            cameraParam = new CameraParameters
            {
                hologramOpacity = showHolograms ? 0.9f : 0.0f,
                cameraResolutionWidth = cameraResolution.width,
                cameraResolutionHeight = cameraResolution.height,
                pixelFormat = format,
            };
            captureObject.StartPhotoModeAsync(cameraParam, OnPhotoModeStarted);
        }
        private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                onTaking.OnNext(Unit.Default);
                if (cameraParam.pixelFormat == CapturePixelFormat.JPEG)
                {
                    // on memory (pixelFormat = CapturePixelFormat.JPEG)
                    photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                }
                else
                {
                    // to disk (pixelFormat = CapturePixelFormat.BGRA32)
                    var filename = $"CapturedPhoto_{(DateTime.Now.ToString("yyyyMMdd_HHmmss"))}.jpg";
                    var filePath = Path.Combine(PictureFileDirectoryPath(), filename);
                    Debug.Log(filePath);
                    photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
                }
            }
            else
            {
                Debug.LogError("Unable to start photo mode.");
                canTakePhoto = true;
            }
        }
        private void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
        {
            if (result.success)
            {
                var imageBufferListRaw = new List<byte>();
                photoCaptureFrame.CopyRawImageDataIntoBuffer(imageBufferListRaw);

                var imageBufferList = imageBufferListRaw.ToArray();

                // カメラの向きをワールド座標に変換するためのパラメータ保持
                var cameraToWorldMatrix = new Matrix4x4();
                photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);

                Matrix4x4 projectionMatrix;
                photoCaptureFrame.TryGetProjectionMatrix(Camera.main.nearClipPlane, Camera.main.farClipPlane, out projectionMatrix);
                var pixelToCameraMatrix = projectionMatrix.inverse;

                Plan = new ShootingPlan
                {
                    CameraResolution = cameraResolution,
                    CameraPosition = Camera.current.transform.position,
                    ImageBuffer = imageBufferListRaw.ToArray(),
                    CameraToWorldMatrix = cameraToWorldMatrix,
                    PixelToCameraMatrix = pixelToCameraMatrix,
                };

                Debug.Log("Done...");
            }
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

            onCaptured.OnNext(Unit.Default);
        }
        private string PictureFileDirectoryPath()
        {
            string directorypath = "";
#if WINDOWS_UWP
            // HoloLens上での動作の場合、LocalAppData/AppName/LocalStateフォルダを参照する
            directorypath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;

#else
            // Unity上での動作の場合、Assetsフォルダ直下を参照する
            directorypath = UnityEngine.Application.dataPath;
#endif
            return directorypath;
        }
        private void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
        {
            if (result.success)
            {
                Debug.Log("Done...");
            }
            else
            {
                Debug.LogError("Failed to save Photo to disk");
            }
            photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);

            onCaptured.OnNext(Unit.Default);
        }
        private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
        {
            photoCaptureObject.Dispose();
            photoCaptureObject = null;
            canTakePhoto = true;
        }
    }
}