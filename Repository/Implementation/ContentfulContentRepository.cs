using AutoMapper;
using KT.Content.Data.Definition;
using KT.Content.Data.Exceptions;
using KT.Content.Data.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Contentful.Core;
using Contentful.Core.Search;
using Contentful.Core.Models;
using Contentful.Core.Configuration;
using Newtonsoft.Json;
using System.Text;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace KT.Content.Data.Implementation
{
    /// <summary>
    /// A wrapper for the contentful content delivery API.
    /// </summary>
    public class ContentfulContentRepository : IContentRepository
    {
        private readonly ILogger<ContentfulContentRepository> _logger;
        private readonly HttpClient _httpClient;
        private readonly ContentfulOptions _options;

        public ContentfulOptions Options
        {
            get { return _options; }
        }

        public ContentfulContentRepository(HttpClient httpClient, ILogger<ContentfulContentRepository> logger)
            : this(httpClient, logger, ConfigureOptions())
        {
        }

        public ContentfulContentRepository(HttpClient httpClient, ILogger<ContentfulContentRepository> logger, ContentfulOptions options)
        {
            _logger = logger;
            _httpClient = httpClient;
            _options = options;
        }

        private static void ValidateEnvironmentVariable(string variableName)
        {
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(variableName)))
            {
                throw new InvalidProgramException($"Missing required environment variable: {variableName}");
            }
        }

        public static ContentfulOptions ConfigureOptions() 
        {
            var envVariables = new string [] {"DELIVERY_API_TOKEN", "MGMT_API_TOKEN", "SPACE_ID", "PREVIEW_API_TOKEN", "CONTENTFUL_ENVIRONMENT"};
            foreach (var variableName in envVariables) ValidateEnvironmentVariable(variableName);

            return new ContentfulOptions()
            {
                DeliveryApiKey = Environment.GetEnvironmentVariable("DELIVERY_API_TOKEN"),
                ManagementApiKey = Environment.GetEnvironmentVariable("MGMT_API_TOKEN"),
                SpaceId = Environment.GetEnvironmentVariable("SPACE_ID"),
                PreviewApiKey = Environment.GetEnvironmentVariable("PREVIEW_API_TOKEN"),
                UsePreviewApi = false,
                Environment = Environment.GetEnvironmentVariable("CONTENTFUL_ENVIRONMENT")
            };
        }

        public async Task<Entry<dynamic>> UpdateEntry(dynamic entry)
        {
            _logger.LogTrace($"ContentfulContentRepository.UpdateEntry({entry})");

            if (entry == null) 
            {
                throw new ArgumentException("Missing required parameter", nameof(entry));
            }

            try
            {
                var managementClient = new ContentfulManagementClient(_httpClient, _options);
                return await managementClient.CreateOrUpdateEntry(entry, null, null, entry.SystemProperties.Version);
            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                var msg = "Error updating entry: " + ex;
                _logger.LogError(msg);
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = "Unable to update entry: " + ex;
                _logger.LogError(msg);
                throw new ProcessException(msg, ex);
            }
        }

        public async Task<Contentful.Core.Models.Management.Snapshot> GetLastSnapshot(string id)
        {
            _logger.LogTrace($"ContentfulContentRepository.GetLastSnapshot({id})");

            if (string.IsNullOrWhiteSpace(id)) 
            {
                throw new ArgumentException("Missing required parameter", nameof(id));
            }

            try
            {
                var managementClient = new ContentfulManagementClient(_httpClient, _options);
                var snapshots = await managementClient.GetAllSnapshotsForEntry(id);
                return snapshots.FirstOrDefault();
            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                var msg = "Error creating environment: " + ex;
                _logger.LogError(msg);
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = "Unable to create environment: " + ex;
                _logger.LogError(msg);
                throw new ProcessException(msg, ex);
            }
        }

        public async Task<Project> GetProject(string slug, bool usePreview=false)
        {
            _logger.LogTrace($"ContentfulContentRepository.GetProject({slug}, {usePreview})");

            if (string.IsNullOrWhiteSpace(slug))
                throw new ArgumentException("Missing required parameter", nameof(slug));

            try
            {
                _options.UsePreviewApi = usePreview;
                var cful = new ContentfulClient(_httpClient, _options);
                var facetQuery = new QueryBuilder<Project>()
                                        .ContentTypeIs("project")
                                        .FieldEquals("fields.slug", slug)
                                        .Include(10);
                _logger.LogDebug($"executing CMS call with query: {facetQuery.Build()}");

                // The GetEntry method (/entry endpoint) doesn't support including the referenced
                // targeted content  items. We must use a list/search instead.
                var entrySet = await cful.GetEntries(facetQuery);
                var entryCount = entrySet.Items.Count();

                if (entryCount > 0)
                {
                    // We assume the slug is unique so there should be only one item.
                    // Log so we can notify the content managers of naming conflicts.
                    if (entryCount > 1)
                    {
                        _logger.LogWarning("GetProject({slug}). Multiple pieces of targetable content found with the same slug: ", slug);
                    }

                    var project = entrySet.First();
                    project.IncludedAssets = entrySet.IncludedAssets;
                    project.IncludedEntries = entrySet.IncludedEntries;

                    return project;
                }
                else if (entryCount == 0)
                {
                    _logger.LogWarning("GetProject({slug}). No pieces of targetable content found with the slug: ", slug);
                    return null;
                }
            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                _logger.LogError(ex, "GetHtmlBlock({slug}).", slug);

                var msg = "Error retrieving content: " + ex;
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetHtmlBlock({slug}).", slug);

                var msg = "Unable to retrieve content: " + ex;
                throw new ProcessException(msg, ex);
            }

            return null;
        }

        public async Task<Contentful.Core.Models.Management.ContentfulEnvironment> CreateEnvironment(string id, string name)
        {
            _logger.LogTrace($"ContentfulContentRepository.CreateEnvironment({id}, {name})");

            if (string.IsNullOrWhiteSpace(name)) 
            {
                throw new ArgumentException("Missing required parameter", nameof(name));
            }

            try
            {
                var managementClient = new ContentfulManagementClient(_httpClient, _options);
                var env = await managementClient.CreateOrUpdateEnvironment(id, name);

                return env;
            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                var msg = "Error creating environment: " + ex;
                _logger.LogError(msg);
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = "Unable to create environment: " + ex;
                _logger.LogError(msg);
                throw new ProcessException(msg, ex);
            }
        }

        public async Task<Contentful.Core.Models.Entry<dynamic>> GetItemFromMgmtAPI(string id)
        {
            // note: consider moving mgmt actions into a new repository so we can 
            // avoid giving all requests the mgmt keys & to avoid XxxFromMgmtXxx style
            _logger.LogTrace($"GetItemFromMgmtAPI({id})");
            if (string.IsNullOrWhiteSpace(id)) 
            {
                throw new ArgumentException("Missing required parameter", nameof(id));
            }

            var managementClient = new ContentfulManagementClient(_httpClient, _options);
            Contentful.Core.Models.Entry<dynamic> mgmtEntry = null;

            try
            {
                mgmtEntry = await managementClient.GetEntry(id);
            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                var msg = "Error getting entry from mgmt api: " + ex;
                _logger.LogError(msg);
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = "Unable to get entry from mgmt api: " + ex;
                _logger.LogError(msg);
                throw new ProcessException(msg, ex);
            }

            return mgmtEntry;
        }

        /// <summary>
        /// Publishes the content item with the provided ID
        /// </summary>
        /// <param name="id">The id identifying the item</param>
        /// <returns></returns>
        public async Task<int> PublishItem(string id)
        {
            _logger.LogTrace($"PublishItem({id})");

            if (string.IsNullOrWhiteSpace(id)) 
            {
                throw new ArgumentException("Missing required parameter", nameof(id));
            }

            var managementClient = new ContentfulManagementClient(_httpClient, _options);
            var mgmtEntry = await GetItemFromMgmtAPI(id);

            try
            {
                var latestVersion = mgmtEntry.SystemProperties.Version.GetValueOrDefault();
                var publishedVersion = mgmtEntry.SystemProperties.PublishedVersion.GetValueOrDefault();
                
                // note: this is intended to reduce re-pubishing already published items but
                // a bug in contentful ALWAYS increments the latest version, no matter what.
                // Leaving for future, but should probably ask contentful about it.
                if (publishedVersion < latestVersion)
                {
                    var result = await managementClient.PublishEntry(id, latestVersion);
                    return result.SystemProperties.PublishedVersion.GetValueOrDefault();
                }
                
                return publishedVersion;
            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                var msg = "Error publishing entry: " + ex;
                _logger.LogError(msg);
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = "Unable to publishing entry: " + ex;
                _logger.LogError(msg);
                throw new ProcessException(msg, ex);
            }
        }

        /// <summary>
        /// Publishes the content items referenced by the project.
        /// </summary>
        /// <param name="slug">The slug identifying the project</param>
        /// <returns></returns>
        public async Task<List<string>> PublishProject(string slug)
        {
            _logger.LogTrace($"ContentfulContentRepository.PublishProject({slug})");

            if (string.IsNullOrWhiteSpace(slug)) 
            {
                throw new ArgumentException("Missing required parameter", nameof(slug));
            }

            try
            {
                var cful = new ContentfulClient(_httpClient, _options);
                var managementClient = new ContentfulManagementClient(_httpClient, _options);

                var facetQuery = new QueryBuilder<Project>()
                                        .ContentTypeIs("project")
                                        .FieldEquals("fields.slug", slug)
                                        .Include(8);
                _logger.LogInformation($"executing CMS call with query: {facetQuery.Build()}");
                
                // Retrieve the entire content tree by starting at the project and pulling includes 
                // an arbitrary depth of 8
                var entrySet = await cful.GetEntries(facetQuery);

                // todo: determine if our process will already have the actual project published or not.
                // var projectId = entrySet.Items.FirstOrDefault().Sys.Id;          
                var includedEntryIds = entrySet.IncludedEntries.Select(x => x.SystemProperties.Id);
                // todo: publish assets, too.
                // var includedAssetIds = entrySet.IncludedAssets.Select(x => x.SystemProperties.Id);
                
                foreach (var entry in entrySet.IncludedEntries) 
                {
                    var id = entry.SystemProperties.Id;
                    // Retrieve the item from mgmt API. Version is not included from delivery API so we get it again.
                    var mgmtEntry = await managementClient.GetEntry(id);

                    var latestVersion = mgmtEntry.SystemProperties.Version.GetValueOrDefault();
                    var result = await managementClient.PublishEntry(id, latestVersion);
                }

                return null;

            }
            catch (Contentful.Core.Errors.ContentfulException ex)
            {
                var msg = "Error retrieving content: " + ex;
                _logger.LogError(msg);
                throw new ContentException(msg, ex);
            }
            catch (Exception ex)
            {
                var msg = "Unable to retrieve content: " + ex;
                _logger.LogError(msg);
                throw new ProcessException(msg, ex);
            }
        }
    }
}
