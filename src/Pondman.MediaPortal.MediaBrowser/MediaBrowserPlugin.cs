﻿using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Services;
using Pondman.MediaPortal.MediaBrowser.GUI;
using Pondman.MediaPortal.MediaBrowser.Resources.Languages;

namespace Pondman.MediaPortal.MediaBrowser
{
    /// <summary>
    /// The MediaBrowser Plugin for MediaPortal
    /// </summary>
    [PluginIcons("Pondman.MediaPortal.MediaBrowser.Resources.Images.mblogoicon.png", "Pondman.MediaPortal.MediaBrowser.Resources.Images.mblogoicon.png")]
    public class MediaBrowserPlugin : PluginBase
    {
        #region Ctor

        public MediaBrowserPlugin()
            : base((int)MediaBrowserWindow.Main)
        {           
            // check if we have registered the MediaBrowserService
            bool isServiceRegistered = GlobalServiceProvider.IsRegistered<IMediaBrowserService>();
            if (!isServiceRegistered)
            {
                Log.Debug("Registering MediaBrowserService.");

                // if not register it with the global service provider
                IMediaBrowserService service = new MediaBrowserService(this, MediaBrowserPlugin.Log);

                // add service to the global service provider
                GlobalServiceProvider.Add<IMediaBrowserService>(service);

                // trigger discovery so client gets loaded.
                service.Discover();
            }

            // setup property management
            GUIPropertyManager.OnPropertyChanged += GUIPropertyManager_OnPropertyChanged;
            
            // version information
            Version.Publish(MediaBrowserPlugin.DefaultProperty + ".Version");
            VersionName.Publish(MediaBrowserPlugin.DefaultProperty + ".Version.Name");
            GUIUtils.Publish(MediaBrowserPlugin.DefaultProperty + ".Version.String", Version.ToString());

            // Translations
            UI.Publish(DefaultProperty + ".Translation");

            // Settings
            Settings.Publish(DefaultProperty + ".Settings");
        }

        ~MediaBrowserPlugin() 
        {
            GUIPropertyManager.OnPropertyChanged -= GUIPropertyManager_OnPropertyChanged;
        }

        #endregion

        #region Handlers

        void GUIPropertyManager_OnPropertyChanged(string tag, string tagValue)
        {
            if (!Settings.LogProperties || !tag.StartsWith(DefaultProperty))
                return;

            if (tagValue != " ") 
            {
                Log.Debug("SET: \"" + tag + ": \"" + tagValue + "\"");
            } 
            else 
            {
                Log.Debug("UNSET: \"" + tag + "\"");
            }            
        }

        #endregion

        #region Overrides

        public override bool HasSetup()
        {
            return false;
        }

        #endregion        

        #region Core 

        public static readonly string DefaultProperty = "#MediaBrowser";
        public static readonly string DefaultName = "MediaBrowser";

        /// <summary>
        /// Wrapper for log4net
        /// </summary>
        public static readonly ILogger Log = new Log4NetLogger(MediaBrowserPlugin.DefaultName);

        /// <summary>
        /// Translations
        /// </summary>
        public static readonly i18n<Translations> UI = new i18n<Translations>(MediaBrowserPlugin.DefaultName, Log);

        /// <summary>
        /// Settings
        /// </summary>
        public static readonly MediaBrowserSettings Settings = new MediaBrowserSettings();

        #endregion

    }
}
