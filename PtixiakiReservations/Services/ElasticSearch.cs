using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Options;
using PtixiakiReservations.Configurations;
using PtixiakiReservations.Models;
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

        public ElasticSearchService(IOptions<ElasticSettings> options)
        {
            _settings = options.Value;
            var settings = new ElasticsearchClientSettings(new Uri(_settings.Url))
                .DefaultIndex(_settings.DefaultIndex);
            _client = new ElasticsearchClient(settings);
        }

        public async Task<bool> CreateIndexIfNotExistsAsync(string indexName)
        {
            var existsResponse = await _client.Indices.ExistsAsync(indexName);
            if (!existsResponse.Exists)
            {
                var createResponse = await _client.Indices.CreateAsync(indexName);
                return createResponse.IsValidResponse;
            }
            return true;
        }

        public async Task<bool> AddOrUpdateAsync<T>(T item, string indexName) where T : class
        {
            var response = await _client.IndexAsync(item, i => i.Index(indexName));
            return response.IsValidResponse;
        }

        public async Task<bool> AddOrUpdateBulkAsync<T>(IEnumerable<T> items, string indexName) where T : class
        {
            var bulkResponse = await _client.BulkAsync(b => b.Index(indexName).IndexMany(items));
            return bulkResponse.IsValidResponse;
        }

        public async Task<T> GetByIdAsync<T>(string id, string indexName) where T : class
        {
            var response = await _client.GetAsync<T>(id, g => g.Index(indexName));
            return response.Source;
        }

        public async Task<IEnumerable<T>> SearchAsync<T>(string query, string indexName) where T : class
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(indexName)
                .Query(q => q.Match(m => m.Field("_all").Query(query)))
            );
            return response.Documents;
        }

        public async Task<bool> DeleteByIdAsync<T>(string id, string indexName) where T : class
        {
            var deleteResponse = await _client.DeleteAsync<T>(id, d => d.Index(indexName));
            return deleteResponse.IsValidResponse;
        }
    }
}