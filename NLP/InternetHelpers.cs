using System;
using System.Collections.Generic;
using Google.Apis.Customsearch.v1;
using Google.Apis.Services;
using HtmlAgilityPack;
using Google.Apis.Customsearch.v1.Data;
using Newtonsoft.Json.Linq;

namespace NLP
{
    public class InternetHelpers
    {
        public CseResource.ListRequest GetUrls(string query) 
        {
			//Store these string on as enviroment variable to keep our secrets safe.
			string apiKey = Environment.GetEnvironmentVariable("GOOGLEAPI");
			string searchEngineId = Environment.GetEnvironmentVariable("CSEID");

            //access the custom search and return the 
			var customSearchService = new CustomsearchService(new BaseClientService.Initializer { ApiKey = apiKey });
			var listRequest = customSearchService.Cse.List(query);
			listRequest.Cx = searchEngineId;

			return listRequest;
        }

        //Method using HAP to select the json versino of the recipe
        public HtmlNodeCollection GetNodes(string url)
        {
			HtmlWeb web = new HtmlWeb();

			var htmlDoc = web.Load(url);

			HtmlNodeCollection nodes = htmlDoc.DocumentNode.SelectNodes("//script[@type=\"application/ld+json\"]");
            return nodes;
		}
    }
}
