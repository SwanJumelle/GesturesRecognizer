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
        private int counter = 0;
        private double[,] coords;
        private Boolean isOK;
        private int clusterOuCaBougePas;
        alglib.clusterizerstate s;
        alglib.kmeansreport rep;
        private int clusterOuCaBouge;

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
            isOK = false;
            coords = new double[,] { {0.0f,0.0f} };

            launch_kmeans(InitClustering.init());

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

        private void launch_kmeans(double[,] elcoords)
        {
            alglib.clusterizercreate(out s);
            alglib.clusterizersetpoints(s, elcoords, 2);
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
                    //cover_image.Opacity = 0.0;
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
                shakeEnabled = !shakeEnabled;
            }
        }

        private void start_accelero_button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            
            if (accelerometer == null)
            {
                // Instantiate the Accelerometer.
                accelerometer = new Accelerometer();
                accelerometer.TimeBetweenUpdates = TimeSpan.FromMilliseconds(100);
                accelerometer.CurrentValueChanged +=
                new EventHandler<SensorReadingEventArgs<AccelerometerReading>>(accelerometer_CurrentValueChanged);
            }
            if (!detectionStarted)
            {
                // Start detection
                try
                {
                    accelerometer.Start();
                    Button b = (Button)sender;
                    b.Content = "Stop";
                    b.IsEnabled = false;
                    detectionStarted = !detectionStarted;
                }
                catch (InvalidOperationException ex)
                {
                    System.Diagnostics.Debug.WriteLine("Accelerator not supported");
                }
            }
        }

        void accelerometer_CurrentValueChanged(object sender, SensorReadingEventArgs<AccelerometerReading> e)
        {
            Vector3 acceleration = e.SensorReading.Acceleration;
            coords[0,0] += Math.Abs(acceleration.X);
            coords[0,1] += Math.Abs(acceleration.Y);


            counter ++;
            if (counter == 20)
            {
                // AVG
                coords[0,0] /= 20;
                coords[0,1] /= 20;

                
                System.Diagnostics.Debug.WriteLine("{"+ coords[0,0] + "," + coords[0,1] + "}," );
                //System.Diagnostics.Debug.WriteLine("NBPOINTS: " + rep.npoints + " NBFEATURES: " + rep.nfeatures);
                //alglib.clusterizergetdistances(coords, rep.npoints, rep.nfeatures, 2, out result);
                //System.Diagnostics.Debug.WriteLine("RESULT: " + result);

                if (!isOK)
                {

                    double distanceA = Math.Sqrt((Math.Pow(rep.c[0, 0] - coords[0, 0], 2) + Math.Pow(rep.c[0, 1] - coords[0, 1], 2)));
                    double distanceB = Math.Sqrt((Math.Pow(rep.c[1, 0] - coords[0, 0], 2) + Math.Pow(rep.c[1, 1] - coords[0, 1], 2)));

                    if (distanceA > distanceB)
                    {
                        clusterOuCaBougePas = 1;
                        clusterOuCaBouge = 0;
                    }
                    else
                    {
                        clusterOuCaBougePas = 0;
                        clusterOuCaBouge = 1;
                    }
                    isOK = !isOK;
                }
                else
                {
                    double distanceCaBouge = Math.Sqrt((Math.Pow(rep.c[clusterOuCaBouge, 0] - coords[0, 0], 2) + Math.Pow(rep.c[clusterOuCaBouge, 1] - coords[0, 1], 2)));
                    double distanceCaBougePas = Math.Sqrt((Math.Pow(rep.c[clusterOuCaBougePas, 0] - coords[0, 0], 2) + Math.Pow(rep.c[clusterOuCaBougePas, 1] - coords[0, 1], 2)));
                    if (distanceCaBouge < distanceCaBougePas)
                    {
                        System.Diagnostics.Debug.WriteLine("SHAKE IT BABY!");
                        playStopBehavior();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("YOU DON'T MOVE, BITCH!");
                    }
                }


                counter = 0;
                Dispatcher.BeginInvoke(() => UpdateUI());
                detectionStarted = !detectionStarted;

                
                coords[0, 0] = 0.0f;
                coords[0, 1] = 0.0f;
            }
        }

        private void UpdateUI()
        {
            start_accelero_button.Content = "Start";
            start_accelero_button.IsEnabled = true;
        }
    }
}