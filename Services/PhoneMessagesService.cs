using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AuthApp.Services{
    public class PhoneMessagesService{

        string Sid = "Twoilio SID Here";
        string token = "Your Token Here";
        public async Task<bool> SendSMSAsync(string phoneNumber,string Message){
            TwilioClient.Init(Sid,token);
            
            var To = new PhoneNumber(phoneNumber);
            var From = new PhoneNumber("Twilio Phone Number Here"); // Twilio Phone Number
            await MessageResource.CreateAsync(to:To,from:From,body:$"Code {Message}");

            return true;
        }       
    }
}