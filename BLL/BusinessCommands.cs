using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRegistration.BLL
{
    class BusinessCommands
    {
        private static readonly string REST_URL = "https://vregistration.the-v.net/api.aspx?action=";

        public static string CheckLogin(string username, string password)
        {
            string action = "CheckLogin";
            var client = new RestClient(REST_URL + action);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("action", action);
            dictionary.Add("username", username);
            dictionary.Add("password", password);

            // Should I use Tuples or 'out' parameters?
            Tuple<string, string, string> paramTuple = GetMultipartFormData(dictionary);
            request.AddHeader("content-type", String.Format("multipart/form-data; boundary=----{0}", paramTuple.Item3));

            request.AddParameter(paramTuple.Item1, paramTuple.Item2, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            return response.Content.ToString();
        }

        public static string CheckInWizard(string ticketno,  string bookingid, string irid, string behalf, string remark, string registrar)
        {
            string action = "CheckInWizard";
            var client = new RestClient(REST_URL + action);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("action", action);
            dictionary.Add("ticketno", ticketno);
            dictionary.Add("wristband", "1");
            dictionary.Add("bookingid", bookingid);
            dictionary.Add("irid", irid);
            dictionary.Add("behalf", behalf);
            dictionary.Add("remark", remark);
            dictionary.Add("registrar", registrar);

            // Should I use Tuples or 'out' parameters?
            Tuple<string, string, string> paramTuple = GetMultipartFormData(dictionary);
            request.AddHeader("content-type", String.Format("multipart/form-data; boundary=----{0}", paramTuple.Item3));

            request.AddParameter(paramTuple.Item1, paramTuple.Item2, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            return response.Content.ToString();
        }

        public static string PayBooking(string ir, string currency, string payment, string reference, string behalf, string barcode, string feeid, string eventid, string time, string registrar)
        {
            string action = "PayBooking";
            var client = new RestClient(REST_URL + action);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("action", action);
            dictionary.Add("ir", ir);
            dictionary.Add("currency", currency);
            dictionary.Add("payment", payment);
            dictionary.Add("reference", reference);
            dictionary.Add("behalf", behalf);
            dictionary.Add("barcode", barcode);
            dictionary.Add("feeid", feeid);
            dictionary.Add("eventid", eventid);
            dictionary.Add("time", time);
            dictionary.Add("registrar", registrar);


            Tuple<string, string, string> paramTuple = GetMultipartFormData(dictionary);
            request.AddHeader("content-type", String.Format("multipart/form-data; boundary=----{0}", paramTuple.Item3));

            request.AddParameter(paramTuple.Item1, paramTuple.Item2, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            return response.Content.ToString();
        }

        public static string getPastBookings(string ir)
        {
            string action = "getPastBooking";
            var client = new RestClient(REST_URL + action);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("action", action);
            dictionary.Add("ir", ir);


            Tuple<string, string, string> paramTuple = GetMultipartFormData(dictionary);
            request.AddHeader("content-type", String.Format("multipart/form-data; boundary=----{0}", paramTuple.Item3));

            request.AddParameter(paramTuple.Item1, paramTuple.Item2, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            return response.Content.ToString();
        }

        public static string getFeeList()
        {
            string action = "getFeeList";
            var client = new RestClient(REST_URL + action);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("action", action);


            Tuple<string, string, string> paramTuple = GetMultipartFormData(dictionary);
            request.AddHeader("content-type", String.Format("multipart/form-data; boundary=----{0}", paramTuple.Item3));

            request.AddParameter(paramTuple.Item1, paramTuple.Item2, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            return response.Content.ToString();
        }
        public static string getExchangeList()
        {
            string action = "getExchangeList";
            var client = new RestClient(REST_URL + action);
            var request = new RestRequest(Method.POST);
            request.AddHeader("cache-control", "no-cache");

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("action", action);


            Tuple<string, string, string> paramTuple = GetMultipartFormData(dictionary);
            request.AddHeader("content-type", String.Format("multipart/form-data; boundary=----{0}", paramTuple.Item3));

            request.AddParameter(paramTuple.Item1, paramTuple.Item2, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            return response.Content.ToString();
        }
        public static string getIR(string ir)
        {
            //Show User details from webservice
            var client = new RestClient("https://vregistration.the-v.net/verifyir.aspx?irid=" + ir);
            var request = new RestRequest(Method.GET);
            request.AddHeader("postman-token", "32deda11-774c-5a78-c008-e775dd0906fc");
            request.AddHeader("cache-control", "no-cache");
            IRestResponse response = client.Execute(request);

            return response.Content.ToString();
        }
        #region Helpers
        private static Tuple<string, string, string> GetMultipartFormData(Dictionary<string, string> postParameters)
        {
            string FormData = "";
            string webkitID = String.Format("WebKitFormBoundary{0:N}", Guid.NewGuid());

            // Note: this makes sure that the length of the NewGuid is 15 characters,
            // which seems to be a requirement since the original code has a 15
            // character 'code' and works while the NewGuid has more and the response
            // returns an error. More work needs to be done here
            webkitID = webkitID.Substring(0, webkitID.Length - 17);

            string formDataBoundary = String.Format("------{0}", webkitID);
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;
            FormData += formDataBoundary + "\r\n";

            foreach (var param in postParameters)
            {
                string postData = String.Format("Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n{2}\r\n",
                    param.Key,
                    param.Value,
                    formDataBoundary);
                FormData += postData;
            }

            FormData = FormData.Substring(0, FormData.Length - 2) + "--";
            return Tuple.Create(contentType, FormData, webkitID);
        }
        
        #endregion
     }
}
