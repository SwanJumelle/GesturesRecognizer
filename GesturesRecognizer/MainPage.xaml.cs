using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using GesturesRecognizer.Resources;
using Microsoft.Devices.Sensors;
using Microsoft.Xna.Framework;
using ShakeGestures;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GesturesRecognizer
{
    public partial class MainPage : PhoneApplicationPage
    {
        private SongCollection songCollection;
        private BitmapImage playButton_Image;
        private BitmapImage pauseButton_Image;

        private Boolean shakeEnabled;
        private Boolean detectionStarted;
        Accelerometer accelerometer;

        alglib.clusterizerstate s;
        alglib.kmeansreport rep;

        // Constructeur
        public MainPage()
        {
            InitializeComponent();
            DispatcherTimer XnaDispatchTimer = new DispatcherTimer();
            XnaDispatchTimer.Interval = TimeSpan.FromMilliseconds(50);

            XnaDispatchTimer.Tick += delegate
            {
                try
                {
                    FrameworkDispatcher.Update();
                }
                catch { }
            };

            XnaDispatchTimer.Start();
            this.Loaded += new RoutedEventHandler(MainPage_Loaded);

            playButton_Image = new BitmapImage();
            playButton_Image.SetSource(Application.GetResourceStream(new Uri(@"Assets/MusicPlayer/play.png", UriKind.Relative)).Stream);
            pauseButton_Image = new BitmapImage();
            pauseButton_Image.SetSource(Application.GetResourceStream(new Uri(@"Assets/MusicPlayer/pause.png", UriKind.Relative)).Stream);

            shakeEnabled = false;
            detectionStarted = false;
            this.start_accelero_button.Visibility = System.Windows.Visibility.Collapsed;

            if (Accelerometer.IsSupported)
            {
                // register shake event
                ShakeGesturesHelper.Instance.ShakeGesture += new EventHandler<ShakeGestureEventArgs>(Instance_ShakeGesture);

                // optional, set parameters
                ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 2;

                // start shake detection
                ShakeGesturesHelper.Instance.Active = true;
            }
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Load Music Library
            FrameworkDispatcher.Update();
            MediaLibrary library = new MediaLibrary();

            songCollection = library.Songs;
            MediaPlayer.ActiveSongChanged += new EventHandler<EventArgs>(MediaPlayer_ActiveSongChanged);
            MediaPlayer.MediaStateChanged += new EventHandler<EventArgs>(MediaPlayer_MediaStateChanged);

            
            UpdateCurrentSongInformation();
        }

        private void Instance_ShakeGesture(object sender, ShakeGestureEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                ShakeType CurrentShakeType = e.ShakeType;
                if (CurrentShakeType == ShakeType.Z)
                {
                    playStopBehavior();
                }
                if (CurrentShakeType == ShakeType.X)
                {
                    MediaPlayer.MoveNext();
                }
                if (CurrentShakeType == ShakeType.Y)
                {
                    MediaPlayer.MovePrevious();
                }

            });
        }

        private void UpdateCurrentSongInformation()
        {
            if (MediaPlayer.Queue.Count != 0)
            {
                artistname_textfield.Text = MediaPlayer.Queue.ActiveSong.Artist.Name;
                songname_textfield.Text = MediaPlayer.Queue.ActiveSong.Name;
                cover_image.Opacity = 1.0;
                BitmapImage bmp = new BitmapImage();
                bmp.SetSource(MediaPlayer.Queue.ActiveSong.Album.GetAlbumArt());
                cover_image.Source = bmp;
            }
            else
            {
                artistname_textfield.Text = "Tap the Play button";
                songname_textfield.Text = "And start enhancing your music experience.";
                if (this.songCollection.Count == 0)
                {
                    // Hide the element
                    cover_image.Opacity = 0.0;
                }
            }

        }

        void MediaPlayer_ActiveSongChanged(object sender, EventArgs e)
        {
            UpdateCurrentSongInformation();
        }

        void MediaPlayer_MediaStateChanged(object sender, EventArgs e)
        {
            switch (MediaPlayer.State)
            {
                case MediaState.Stopped:
                    music_play_button.Source = playButton_Image;
                    break;
                case MediaState.Playing:
                    music_play_button.Source = pauseButton_Image;
                    break;
                case MediaState.Paused:
                    music_play_button.Source = playButton_Image;
                    break;
            }
        }

        private void launch_kmeans()
        {
            alglib.clusterizercreate(out s);
            alglib.clusterizersetpoints(s, coords, 2);
            alglib.clusterizersetkmeanslimits(s, 5, 0);
            alglib.clusterizerrunkmeans(s, 2, out rep);
        }

        private void playStopBehavior()
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }
            else if (MediaPlayer.State == MediaState.Paused)
            {
                MediaPlayer.Resume();
            }
            else if (MediaPlayer.State == MediaState.Stopped)
            {
                if (songCollection.Count != 0)
                {
                    MediaPlayer.Play(songCollection);
                }
                else
                {
                    // Hide the element
                    cover_image.Opacity = 0.0;
                }
            }
        }

        private void music_play_button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            playStopBehavior();
        }

        private void music_previous_button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MediaPlayer.MovePrevious();
        }

        private void music_next_button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MediaPlayer.MoveNext();
        }

        private void clusterize()
        {
            this.launch_kmeans();
            "Cluster : {" + Math.Round(rep.c[0, 0], 3) + ", " + 
                Math.Round(rep.c[0, 1], 3) + ", " +
                Math.Round(rep.c[0, 2], 3) +
                "} \nNombre de points : " +
                count_points(0, rep.cidx);
        }

        private int count_points(int clusterindex, int[] points)
        {
            int count = 0;

            for (int i = 0; i < Y.Count; i++) { if (points[i] == clusterindex) count++; }

            return count;
        }

        private void Shake_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button b = (Button)sender;
            if (shakeEnabled)
            {
                b.Content = "Shake: OFF";
                this.start_accelero_button.Visibility = System.Windows.Visibility.Collapsed;
                // start accelerometer detection
                ShakeGesturesHelper.Instance.Active = true;
                // stop shake machine learning detection
                shakeEnabled = !shakeEnabled;
            }
            else
            {
                b.Content = "Shake: ON";
                this.start_accelero_button.Visibility = System.Windows.Visibility.Visible;
                // stop accellerometer detection
                ShakeGesturesHelper.Instance.Active = false;
                // start shake machine learning detection
                clusterize();
                shakeEnabled = !shakeEnabled;
            }
        }

        private void start_accelero_button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (accelerometer == null)
            {
                // Instantiate the Accelerometer.
                accelerometer = new Accelerometer();
                accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                accelerometer.CurrentValueChanged +=
                new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
            }

            if (!detectionStarted)
            {
                // Start detection
                try
                {
                    //statusTextBlock.Text = "starting accelerometer.";
                    accelerometer.Start();
                }
                catch (InvalidOperationException ex)
                {
                }
            }
            else
            {
                // Stop detection
                if (accelerometer != null)
                {
                    accelerometer.Stop();
                }
            }

        }

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            Dispatcher.BeginInvoke(() => getAccValues(e.SensorReading));
        }

        private void getAccValues(AccelerometerReading accelerometerReading)
        {

            Vector3 acceleration = accelerometerReading.Acceleration;
            System.Diagnostics.Debug.WriteLine("X: " + acceleration.X.ToString("0.00"));
            System.Diagnostics.Debug.WriteLine("Y: " + acceleration.Y.ToString("0.00"));
            System.Diagnostics.Debug.WriteLine("Z: " + acceleration.Z.ToString("0.00"));
        }

    }
}