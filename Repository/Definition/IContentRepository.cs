using KT.Content.Data.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Contentful.Core.Models.Management;
using Contentful.Core.Models;

namespace KT.Content.Data.Definition
{
    public interface IContentRepository
    {
        Task<int> PublishItem(string slug);
        Task<Project> GetProject(string slug, bool preview);
        Task<Snapshot> GetLastSnapshot(string id);
        Task<Entry<dynamic>> GetItemFromMgmtAPI(string id);
        Task<Entry<dynamic>> UpdateEntry(dynamic entry);
    }
}
