﻿using MediaPortal.GUI.Library;
using MediaPortal.Player;
using System;
using System.Threading;

namespace Pondman.MediaPortal
{
    public enum MediaPlayerState 
    { 
        Idle, 
        Processing, 
        Playing 
    }
    
    public class MediaPlayer
    {
        #region Variables
        
        protected GUIWindow _window;
        protected ILogger _logger;
        protected MediaPlayerState _state;
        protected int _resumeTime = 0;

        #endregion

        public event Action<MediaPlayerInfo> PlayerStarted;

        #region Ctor

        public MediaPlayer(GUIWindow window, ILogger logger = null)
        {
            Guard.NotNull(() => window, window);
            _logger = logger ?? NullLogger.Instance;
            _state = MediaPlayerState.Idle;
            
            // hookup internal playback handlers
            g_Player.PlayBackStarted += new g_Player.StartedHandler(OnPlaybackStarted);
            g_Player.PlayBackEnded += new g_Player.EndedHandler(OnPlayBackEnded);
            g_Player.PlayBackStopped += new g_Player.StoppedHandler(OnPlayBackStoppedOrChanged);
            g_Player.PlayBackChanged += new g_Player.ChangedHandler(OnPlayBackStoppedOrChanged);
        }

        ~MediaPlayer()
        {
            // unhook internal playback handlers
            g_Player.PlayBackStarted -= new g_Player.StartedHandler(OnPlaybackStarted);
            g_Player.PlayBackEnded -= new g_Player.EndedHandler(OnPlayBackEnded);
            g_Player.PlayBackStopped -= new g_Player.StoppedHandler(OnPlayBackStoppedOrChanged);
            g_Player.PlayBackChanged -= new g_Player.ChangedHandler(OnPlayBackStoppedOrChanged);
        }

        #endregion

        #region Protected Properties

        protected ILogger Log
        {
            get
            {
                return _logger;
            }
        }

        #endregion

        #region Internal Event Handlers

        protected void OnPlaybackStarted(g_Player.MediaType type, string filename)
        {
            if (!IsPlaying)
                return;

            g_Player.ShowFullScreenWindow();
            _state = MediaPlayerState.Playing;
            
            if (_resumeTime > 0) 
            {
                SeekPosition(_resumeTime);
            }

            MediaPlayerInfo info = new MediaPlayerInfo(filename);
            if (PlayerStarted != null)
            {
                PlayerStarted(info);
                UpdateOSD(info);
            }
        }

        protected void OnPlayBackStoppedOrChanged(g_Player.MediaType type, int timeMovieStopped, string filename)
        {
            if (IsPlaying)
            {
                Reset();
            }
        }

        protected void OnPlayBackEnded(g_Player.MediaType type, string filename)
        {
            if (IsPlaying)
            {
                Reset();
            }
        }

        #endregion

        /// <summary>
        /// Plays the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="resumeTimeInSeconds">The resume time in seconds.</param>
        public virtual void Play(string path, int resumeTimeInSeconds = 0)
        {
            _state = MediaPlayerState.Processing;
            _resumeTime = resumeTimeInSeconds;
            
            // Play the file using the mediaportal player
            _logger.Debug("Play: Path={0}, ResumeFrom={1}", path, resumeTimeInSeconds);           
            
            bool success = g_Player.Play(path.Trim(), g_Player.MediaType.Video);

            // if the playback started and we are still playing go full screen (internal player)
            if (!success)
            {
                _logger.Error("Playback failed: Media={0}", path);
                Reset();
            }
        }

        public virtual void Stop()
        {
            if (g_Player.Playing)
            {
                g_Player.Stop();
            }
        }

        public MediaPlayerState State
        {
            get
            {
                return _state;
            }
        }
        
        public bool IsPlaying
        {
            get
            {
                return (_state != MediaPlayerState.Idle);
            }
        }

        protected void Reset() 
        {
            _state = MediaPlayerState.Idle;
        }

        protected void UpdateOSD(MediaPlayerInfo info)
        {
            // todo: listen to property set event?
            Timer delayed = new Timer((x) => info.Publish("#Play.Current"), null, 2000, -1);
        }

        static void SeekPosition(int resumePositionInSeconds)
        {
            GUIMessage msg = new GUIMessage(GUIMessage.MessageType.GUI_MSG_SEEK_POSITION, 0, 0, 0, 0, 0, null);
            msg.Param1 = resumePositionInSeconds;
            GUIGraphicsContext.SendMessage(msg);
        }
    }
}
