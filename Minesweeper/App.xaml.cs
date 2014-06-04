using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.IO.IsolatedStorage;
using System.IO;
using System.Xml.Serialization;
using BugSense;
using BugSense.Core.Model;
using Languages.Resources;
using System.Windows.Markup;
using System.Diagnostics;

namespace Minesweeper
{
    public partial class App : Application
    {
        public static string email = "evg.freyman@gmail.com";
        public Data data;

        /// <summary>
        /// Provides easy access to the root frame of the Phone Application.
        /// </summary>
        /// <returns>The root frame of the Phone Application.</returns>
        public PhoneApplicationFrame RootFrame { get; private set; }

        /// <summary>
        /// Provides access to a ContentManager for the application.
        /// </summary>
        public ContentManager Content { get; private set; }

        /// <summary>
        /// Provides access to a GameTimer that is set up to pump the FrameworkDispatcher.
        /// </summary>
        public GameTimer FrameworkDispatcherTimer { get; private set; }

        /// <summary>
        /// Provides access to the AppServiceProvider for the application.
        /// </summary>
        public AppServiceProvider Services { get; private set; }

        NavigatingCancelEventHandler navigatingCancelEventHandler;

        /// <summary>
        /// Constructor for the Application object.
        /// </summary>
        public App()
        {
            BugSenseHandler.Instance.InitAndStartSession(new ExceptionManager(Current), "b4d28dc4");

            // Global handler for uncaught exceptions. 
            UnhandledException += Application_UnhandledException;

            // Standard Silverlight initialization
            InitializeComponent();

            // Phone-specific initialization
            InitializePhoneApplication();

            // XNA initialization
            InitializeXnaApplication();

            // Language display initialization
            InitializeLanguage();

            // Show graphics profiling information while debugging.
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // Display the current frame rate counters.
                Application.Current.Host.Settings.EnableFrameRateCounter = true;

                // Show the areas of the app that are being redrawn in each frame.
                //Application.Current.Host.Settings.EnableRedrawRegions = true;

                // Enable non-production analysis visualization mode, 
                // which shows areas of a page that are handed off to GPU with a colored overlay.
                //Application.Current.Host.Settings.EnableCacheVisualization = true;

                // Disable the application idle detection by setting the UserIdleDetectionMode property of the
                // application's PhoneApplicationService object to Disabled.
                // Caution:- Use this under debug mode only. Application that disables user idle detection will continue to run
                // and consume battery power when the user is not using the phone.
                PhoneApplicationService.Current.UserIdleDetectionMode = IdleDetectionMode.Disabled;
            }            

            navigatingCancelEventHandler = new NavigatingCancelEventHandler(RootFrame_Navigating);
        }

        // Initialize the app's font and flow direction as defined in its localized resource strings.
        //
        // To ensure that the font of your application is aligned with its supported languages and that the
        // FlowDirection for each of those languages follows its traditional direction, ResourceLanguage
        // and ResourceFlowDirection should be initialized in each resx file to match these values with that
        // file's culture. For example:
        //
        // AppResources.es-ES.resx
        //    ResourceLanguage's value should be "es-ES"
        //    ResourceFlowDirection's value should be "LeftToRight"
        //
        // AppResources.ar-SA.resx
        //     ResourceLanguage's value should be "ar-SA"
        //     ResourceFlowDirection's value should be "RightToLeft"
        //
        // For more info on localizing Windows Phone apps see http://go.microsoft.com/fwlink/?LinkId=262072.
        //
        private void InitializeLanguage()
        {
            try
            {
                // Set the font to match the display language defined by the
                // ResourceLanguage resource string for each supported language.
                //
                // Fall back to the font of the neutral language if the Display
                // language of the phone is not supported.
                //
                // If a compiler error is hit then ResourceLanguage is missing from
                // the resource file.
                RootFrame.Language = XmlLanguage.GetLanguage(AppResources.ResourceLanguage);

                // Set the FlowDirection of all elements under the root frame based
                // on the ResourceFlowDirection resource string for each
                // supported language.
                //
                // If a compiler error is hit then ResourceFlowDirection is missing from
                // the resource file.
                FlowDirection flow = (FlowDirection)Enum.Parse(typeof(FlowDirection), AppResources.ResourceFlowDirection, true);
                RootFrame.FlowDirection = flow;
            }
            catch
            {
                // If an exception is caught here it is most likely due to either
                // ResourceLangauge not being correctly set to a supported language
                // code or ResourceFlowDirection is set to a value other than LeftToRight
                // or RightToLeft.

                if (Debugger.IsAttached)
                {
                    Debugger.Break();
                }

                throw;
            }
        }


