using CompactPhotoCamera_Unity.Model.Device;
using HoloToolkit.Unity.InputModule;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace CompactPhotoCamera_Unity.Examples
{
    public class Example03_CountDown : MonoBehaviour, IInputClickHandler
    {
        public int sec = 3;
        public TextMesh text;
        public GameObject focus;
        public RawImage preview;

        private CompactPhotoCamera photoCamera;
        private IDisposable disposable;

        private readonly Subject<int> onTick = new Subject<int>();
        private readonly Subject<Unit> onCountDownCompleted = new Subject<Unit>();

        void Start()
        {
            InputManager.Instance.AddGlobalListener(gameObject);

            photoCamera = new CompactPhotoCamera(true);
            photoCamera.OnTaking
                .Subscribe(_ =>
                {
                    // 撮影直前にカウントダウンを非表示にする
                    UndrawCountDown();
                })
                .AddTo(this);
            photoCamera.OnCaptured
                .Where(_ => photoCamera.Plan != null)
                .Subscribe(_ =>
                {
                    // 画像に対して何か処理したい場合はここらで
                    var texture = new Texture2D(photoCamera.Plan.CameraResolution.width, photoCamera.Plan.CameraResolution.height);
                    texture.LoadImage(photoCamera.Plan.ImageBuffer);

                    preview.texture = texture;

                    ReadyCountDown();
                })
                .AddTo(this);

            onTick
                .Where(x => x == 1)
                .Subscribe(_ =>
                {
                    if (photoCamera.CanTakePhoto)
                    {
                        // 実際に撮影する際はラグが発生するので1秒前に処理を開始して、シャッター音をカウントダウンに合わせることでUXを向上させる
                        photoCamera.TakePhoto();
                    }
                })
                .AddTo(this);
            onCountDownCompleted
                .Subscribe(_ =>
                {
                    // シャッター音を再生する場合はここらで
                })
                .AddTo(this);

            InitializeCountDown();
        }

        private void InitializeCountDown()
        {
            focus.SetActive(true);
            text.text = "";
            if (disposable != null)
            {
                disposable.Dispose();
                disposable = null;
            }
        }
        private void ReadyCountDown()
            => InitializeCountDown();

        private void BeginCountDown()
        {
            disposable = Observable.Timer(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1))
                .Select(x => (int)(sec - x))
                .Do(time => onTick.OnNext(time))
                .TakeWhile(x => x > 0)
                .Subscribe(
                    time => text.text = time.ToString(),
                    () => onCountDownCompleted.OnNext(Unit.Default)
                );
            focus.SetActive(false);
        }

        private void UndrawCountDown()
        {
            focus.SetActive(false);
            text.text = "";
        }

        public void OnInputClicked(InputClickedEventData eventData)
        {
            if(disposable == null)
            {
                BeginCountDown();
            }
        }

    }
}