using CompactPhotoCamera_Unity.Model.Device;
using HoloToolkit.Unity.InputModule;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CompactPhotoCamera_Unity.Examples
{
    public class Example02_OnMemory : MonoBehaviour, IInputClickHandler
    {
        public RawImage preview;

        private CompactPhotoCamera photoCamera;

        void Start()
        {
            InputManager.Instance.AddGlobalListener(gameObject);

            preview.GetComponent<RectTransform>().sizeDelta = new Vector2(320, 180);

            photoCamera = new CompactPhotoCamera(true);
            photoCamera.OnCaptured
                .Where(_ => photoCamera.Plan != null)
                .Subscribe(_ =>
                {
                    // 画像に対して何か処理したい場合はここらで
                    var texture = new Texture2D(photoCamera.Plan.CameraResolution.width, photoCamera.Plan.CameraResolution.height);
                    texture.LoadImage(photoCamera.Plan.ImageBuffer);

                    preview.texture = texture;
                })
                .AddTo(this);
        }

        public void OnInputClicked(InputClickedEventData eventData)
        {
            if (photoCamera.CanTakePhoto)
            {
                // シャッター音を再生する場合はここらで
                photoCamera.TakePhoto();
            }
        }

    }
}