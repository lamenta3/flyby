﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using FlickrNet;
using System.Collections.Generic;

namespace FlyBy
{
	public partial class MainWindow
	{
        /// <summary>
        /// Public constructor
        /// </summary>
		public MainWindow()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
            ResetTwitterAccountList();
            ResetFlickrAccountList();
		}

        /// <summary>
        /// Update the text length counter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TweetBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            NumberLabel.Content = (140 - TweetBox.Text.Length).ToString();
            if (TweetBox.Text.Length > 140)
            {
                NumberLabel.Foreground = new SolidColorBrush(Colors.Red);
            }
            else
            {
                NumberLabel.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        /// <summary>
        /// Check if the user has pressed enter; if so, attempt to submit the tweet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TweetBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SubmitTweet();
            }
        }

        /// <summary>
        /// Submit the tweet
        /// </summary>
        /// <returns></returns>
        private bool SubmitTweet()
        {
            if(TweetBox.Text.Length < 140)
            {
                // submit the tweet
                string tweet = TweetBox.Text;
                TweetBox.Text = string.Empty;

                TweetList.Items.Add(tweet);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Copy "RT @author: tweet" to the tweetbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Retweet_Click(object sender, RoutedEventArgs e)
        {
            TweetBox.Text = "RT " + TweetList.SelectedItem.ToString();
        }

        /// <summary>
        /// Copy the text of this tweet to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyTweet_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(System.Windows.DataFormats.Text, TweetList.SelectedItem.ToString());
        }

        /// <summary>
        /// Open this tweet in a browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenTweet_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(TweetList.SelectedItem.ToString());
            }
            catch (Exception)
            {
                // couldn't open it, oh well
            }
        }

        /// <summary>
        /// Show the Flickr username and password fields
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Flickr_NewAccount_Click(object sender, RoutedEventArgs e)
        {
            FlickrUsernameBox.Clear();
            FlickrUsernameLabel.Visibility = Visibility.Visible;
            FlickrUsernameBox.Visibility = Visibility.Visible;

            FlickrPasswordBox.Clear();
            FlickrPasswordLabel.Visibility = Visibility.Visible;
            FlickrPasswordBox.Visibility = Visibility.Visible;

            FlickrNewAccountButton.Visibility = Visibility.Collapsed;
            FlickrAddUserButton.Visibility = Visibility.Visible;

            FlickrOptionsGroupBox.InvalidateVisual();
        }

        private void FlickrUsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FlickrUsernameBox.Text != "")
            {
                FlickrUsernameLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                FlickrUsernameLabel.Visibility = Visibility.Visible;
            }
        }

        private void FlickrAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (FlickrUsernameBox.Text != "" && FlickrPasswordBox.Password != "")
            {
                App.Instance().Options.AddFlickrUser(FlickrUsernameBox.Text, FlickrPasswordBox.Password);

                // update the username box
                ResetFlickrAccountList();

                // reset the controls
                FlickrUsernameBox.Clear();
                FlickrUsernameLabel.Visibility = Visibility.Collapsed;
                FlickrUsernameBox.Visibility = Visibility.Collapsed;

                FlickrPasswordBox.Clear();
                FlickrPasswordLabel.Visibility = Visibility.Collapsed;
                FlickrPasswordBox.Visibility = Visibility.Collapsed;

                FlickrNewAccountButton.Visibility = Visibility.Visible;
                FlickrAddUserButton.Visibility = Visibility.Collapsed;
            }

            FlickrOptionsGroupBox.InvalidateVisual();
        }

        /// <summary>
        /// Populate the list of twitter users
        /// </summary>
        private void ResetTwitterAccountList()
        {
            TwitterAccountList.Items.Clear();
            // fill in the users list
            foreach (KeyValuePair<string, string> userLine in App.Instance().Options.TwitterCredentials)
            {
                TwitterAccountList.Items.Add(userLine.Key);
            }
        }

        /// <summary>
        /// Populate the list of flickr users
        /// </summary>
        private void ResetFlickrAccountList()
        {
            FlickrAccountList.Items.Clear();
            // fill in the users list
            foreach (KeyValuePair<string, string> userLine in App.Instance().Options.FlickrCredentials)
            {
                FlickrAccountList.Items.Add(userLine.Key);
            }
        }

        private void FlickrPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (FlickrPasswordBox.Password != "")
            {
                FlickrPasswordLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                FlickrPasswordLabel.Visibility = Visibility.Visible;
            }
        }

        private void FlickrDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            string username = FlickrAccountList.SelectedItem.ToString().ToLower();

            App.Instance().Options.DeleteFlickrUser(username);

            ResetFlickrAccountList();
        }

        /// <summary>
        /// Show the username and password fields
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Twitter_NewAccount_Click(object sender, RoutedEventArgs e)
        {
            TwitterUsernameBox.Clear();
            TwitterUsernameLabel.Visibility = Visibility.Visible;
            TwitterUsernameBox.Visibility = Visibility.Visible;

            TwitterPasswordBox.Clear();
            TwitterPasswordLabel.Visibility = Visibility.Visible;
            TwitterPasswordBox.Visibility = Visibility.Visible;

            TwitterNewAccountButton.Visibility = Visibility.Collapsed;
            TwitterAddUserButton.Visibility = Visibility.Visible;

            TwitterOptionsGroupBox.InvalidateVisual();
        }

        private void TwitterUsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TwitterUsernameBox.Text != "")
            {
                TwitterUsernameLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                TwitterUsernameLabel.Visibility = Visibility.Visible;
            }
        }

        private void TwitterPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (TwitterPasswordBox.Password != "")
            {
                TwitterPasswordLabel.Visibility = Visibility.Hidden;

                // disable the add button
            }
            else
            {
                TwitterPasswordLabel.Visibility = Visibility.Visible;
            }
        }

        private void TwitterAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (TwitterUsernameBox.Text != "" && TwitterPasswordBox.Password != "")
            {
                App.Instance().Options.AddTwitterUser(TwitterUsernameBox.Text, TwitterPasswordBox.Password);

                // update the username box
                ResetTwitterAccountList();

                // reset the controls
                TwitterUsernameBox.Clear();
                TwitterUsernameLabel.Visibility = Visibility.Collapsed;
                TwitterUsernameBox.Visibility = Visibility.Collapsed;

                TwitterPasswordBox.Clear();
                TwitterPasswordLabel.Visibility = Visibility.Collapsed;
                TwitterPasswordBox.Visibility = Visibility.Collapsed;

                TwitterNewAccountButton.Visibility = Visibility.Visible;
                TwitterAddUserButton.Visibility = Visibility.Collapsed;
            }

            FlickrOptionsGroupBox.InvalidateVisual();
        }

        private void TwitterDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            string username = TwitterAccountList.SelectedItem.ToString().ToLower();

            App.Instance().Options.DeleteTwitterUser(username);

            ResetTwitterAccountList();
        }
	}
}