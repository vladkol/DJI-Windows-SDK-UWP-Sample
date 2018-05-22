using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace DJIDemo
{
    public class MainPageViewModel : INotifyPropertyChanged
    {
        private CoreDispatcher Dispatcher;
        private DJIClient djiClient;

        private Windows.Media.SpeechSynthesis.SpeechSynthesizer speechSynthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
        private string lastAudioTag = string.Empty;
        private MediaPlayer player = new MediaPlayer();

        private FruitWinML.FruitModel mlModel = null;
        private Task runProcessTask = null;


        public MainPageViewModel(CoreDispatcher dispatcher, DJIClient djiClient)
        {
            this.Dispatcher = dispatcher;
            this.djiClient = djiClient;
            djiClient.ConnectedChanged += DjiClient_ConnectedChanged;
            djiClient.FlyingChanged += DjiClient_FlyingChanged;
            djiClient.AltitudeChanged += DjiClient_AltitudeChanged;
            djiClient.AttitudeChanged += DjiClient_AttitudeChanged;
            djiClient.VelocityChanged += DjiClient_VelocityChanged;
            djiClient.FrameArived += DjiClient_FrameArived;

            djiClient.Initialize();

            //speechSynthesizer.Voice = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices();
            player.AutoPlay = true;

            Task.Run(async () =>
            {
                var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///WinML/Fruit.onnx"));
                mlModel = await FruitWinML.FruitModel.CreateFruitModel(file);
            });
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisepropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (Dispatcher.HasThreadAccess)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                var ignore = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }

        private bool isConnected = false;
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                isConnected = value;
                RaisepropertyChanged();
                RaisepropertyChanged(nameof(ConnectedStatus));
                RaisepropertyChanged(nameof(ControlsVisible));
                GimbleAngle = 0;
            }
        }

        private bool isFlying = false;
        public bool IsFlying
        {
            get => isFlying;
            set
            {
                isFlying = value;
                RaisepropertyChanged();
            }
        }

        public string ConnectedStatus
        {
            get
            {
                if (IsConnected)
                {
                    return "Connected";
                }
                else
                {
                    return "Connecting...";
                }
            }
        }

        private int gimbleAngle = 0;
        private int gimbleDelay = 100;
        DateTime lastGimbleUpdate = DateTime.UtcNow.AddMinutes(-1);
        public int GimbleAngle
        {
            get
            {
                return gimbleAngle;
            }
            set
            {
                gimbleAngle = value;
                RaisepropertyChanged();
                // only send a gimble change every gimbleDelay ms to limit noise
                var diff = (int)(DateTime.UtcNow - lastGimbleUpdate).TotalMilliseconds;
                if (diff > gimbleDelay)
                {
                    lastGimbleUpdate = DateTime.UtcNow;
                    Task.Delay(gimbleDelay).ContinueWith((x) =>
                    {
                        djiClient.SetGimbleAngle(gimbleAngle);
                    });
                }
            }
        }

        private double altitude;

        public double Altitude
        {
            get { return altitude; }
            set
            {
                altitude = value;
                RaisepropertyChanged();
            }
        }

        private double velocity;

        public double Velocity
        {
            get { return velocity; }
            set
            {
                velocity = value;
                RaisepropertyChanged();
            }
        }

        private double heading;

        public double Heading
        {
            get { return heading; }
            set
            {
                heading = value;
                RaisepropertyChanged();
            }
        }

        public Visibility ControlsVisible
        {
            get
            {
                if (IsConnected)
                {
                    return Visibility.Visible;
                }
                return Visibility.Collapsed;
            }
        }


        public string recognizedObject = string.Empty;
        public string RecognizedObject
        {
            get
            {
                return recognizedObject;
            }
            set
            {
                recognizedObject = value;
                RaisepropertyChanged();
            }
        }



        private WriteableBitmap videoSource;
        public WriteableBitmap VideoSource
        {
            get { return videoSource; }
            set
            {
                if (videoSource != value)
                {
                    videoSource = value;
                    RaisepropertyChanged();
                }
            }
        }


        private ImageSource recognitionResultsImage;
        public ImageSource RecognitionResultsImage
        {
            get { return recognitionResultsImage; }
            set
            {
                recognitionResultsImage = value;
                RaisepropertyChanged();
            }
        }


        private void DjiClient_FlyingChanged(bool newValue)
        {
            IsFlying = newValue;
        }

        private void DjiClient_ConnectedChanged(bool newValue)
        {
            IsConnected = newValue;
        }

        private void DjiClient_VelocityChanged(double X, double Y, double Z)
        {
            double airSpeed = X * X + Y * Y + Z * Z;
            airSpeed = Math.Abs(airSpeed) > 0.0001 ? Math.Sqrt(airSpeed) : 0;
            Velocity = airSpeed;
        }

        private void DjiClient_AttitudeChanged(double pitch, double yaw, double roll)
        {
            Heading = yaw;
        }

        private void DjiClient_AltitudeChanged(double newValue)
        {
            Altitude = newValue;
        }

        private async void DjiClient_FrameArived(IBuffer buffer, uint width, uint height, ulong timeStamp)
        {
            if (runProcessTask == null || runProcessTask.IsCompleted)
            {
                runProcessTask = InferenceModelOnFrameData(buffer, timeStamp, width, height);
                // You may want to uncomment next line if want to wait until processing is done
                //runProcessTask.Wait();
            }

            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (VideoSource == null || VideoSource.PixelWidth != width || VideoSource.PixelHeight != height)
                {
                    VideoSource = new WriteableBitmap((int)width, (int)height);
                }

                buffer.CopyTo(VideoSource.PixelBuffer);
                VideoSource.Invalidate();
            });


        }

        private const int cropFrameWidth = 428;
        private const int cropFrameHeight = 320;
        private const int bytesPerPixel = 4;

        private async Task InferenceModelOnFrameData(IBuffer frameBuffer, ulong timeStamp, uint width, uint height, bool doNotDispose = false)
        {
            // Here you can process your frame data 
            // Make sure you use Dispatcher.RunAsync for updating the UI 

            // For the Fruit model, we compensate the lack of detection boxes by "zooming" in the frame to 428x320 

            var frameArray = frameBuffer.ToArray();

            int minX = ((int)width - cropFrameWidth) / 2;
            int minY = ((int)height - cropFrameHeight) / 2;

            var croppedArray = new byte[cropFrameWidth * cropFrameHeight * bytesPerPixel];
            int stride = cropFrameWidth * bytesPerPixel;

            for (int y = minY; y < minY + cropFrameHeight; y++)
            {
                int startIndex = y * (int)width * bytesPerPixel + minX * bytesPerPixel;

                Array.Copy(frameArray, startIndex, croppedArray, (y-minY) * cropFrameWidth * bytesPerPixel, stride);
            }

            // Do not forget to dispose it! 
            SoftwareBitmap bitmap = SoftwareBitmap.CreateCopyFromBuffer(croppedArray.AsBuffer(),
                    BitmapPixelFormat.Bgra8, cropFrameWidth, cropFrameHeight, BitmapAlphaMode.Premultiplied);


            // But we will run evalustion of a WinML model 
            try
            {
                await RunModelOnBitmap(bitmap);
            }
            finally
            {
                if (!doNotDispose)
                {
                    bitmap.Dispose();
                }
            }
        }


        private async Task RunModelOnBitmap(SoftwareBitmap bitmap)
        {
            using (VideoFrame frame = VideoFrame.CreateWithSoftwareBitmap(bitmap))
            {
                FruitWinML.FruitModelInput input = new FruitWinML.FruitModelInput();
                input.data = frame;

                var stopwatch = Stopwatch.StartNew();
                var output = await mlModel.EvaluateAsync(input);
                stopwatch.Stop();

                string newRecognizedObject = " ";

                if (output != null)
                {
                    Debug.WriteLine($"Inference completed with results. {1000f / stopwatch.ElapsedMilliseconds,4:f1} fps.");

                    var mostConfidentResult = output.loss.OrderByDescending(kv => kv.Value).First();
                    string recognizedTag = mostConfidentResult.Key;
                    float recognitionConfidence = mostConfidentResult.Value;

                    // exclude "orange" because of a lot false positives 
                    if (recognitionConfidence > 0.65f && recognizedTag != "orange")
                    {
                        Debug.WriteLine($"Recognized {recognizedTag} with {recognitionConfidence} confidence.");
                        newRecognizedObject = recognizedTag;

                        if (lastAudioTag != recognizedTag || player.Source == null)
                        {
                            

                            lastAudioTag = recognizedTag;
                            var audioStream = await speechSynthesizer.SynthesizeTextToStreamAsync(recognizedTag);
                            player.MediaEnded += Player_MediaEnded;
                            player.Source = MediaSource.CreateFromStream(audioStream, audioStream.ContentType);
                        }                       
                    }
                }

                if (!newRecognizedObject.Equals(RecognizedObject, StringComparison.InvariantCultureIgnoreCase))
                {
                    RecognizedObject = newRecognizedObject.ToUpper();
                }
            }
        }

        private void Player_MediaEnded(MediaPlayer sender, object args)
        {
            player.MediaEnded -= Player_MediaEnded;
            player.Source = null;
        }
    }
}
