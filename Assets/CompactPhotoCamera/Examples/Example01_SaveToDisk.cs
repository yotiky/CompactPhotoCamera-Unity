using CompactPhotoCamera_Unity.Model.Device;
using HoloToolkit.Unity.InputModule;
using UniRx;
using UnityEngine;

namespace CompactPhotoCamera_Unity.Examples
{
    public class Example01_SaveToDisk : MonoBehaviour, IInputClickHandler
    {
        private CompactPhotoCamera photoCamera;

        void Start()
        {
            InputManager.Instance.AddGlobalListener(gameObject);
            photoCamera = new CompactPhotoCamera(true, true);
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