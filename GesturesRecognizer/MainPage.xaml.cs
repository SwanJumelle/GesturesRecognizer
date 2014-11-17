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


        private void Shake_Button_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Button b = (Button)sender;
            if (shakeEnabled)
            {
                b.Content = "Shake: OFF";
                // start accelerometer detection
                ShakeGesturesHelper.Instance.Active = true;
                // stop shake machine learning detection
                shakeEnabled = !shakeEnabled;
            }
            else
            {
                b.Content = "Shake: ON";
                // stop accellerometer detection
                ShakeGesturesHelper.Instance.Active = false;
                // start shake machine learning detection
                shakeEnabled = !shakeEnabled;
            }
        }

    }
}