        private void RootFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            RootFrame.Navigating -= navigatingCancelEventHandler;
            if (e.IsCancelable)
            {
                e.Cancel = true;
                RootFrame.Dispatcher.BeginInvoke(delegate
                {
                    switch (data.page)
                    {
                        case Data.Page.Main:
                            RootFrame.Navigate(new Uri("/MainPage.xaml", UriKind.Relative));
                            break;
                        case Data.Page.Game:
                            RootFrame.Navigate(new Uri("/GamePage.xaml", UriKind.Relative));
                            break;
                        case Data.Page.Congratulation:
                            RootFrame.Navigate(new Uri("/CongratulationPage.xaml", UriKind.Relative));
                            break;
                    }
                });
            }
        }

        // Code to execute when the application is launching (eg, from Start)
        // This code will not execute when the application is reactivated
        private void Application_Launching(object sender, LaunchingEventArgs e)
        {
            FlurryWP8SDK.Api.StartSession("26GPX83SYHNTDD4CDDDP");
            Data.Load(out data);
            RootFrame.Navigating += navigatingCancelEventHandler;
        }

        // Code to execute when the application is activated (brought to foreground)
        // This code will not execute when the application is first launched
        private void Application_Activated(object sender, ActivatedEventArgs e)
        {
            FlurryWP8SDK.Api.StartSession("26GPX83SYHNTDD4CDDDP");
            Data.Load(out data);
            RootFrame.Navigating += navigatingCancelEventHandler;
        }

        // Code to execute when the application is deactivated (sent to background)
        // This code will not execute when the application is closing
        private void Application_Deactivated(object sender, DeactivatedEventArgs e)
        {
            Data.Save(data);
        }

        // Code to execute when the application is closing (eg, user hit Back)
        // This code will not execute when the application is deactivated
        private void Application_Closing(object sender, ClosingEventArgs e)
        {
            Data.Save(data);
        }

        void ShowUnhandledExceptionMessage()
        {
            MessageBox.Show(
                  AppResources.UnhandledMessage_line1 + "\n\n" +
                  AppResources.UnhandledMessage_line2 + "\n\n" +
                  AppResources.UnhandledMessage_line3 + " " + App.email, AppResources.ooops, MessageBoxButton.OK);
        }

        // Code to execute if a navigation fails
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // A navigation has failed; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                ShowUnhandledExceptionMessage();
            }
        }

        // Code to execute on Unhandled Exceptions
        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                System.Diagnostics.Debugger.Break();
            }
            else
            {
                ShowUnhandledExceptionMessage();
            }
        }

        #region Phone application initialization

        // Avoid double-initialization
        private bool phoneApplicationInitialized = false;

        // Do not add any additional code to this method
        private void InitializePhoneApplication()
        {
            if (phoneApplicationInitialized)
                return;

            // Create the frame but don't set it as RootVisual yet; this allows the splash
            // screen to remain active until the application is ready to render.
            RootFrame = new PhoneApplicationFrame();
            RootFrame.Navigated += CompleteInitializePhoneApplication;

            // Handle navigation failures
            RootFrame.NavigationFailed += RootFrame_NavigationFailed;

            // Ensure we don't initialize again
            phoneApplicationInitialized = true;
        }

        // Do not add any additional code to this method
        private void CompleteInitializePhoneApplication(object sender, NavigationEventArgs e)
        {
            // Set the root visual to allow the application to render
            if (RootVisual != RootFrame)
                RootVisual = RootFrame;

            // Remove this handler since it is no longer needed
            RootFrame.Navigated -= CompleteInitializePhoneApplication;
        }

        #endregion

        #region XNA application initialization

        // Performs initialization of the XNA types required for the application.
        private void InitializeXnaApplication()
        {
            // Create the service provider
            Services = new AppServiceProvider();

            // Add the SharedGraphicsDeviceManager to the Services as the IGraphicsDeviceService for the app
            foreach (object obj in ApplicationLifetimeObjects)
            {
                if (obj is IGraphicsDeviceService)
                    Services.AddService(typeof(IGraphicsDeviceService), obj);
            }

            // Create the ContentManager so the application can load precompiled assets
            Content = new ContentManager(Services, "Content");

            // Create a GameTimer to pump the XNA FrameworkDispatcher
            FrameworkDispatcherTimer = new GameTimer();
            FrameworkDispatcherTimer.FrameAction += FrameworkDispatcherFrameAction;
            FrameworkDispatcherTimer.Start();
        }

        // An event handler that pumps the FrameworkDispatcher each frame.
        // FrameworkDispatcher is required for a lot of the XNA events and
        // for certain functionality such as SoundEffect playback.
        private void FrameworkDispatcherFrameAction(object sender, EventArgs e)
        {
            FrameworkDispatcher.Update();
        }

        #endregion
    }  
}