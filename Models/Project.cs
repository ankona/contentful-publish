using System.Collections.Generic;
using Contentful.Core.Models;

namespace KT.Content.Data.Models
{
    public interface IContentItem
    {
        SystemProperties Sys { get; set; }
    }
    public class ContentItem : IContentItem
    {
        public SystemProperties Sys { get; set; }
    }

    /// <summary>
    /// A collection of related content items that will 
    /// be released together.
    /// </summary>
    public class Project
    {
        public SystemProperties Sys { get; set; }
        public string Slug { get; set; }
        public IEnumerable<Contentful.Core.Models.Entry<dynamic>> IncludedEntries { get; set; }
        public IEnumerable<Contentful.Core.Models.Asset> IncludedAssets { get; set; }
        public IEnumerable<Contentful.Core.Models.Entry<dynamic>> Items { get; set; }
    }
}
