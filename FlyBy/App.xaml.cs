using System;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Permissions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using FlickrNet;
using System.Xml.Serialization;

namespace FlyBy
{
	public partial class App : System.Windows.Application
	{
        /// <summary>
        /// Our public constructor
        /// </summary>
        public App()
            : base()
        {

        }

        /// <summary>
        /// Code to execute on startup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void AppStartup(object sender, StartupEventArgs args)
        {
            // create user option objects
            CreateUserOptions();

            // show the main window
            ShowMainWindow();
        }

        private void ShowMainWindow()
        {
            // show the detail box
            if (mainWindow == null)
            {
                mainWindow = new MainWindow();
            }

            // show the dialog if it isn't already
            mainWindow.Show();

            // bring it to front
            mainWindow.Activate();
        }

        private void CreateUserOptions()
        {
            // deserialize or create a new UserManager
            XmlSerializer deserial = new XmlSerializer(typeof(UserOptions));
            try
            {
                System.IO.TextReader read = new System.IO.StreamReader("options.xml");
                Options = (UserOptions)deserial.Deserialize(read);
                read.Close();
                //App.WriteLine("Deserialized UserManager from users.xml");
            }
            catch (System.IO.FileNotFoundException)
            {
                //App.WriteLine("Couldn't find users.xml; creating new UserManager");
                Options = new UserOptions();
            }
            catch (System.Xml.XmlException)
            {
                //App.WriteLine("An error was encountered while parsing the users.xml file!");
                Options = new UserOptions();
            }
        }

        /// <summary>
        /// Singleton method to access the App-specific functions 
        /// without needing to cast
        /// </summary>
        /// <returns></returns>
        public static App Instance()
        {
            return ((App)App.Current);
        }

        /// <summary>
        /// Handle to the main window
        /// </summary>
        private MainWindow mainWindow;

        /// <summary>
        /// Handle to the user options
        /// </summary>
        public UserOptions Options { get; set; }

        /// <summary>
        /// Flickr connection info
        /// </summary>
        private static string FlickrApiKey = "895c13e7f2cf6c145abdcad5d5003f49";
        private static string FlickrSharedSecret = "1d4b8d4055e40a3c";
        public Flickr F = new Flickr(FlickrApiKey, FlickrSharedSecret);
	}
}
