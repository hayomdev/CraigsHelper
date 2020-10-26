using System;
using System.Collections.Generic;

namespace CraigsHelper
{
    public class Post
    {
        public string PostID { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Item { get; set; }
        public decimal Price { get; set; }
        public List<string> ImageUrls { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Attributes { get; set; }
        public Location Location { get; set; }
        public DateTime PostedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }

    public class Location
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}
