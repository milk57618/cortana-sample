﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using Windows.ApplicationModel.VoiceCommands;
using Windows.Media.SpeechSynthesis;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Media;
using ApiAiSDK;
using ApiAiSDK.Model;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApiAiDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private SpeechSynthesizer speechSynthesizer;

        private AIService AIService => (Application.Current as App)?.AIService;

        public MainPage()
        {
            InitializeComponent();
            
            speechSynthesizer = new SpeechSynthesizer();
        
            mediaElement.MediaEnded += MediaElement_MediaEnded;
            
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("MediaElement_MediaEnded");
            Listen_Click(listenButton, null);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var appView = ApplicationView.GetForCurrentView();
            var titleBar = appView.TitleBar;
            titleBar.BackgroundColor = Color.FromArgb(255, 43, 48, 62);
            titleBar.InactiveBackgroundColor = Color.FromArgb(255, 43, 48, 62);
            titleBar.ButtonBackgroundColor = Color.FromArgb(255, 43, 48, 62);
            titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(255, 43, 48, 62);
            titleBar.ForegroundColor = Color.FromArgb(255, 247, 255, 255);
            

            if (e.Parameter != null)
            {
                var param = Convert.ToString(e.Parameter);
                if (!string.IsNullOrEmpty(param))
                {
                    TryLoadAiResponse(param);
                }
                
            }

            InitializeRecognizer();

            AIService.OnResult += AIService_OnResult;
            AIService.OnError += AIService_OnError;
        }

        private void AIService_OnError(AIServiceException ex)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.High, () => resultTextBlock.Text = ex.ToString());
        }

        private async void AIService_OnResult(AIResponse aiResponse)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.High, ()=> OutputJson(aiResponse));
            Dispatcher.RunAsync(CoreDispatcherPriority.High, () => OutputParams(aiResponse));

            var speechText = aiResponse.Result?.Fulfillment?.Speech;
            if (!string.IsNullOrEmpty(speechText))
            {
                var speechStream = await speechSynthesizer.SynthesizeTextToStreamAsync(speechText);
                mediaElement.SetSource(speechStream, speechStream.ContentType);
                mediaElement.Play();
            }
        }

        private void TryLoadAiResponse(string s)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<AIResponse>(s);
                OutputJson(response);
                OutputParams(response);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
        
        private async Task InitializeRecognizer()
        {
            await AIService.InitializeAsync();
            listenButton.IsEnabled = true;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            AIService.OnResult -= AIService_OnResult;
            AIService.OnError -= AIService_OnError;
        }

        private async void Listen_Click(object sender, RoutedEventArgs e)
        {

            if(mediaElement.CurrentState == MediaElementState.Playing)
            {
                mediaElement.Stop();
            }
            
            try
            {
                await AIService.StartRecognitionAsync();
                listenButton.Content = "Listen";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                resultTextBlock.Text = "Empty or error result";
                
                listenButton.Content = "Listen";
            }
            
        }

        private void OutputParams(AIResponse aiResponse)
        {
            var contextsParams = new Dictionary<string,string>();

            if (aiResponse.Result?.Contexts != null)
            {
                foreach (var context in aiResponse.Result?.Contexts)
                {
                    if (context.Parameters != null)
                    {
                        foreach (var parameter in context.Parameters)
                        {
                            if (!contextsParams.ContainsKey(parameter.Key))
                            {
                                contextsParams.Add(parameter.Key, parameter.Value);
                            }
                        }
                    }
                }
            }

            var resultBuilder = new StringBuilder();
            foreach (var contextsParam in contextsParams)
            {
                resultBuilder.AppendLine(contextsParam.Key + ": " + contextsParam.Value);
            }

            parametersTextBlock.Text = resultBuilder.ToString();
        }

        private void OutputJson(AIResponse aiResponse)
        {
            resultTextBlock.Text = JsonConvert.SerializeObject(aiResponse, Formatting.Indented);
        }

        private async void InstallVoiceCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///VoiceCommands.xml"));
                await AIService.InstallVoiceCommands(storageFile);

                parametersTextBlock.Text = "Voice commands installed";
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                resultTextBlock.Text = ex.ToString();
            }
        }

        private async void UninstallCommands_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///UninstallCommands.xml"));
                await AIService.InstallVoiceCommands(storageFile);

                parametersTextBlock.Text = "Voice commands uninstalled";

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                resultTextBlock.Text = ex.ToString();
            }
        }

        private string RestoreSessionId()
        {
            var roamingSettings = ApplicationData.Current.LocalSettings;
            if (roamingSettings.Values.ContainsKey("SessionId"))
            {
                var sessionId = Convert.ToString(roamingSettings.Values["SessionId"]);
                return sessionId;
            }
            return string.Empty;
        }

        private void JsonButton_Click(object sender, RoutedEventArgs e)
        {
            jsonContaner.Visibility = jsonContaner.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
            
        }
    }
}
