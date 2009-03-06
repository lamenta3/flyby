using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace FlyBy
{
	public partial class Window1
	{
        /// <summary>
        /// Public constructor
        /// </summary>
		public Window1()
		{
			this.InitializeComponent();
			
			// Insert code required on object creation below this point.
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
	}
}