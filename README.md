# CraigsHelper
A simple package to help get craigslist sale postings into a C# model.

# Useage
The current functionality of this package is to retrieve Craigslist posts into useable models.

The static class PostClient has methods for getting a Post from a url and a list of Posts from a search url.
There are asynchronous methods for these as well.

# Post Class:

    public class Post
    {
        public string PostID { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Item { get; set; }
        public decimal Price { get; set; }
        public List<string> ImageUrls { get; set; }
        public string Body { get; set; }
        public Dictionary<string,string> Attributes { get; set; }
        public  Location Location { get; set; }
    }

    public class Location
    {
        public string Name { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
