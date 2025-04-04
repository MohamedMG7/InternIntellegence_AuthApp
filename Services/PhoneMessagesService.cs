using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AuthApp.Services{
    public class PhoneMessagesService{

        string Sid = "Your Twilio SID Here";
        string token = "Your Twoilio Token Here";
        public async Task<bool> SendSMSAsync(string phoneNumber,string Message){
            TwilioClient.Init(Sid,token);
            
            var To = new PhoneNumber(phoneNumber);
            var From = new PhoneNumber("You Twilio Phone Number"); // Twilio Phone Number
            await MessageResource.CreateAsync(to:To,from:From,body:$"Code {Message}");

            return true;
        }       
    }
}