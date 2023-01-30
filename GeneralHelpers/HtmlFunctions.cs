using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MDR_Harvester
{
    public class HtmlHelpers
    {
        //private readonly IMonDataLayer monDataLayer;
        private readonly LoggingHelper _loggingHelper;

        public HtmlHelpers(LoggingHelper loggingHelper)
        {
            _loggingHelper = loggingHelper; 
        }

        
        public async Task CheckURLsAsync(List<ObjectInstance> web_resources)
        {
            HttpClient Client = new HttpClient();
            DateTime today = DateTime.Today;
            foreach (ObjectInstance i in web_resources)
            {
                if (i.resource_type_id == 11)  // just do the study docs for now (pdfs)
                {
                    string? url_to_check = i.url;
                    if (!string.IsNullOrEmpty(url_to_check))
                    {
                        HttpRequestMessage http_request = new HttpRequestMessage(HttpMethod.Head, url_to_check);
                        var result = await Client.SendAsync(http_request);
                        if ((int)result.StatusCode == 200)
                        {
                            i.url_last_checked = today;
                        }
                    }
                }
            }
        }

        public async Task<bool> CheckURLAsync(string url_to_check)
        {
            HttpClient Client = new HttpClient();
            DateTime today = DateTime.Today;
            if (!string.IsNullOrEmpty(url_to_check))
            {
                try
                {
                    HttpRequestMessage http_request = new HttpRequestMessage(HttpMethod.Head, url_to_check);
                    var result = await Client.SendAsync(http_request);
                    return ((int)result.StatusCode == 200);
                }
                catch (Exception e)
                {
                    string message = e.Message;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


    }
}
