// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Text;
using HtmlAgilityPack;
using Newtonsoft.Json;
using RestSharp;

Console.WriteLine("Hello, World!");

const string Base_Url = "https://masothue.com";
const string User_Agent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:107.0) Gecko/20100101 Firefox/107.0";
RestClient CreateRequest()
{
  var clientOpt = new RestClientOptions()
  {
      BaseUrl = new Uri(Base_Url),
      MaxTimeout =  -1
  };
  var clientRequest = new RestClient(clientOpt);
  return clientRequest;
}

(string? token,string? cookie) GetToken()
{
    var fail = (string.Empty, string.Empty);
    var clientRequest = CreateRequest();
    var restRequest = new RestRequest("/Ajax/Token",Method.Post);
    restRequest.AddHeader("user-agent",User_Agent);
    var response = clientRequest.Execute(restRequest);
    if (response.IsSuccessStatusCode)
    {
        var cookie = response?.Cookies?.FirstOrDefault();
        if (cookie == null)
        {
            return fail;
        }

        var cookieValue = $"{cookie.Name}={cookie.Value}";
        var content = response?.Content;
        if (content != null)
        {
            dynamic? data = JsonConvert.DeserializeObject(content);
            if (data?.success == "1")
            {
                return (data?.token,cookieValue);
            }
        }
    }
    return fail;
}

string? GetUrlSearch(string query, string token,string cookie)
{
    var clientRequest = CreateRequest();
    var restRequest = new RestRequest("/Ajax/Search",Method.Post);
    restRequest.AddHeader("user-agent",User_Agent);
    restRequest.AddHeader("Cookie", cookie);
    restRequest.AlwaysMultipartFormData = true;
    restRequest.AddParameter("q",query);
    restRequest.AddParameter("token",token);
    var response = clientRequest.Execute(restRequest);
    if (response.IsSuccessStatusCode)
    {
        var content = response.Content;
        if (content != null)
        {
            dynamic? data = JsonConvert.DeserializeObject(content);
            if (data?.success == "1")
            {
                return data?.url;
            }
        }
    }
    return null; 
}

string? SearchUrlByTax(string tax)
{
    (string? token,string?cookie) = GetToken();
    if (token is not null && cookie is not null)
    {
        var result = GetUrlSearch(tax,token,cookie);
        return result;
    }

    return null;
}

string GetDate(string tax)
{
    var endPoint = SearchUrlByTax(tax);
    var url = new StringBuilder(Base_Url).Append(endPoint).ToString();
    var web = new HtmlWeb();
    web.UserAgent = User_Agent;
    var doc = web.Load(url);
    var htmlNodes = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-taxinfo')]");
    if (htmlNodes.Count >0)
    {
        var table = htmlNodes[0];
        var tHead = table.SelectNodes("//thead/tr/th/span");
        var nameCty = tHead.First().InnerText;
        var rows = table.SelectNodes("//tbody/tr/td");
        IDictionary<string, string> dictionary = new Dictionary<string, string>();
        List<string> lstName = new List<string>();
        List<string> lstValue = new List<string>();
        if (rows.Count > 0)
        {
            foreach (var td in rows)
            {
                var child = td.ChildNodes;
                if (td.Descendants("span").Any() && td.Descendants("i").Any() )
                {
                    var value = td.InnerText.Trim();
                    lstValue.Add(value);
                    continue;
                }
                if (td.Descendants("i").Any())
                {
                    var name = td.InnerText.Trim();
                    dictionary[name] = String.Empty; // lưu giá trị tạm
                    lstName.Add(name);
                    continue;
                }
                if (td.Descendants("span").Any())
                {
                    var value = td.InnerText.Trim();
                    lstValue.Add(value);
                }
                else
                {
                    break;
                }
            }

            if (lstName.Count > 0 && lstValue.Count > 0)
            {
                IEnumerable<(string name, string value)> mapNameValue = lstName.Zip(lstValue);
                foreach (var item in mapNameValue)
                {
                    if (dictionary.ContainsKey(item.name))
                    {
                        dictionary[item.name] = item.value;
                    }
                }
                if (dictionary.Count > 0)
                {
                    return JsonConvert.SerializeObject(dictionary);
                }
            }
        }
    }
    return String.Empty;
}

Console.WriteLine($"Start Crawler {Base_Url}....");
string tax = "8359790628";
var data = GetDate(tax);
Console.WriteLine(data);
Console.WriteLine("finish");