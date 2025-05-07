using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PtixiakiReservations.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PtixiakiReservations.Services
{
    public class ElasticSearchService : IElasticSearch
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticSettings _settings;
        private readonly ILogger<ElasticSearchService> _logger;

        public ElasticSearchService(
            IOptions<ElasticSettings> options,
            ILogger<ElasticSearchService> logger)
        {
            _settings = options.Value;
            _logger = logger;

            try
            {
                // Create a client with error logging
                var settings = new ElasticsearchClientSettings(new Uri(_settings.Uri))
                    .DefaultIndex(_settings.DefaultIndex)
                    .EnableDebugMode()
                    .ServerCertificateValidationCallback(CertificateValidations.AllowAll);

                _client = new ElasticsearchClient(settings);

                _logger.LogInformation("Elasticsearch client created with URL: {Url}", _settings.Uri);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Elasticsearch client");
                throw;
            }
        }

        public async Task<bool> CreateIndexIfNotExistsAsync(string indexName)
        {
            try
            {
                _logger.LogInformation("Checking if index {IndexName} exists", indexName);
                var existsResponse = await _client.Indices.ExistsAsync(indexName);

                if (!existsResponse.Exists)
                {
                    _logger.LogInformation("Creating index {IndexName}", indexName);
                    var createResponse = await _client.Indices.CreateAsync(indexName);

                    if (!createResponse.IsValidResponse)
                    {
                        _logger.LogError("Failed to create index {IndexName}: {ErrorReason}",
                            indexName,
                            createResponse.ElasticsearchServerError?.Error?.Reason);
                    }

                    return createResponse.IsValidResponse;
                }

                _logger.LogInformation("Index {IndexName} already exists", indexName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking/creating index {IndexName}", indexName);
                return false;
            }
        }

        public async Task<bool> AddOrUpdateAsync<T>(T item, string indexName) where T : class
        {
            try
            {
                _logger.LogInformation("Indexing single document to {IndexName}", indexName);
                var response = await _client.IndexAsync(item, i => i.Index(indexName));

                if (!response.IsValidResponse)
                {
                    _logger.LogError("Failed to index document: {ErrorReason}",
                        response.ElasticsearchServerError?.Error?.Reason);
                }

                return response.IsValidResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error indexing document to {IndexName}", indexName);
                return false;
            }
        }

        public async Task<bool> AddOrUpdateBulkAsync<T>(IEnumerable<T> items, string indexName) where T : class
        {
            try
            {
                var itemsList = items.ToList();
                _logger.LogInformation("Bulk indexing {Count} documents to {IndexName}",
                    itemsList.Count,
                    indexName);

                var bulkResponse = await _client.BulkAsync(b => b
                    .Index(indexName)
                    .IndexMany(itemsList));

                if (!bulkResponse.IsValidResponse)
                {
                    _logger.LogError("Bulk indexing failed: {ErrorReason}",
                        bulkResponse.ElasticsearchServerError?.Error?.Reason);

                    if (bulkResponse.Items != null)
                    {
                        foreach (var item in bulkResponse.Items.Where(i => !i.IsValid))
                        {
                            _logger.LogError("Item error: {ErrorReason}", item.Error?.Reason);
                        }
                    }
                }

                return bulkResponse.IsValidResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk indexing to {IndexName}", indexName);
                return false;
            }
        }

        public async Task<T> GetByIdAsync<T>(string id, string indexName) where T : class
        {
            try
            {
                var response = await _client.GetAsync<T>(id, g => g.Index(indexName));
                return response.Source;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting document {Id} from {IndexName}", id, indexName);
                return null;
            }
        }

        public async Task<IEnumerable<T>> SearchAsync<T>(string query, string indexName) where T : class
        {
            try
            {
                _logger.LogInformation("Searching for '{Query}' in {IndexName}", query, indexName);

                // Use MultiMatch query which searches across multiple fields
                var response = await _client.SearchAsync<T>(s => s
                    .Index(indexName)
                    .Query(q => q
                        .MultiMatch(m => m
                                .Fields("*") // Search across all fields
                                .Query(query)
                                .Fuzziness(new Fuzziness(1)) // Allow for typos
                        )
                    )
                );

                if (!response.IsValidResponse)
                {
                    _logger.LogError("Search failed: {ErrorReason}",
                        response.ElasticsearchServerError?.Error?.Reason);
                    return Enumerable.Empty<T>();
                }

                _logger.LogInformation("Found {Count} results", response.Documents.Count);
                return response.Documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for '{Query}' in {IndexName}", query, indexName);
                return Enumerable.Empty<T>();
            }
        }

        public async Task<bool> DeleteByIdAsync<T>(string id, string indexName) where T : class
        {
            try
            {
                var deleteResponse = await _client.DeleteAsync<T>(id, d => d.Index(indexName));
                return deleteResponse.IsValidResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {Id} from {IndexName}", id, indexName);
                return false;
            }
        }
    }
}