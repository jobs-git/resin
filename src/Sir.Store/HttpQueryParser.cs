﻿using Microsoft.AspNetCore.Http;

namespace Sir.Store
{
    public class HttpQueryParser
    {
        private readonly KeyValueBooleanQueryParser _queryParser;

        public HttpQueryParser(KeyValueBooleanQueryParser queryParser)
        {
            _queryParser = queryParser;
        }

        public Query Parse(string collectionId, HttpRequest request, ITokenizer tokenizer)
        {
            Query query = null;

            string[] fields;

            if (request.Query.ContainsKey("fields"))
            {
                fields = request.Query["fields"].ToArray();
            }
            else
            {
                fields = new[] { "title", "body" };
            }

            string queryFormat = string.Empty;

            if (request.Query.ContainsKey("format"))
            {
                queryFormat = request.Query["format"].ToArray()[0];
            }
            else
            {
                foreach (var field in fields)
                {
                    queryFormat += (field + ":{0}\n");
                }

                queryFormat = queryFormat.Substring(0, queryFormat.Length - 1);
            }

            if (!string.IsNullOrWhiteSpace(request.Query["q"]))
            {
                var expandedQuery = string.Format(queryFormat, request.Query["q"]);

                query = _queryParser.Parse(expandedQuery, tokenizer);
                query.Collection = collectionId.ToHash();

                if (request.Query.ContainsKey("take"))
                    query.Take = int.Parse(request.Query["take"]);
            }

            return query;
        }
    }
}
