using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Ozeki.Media.MediaHandlers;
using Ozeki.VoIP;
using Ozeki.VoIP.SDK;
using pqinet.comm;

namespace QR_APP_Mize.Ba
{
    public partial class TTS_Test : System.Web.UI.Page
    {
        static ISoftPhone softphone;   // softphone object
        static IPhoneLine phoneLine;   // phoneline object
        static IPhoneCall call;
        static MediaConnector connector;
        static PhoneCallAudioSender mediaSender;

        private void Main(string[] args)
        {
            // Create a softphone object with RTP port range 5000-10000
            softphone = SoftPhoneFactory.CreateSoftPhone(5000, 10000);

            var registrationRequired = false;
            var userName = "8504";
            var displayName = "bonggil";
            var authenticationId = "1234";
            var registerPassword = "1234";
            var domainHost = "192.168.0.85";
            var domainPort = 5060;

            var account = new SIPAccount(registrationRequired, displayName, userName, authenticationId, registerPassword, domainHost, domainPort);
            
            RegisterAccount(account);

            mediaSender = new PhoneCallAudioSender();
            connector = new MediaConnector();
            
        }

        private void RegisterAccount(SIPAccount account)
        {
            try
            {
                phoneLine = softphone.CreatePhoneLine(account);
                phoneLine.RegistrationStateChanged += line_RegStateChanged;
                softphone.RegisterPhoneLine(phoneLine);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Error during SIP registration: " + ex);
                throw;
            }
        }

        private void line_RegStateChanged(object sender, RegistrationStateChangedArgs e)
        {
            if (e.State == RegState.NotRegistered || e.State == RegState.Error)
                return;
                //Console.WriteLine("Registration failed!");

            if (e.State == RegState.RegistrationSucceeded)
            {
                //Console.WriteLine("Registration succeeded - Online!");
                CreateCall();
            }
        }

        private void CreateCall()
        {
            var numberToDial = "8504";
            call = softphone.CreateCallObject(phoneLine, numberToDial);
            call.CallStateChanged += call_CallStateChanged;
            call.Start();
        }

        private void SetupTextToSpeech()
        {
            var textToSpeech = new TextToSpeech();
            mediaSender.AttachToCall(call);
            connector.Connect(textToSpeech, mediaSender);
            //여기에 텍스트Area
            textToSpeech.Clear();
            textToSpeech.AddAndStartText((this.tts_txt.Text).ToString());
        }

        private void call_CallStateChanged(object sender, CallStateChangedArgs e)
        {
            Console.WriteLine("Call state: {0}.", e.State);

            if (e.State == CallState.Answered)
            {
                SetupTextToSpeech();
                //connector.Disconnect(textToSpeech,mediaSender);
               // connector.Dispose();
            }
        }
        protected void talk_Click(object sender, EventArgs e)
        {
            string nowCalling = this.Call_status.Value;
            if (nowCalling == "1")
            {
                call.HangUp();
            }
            Main(null);
            this.Call_status.Value = "1";
        }
    }
}