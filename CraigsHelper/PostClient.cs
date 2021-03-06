﻿using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraigsHelper
{
    public static class PostClient
    {
        public class ClientResult
        {
            public bool HasError { get; set; }
            public string ErrorMessage { get; set; }
        }

        public class PostResult : ClientResult
        {
            public Post Result { get; set; }
        }

        public class PostListResult : ClientResult
        {
            public List<Post> Result { get; set; }
        }

        ///<summary>Returns a PostResult from a Url</summary>
        ///<param name="url">The Craigslist Url to convert</param>
        public static PostResult GetPost(string url)
        {
            var res = new PostResult { HasError = false };
            var post = new Post { Url = url };
            try
            {
                post.PostID = url.Split('/').Last().Split('.').First();
                HtmlDocument doc = new HtmlDocument();

                var web = new HtmlWeb();
                try
                {
                    if (!url.Contains("https://"))
                        url = "https://" + url;
                    doc = web.Load(url);
                }
                catch (Exception e)
                {
                    throw new Exception("Invalid Url: " + url);
                }

                try
                {
                    post.Title = doc.GetElementbyId("titletextonly").InnerText;
                    post.ImageUrls = new List<string>();
                    IEnumerable<HtmlNode> nodes = doc.DocumentNode.Descendants(0);

                    var prices = nodes.Where(n => n.HasClass("price"));
                    if (prices.Count() > 0)
                        post.Price = decimal.Parse(prices.First().InnerText, NumberStyles.Currency);

                    try
                    {
                        var thumbs = doc.GetElementbyId("thumbs");
                        var links = thumbs.Descendants("a");
                        post.ImageUrls.AddRange(links.SelectMany(x => x.Attributes.Where(a => a.Name == "href")).Select(x => x.Value));
                    }
                    catch
                    {
                        var imgDiv = doc.DocumentNode.SelectNodes("//div[contains(@class, 'slide first visible')]");
                        if (imgDiv != null)
                        {
                            var im = imgDiv.FirstOrDefault()?.Descendants("img");
                            if (im != null)
                            {
                                post.ImageUrls.Add(im.First().Attributes.Single(x => x.Name == "src").Value);
                            }
                        }
                    }

                    var bodyInner = doc.GetElementbyId("postingbody").InnerText.Split('\n');
                    var bodyNodes = bodyInner.Where(x => !string.IsNullOrEmpty(x) && !x.All(char.IsWhiteSpace) && !x.Contains(" QR Code Link to This Post"));

                    post.Body = string.Join("\r\r", bodyNodes.Select(x => x.Replace("show contact info", "[CONTACT INFO REMOVED]")));

                    var times = nodes.Where(x => x.Name == "time");
                    post.PostedDate = DateTime.Parse(times.First().Attributes.Single(x => x.Name == "datetime").Value);
                    if (times.Count() > 2)
                        post.UpdatedDate = DateTime.Parse(times.Last().Attributes.Single(x => x.Name == "datetime").Value);

                    var attributeElements = nodes.Where(x => x.HasClass("attrgroup"));

                    if (attributeElements.Count() > 1)
                    {
                        post.Item = attributeElements.First().Descendants("b").First().InnerText;
                    }

                    post.Attributes = new Dictionary<string, string>();

                    if (attributeElements.Count() > 0)
                    {
                        foreach (var el in attributeElements.Last().Descendants("span"))
                        {
                            var key = el.InnerText.Split(':').First();
                            var val = el.Descendants("b").FirstOrDefault()?.InnerText;
                            post.Attributes.Add(key, val);
                        }
                    }

                    var mapAttr = doc.GetElementbyId("map")?.Attributes;
                    if (mapAttr != null)
                    {
                        post.Location = new Location
                        {
                            Name = doc.DocumentNode.Descendants("small").FirstOrDefault()?.InnerText.Replace("(", "").Replace(")", ""),
                            Address = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'mapaddress')]")?.InnerText,
                            Latitude = double.Parse(mapAttr.Single(x => x.Name == "data-latitude").Value),
                            Longitude = double.Parse(mapAttr.Single(x => x.Name == "data-longitude").Value)
                        };
                    }
                    res.Result = post;
                }

                catch (Exception e)
                {
                    throw new Exception("Url is not a valid Craigslist Posting");
                }
            }
            catch (Exception e)
            {
                res.HasError = true;
                res.ErrorMessage = e.Message;
            }
            return res;
        }

        ///<summary>Returns a PostResult from a Url asynchronously</summary>
        ///<param name="url">The Craigslist Url to convert</param>
        public static async Task<PostResult> GetPostAsync(string url)
        {
            var res = new PostResult();
            await Task.Run(() => { res = GetPost(url); });
            return res;
        }

        ///<summary>Returns a PostListResult from a search Url</summary>
        ///<param name="searchUrl">The Craigslist search Url to convert to a list of Posts</param>
        ///<param name="loadAll">Load all pages of results</param>
        ///<param name="localOnly">Include only local results. Default is true</param>
        public static PostListResult GetPostsFromSearch(string searchUrl, bool loadAll = false, bool localOnly = true)
        {
            var res = new PostListResult { HasError = false, Result = new List<Post>() };

            try
            {
                HtmlDocument doc = new HtmlDocument();
                
                var web = new HtmlWeb();
                var total = 0;
                var loads = 0;
                var loadsLeft = 0;
                var start = 0;
                do
                {
                    var newUrl = "";
                    if (!searchUrl.Contains("https://"))
                        newUrl = "https://" + searchUrl;
                    else
                        newUrl += searchUrl;
                    newUrl += $"?s={start}";
                    try
                    { 
                        doc = web.Load(newUrl);
                        total = Int32.Parse(doc.DocumentNode.SelectSingleNode("//span[contains(@class, 'totalcount')]").InnerText);
                        if (loadAll)
                        {
                            if (loads == 0)
                            {
                                loads = (int)Math.Ceiling((double)total / 120);
                                loadsLeft = loads;
                            }
                            loadsLeft--;
                            start += 120;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception("Invalid Url: " + searchUrl);
                    }

                    try
                    {
                        IEnumerable<HtmlNode> posts = doc.DocumentNode.SelectNodes("//a[contains(@class, 'result-image gallery')]");

                        var urls = posts.SelectMany(x => x.Attributes).Where(x => x.Name == "href").Select(x => x.Value);
                        if (localOnly)
                        {
                            var localId = searchUrl.Split('.').First();
                            urls = urls.Where(x => x.Contains(localId));
                        }
                        foreach (var url in urls)
                        {
                            res.Result.Add(GetPost(url).Result);
                        }
                    }
                    catch
                    {
                        throw new Exception("Invalid Craigslist search Url.");
                    }
                }
                while (loadAll && loadsLeft > 0);
                }
            catch (Exception e)
            {
                res.HasError = true;
                res.ErrorMessage = e.Message;
            }
   

            return res;
        }

        ///<summary>Returns a PostListResult from a search Url asynchronously</summary>
        ///<param name="searchUrl">The Craigslist search Url to convert to a list of Posts</param>
        ///<param name="loadAll">Load all pages of results</param>
        ///<param name="localOnly">Include only local results. Default is true</param>
        public static async Task<PostListResult> GetPostsFromSearchAsync(string searchUrl, bool loadAll, bool localOnly = true)
        {
            var res = new PostListResult();
            await Task.Run(() => { res = GetPostsFromSearch(searchUrl, loadAll, localOnly); });
            return res;
        }
    }
}
