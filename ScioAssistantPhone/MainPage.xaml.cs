﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Windows.Phone.Speech.Recognition;
using Windows.Phone.Speech.Synthesis;
using ScioAssistantPhone.Resources;
using ScioAssistantPhone.Serializer;
using RestSharp;
using Newtonsoft.Json;

namespace ScioAssistantPhone
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        public async  void SpeechToText_Click(object sender, RoutedEventArgs e)
        {
            //Speech recognition only supports spanish from spain not from México
            var Language = (from language in InstalledSpeechRecognizers.All
                            where language.Language == "es-ES"
                            select language).FirstOrDefault();

            SpeechRecognizerUI speechRecognition = new SpeechRecognizerUI();

            speechRecognition.Recognizer.SetRecognizer(Language);

            SpeechRecognitionUIResult recoResult = await speechRecognition.RecognizeWithUIAsync();

            if (recoResult.ResultStatus == SpeechRecognitionUIStatus.Succeeded)
            {
                txtPregunta.Text = recoResult.RecognitionResult.Text.Replace(".","");
                LaunchSearch();
            }
        }

        public  void busca_Click(object sender, RoutedEventArgs e)
        {
            LaunchSearch();
        }


        public  void  LaunchSearch()
        {
            results.Children.Clear();
            //launch the query
            var client = new RestClient("http://scioassistant.cloudapp.net");
            client.AddHandler("application/json", new DynamicSerializer());

            client.ExecuteAsync<dynamic>(new RestRequest("home/searchforphone?query=" + txtPregunta.Text), (res) =>
            {
                //MessageBox.Show("result " + res.Content);
                var data = JsonConvert.DeserializeObject<dynamic>(res.Content);
                SpeechSynthesizer synth = new SpeechSynthesizer();
                var spvoice = InstalledVoices.All
                            .Where(voice => voice.Language.Equals("es-ES") & voice.Gender == VoiceGender.Female)
                            .FirstOrDefault();
                
                bool found = true;

                //show the results
                foreach(var e in data)
                {
                    string result = e.ToString();
                    result = result.Replace("{", "").Replace("}", "").Replace("\r\n","").Replace("\"", "");
                    var lines = result.Split(new char[] { ','});
                    result = string.Empty;
                    foreach (var l in lines)
                    {
                        if (l.Contains("no puedo entenderte"))
                        {
                            found = false;
                            spvoice = InstalledVoices.All
                            .Where(voice => voice.Language.Equals("es-ES") & voice.Gender == VoiceGender.Male)
                            .FirstOrDefault();
                            break;
                        }
                        if (!l.Trim().ToLower().StartsWith("id"))
                            result += l + "\r\n";
                        
                    }
                    results.Children.Add(new TextBlock { Text = result });
                }
                synth.SetVoice(spvoice);
                if (found)
                    synth.SpeakTextAsync("Esto es lo que encontré");
                else
                    synth.SpeakTextAsync("Lo siento, no puedo entenderte, intenta de nuevo.");
            });
        }


       

    }
}