﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Sir.Store
{
    public class RemotePostingsReader
    {
        private IConfigurationService _config;

        public RemotePostingsReader(IConfigurationService config)
        {
            _config = config;
        }

        public async Task<IList<ulong>> Read(string collectionId, long offset)
        {
            var endpoint = string.Format("{0}{1}?id={2}",
                _config.Get("postings_endpoint"), collectionId, offset);

            var request = (HttpWebRequest)WebRequest.Create(endpoint);

            request.Accept = "application/postings";
            request.Method = WebRequestMethods.Http.Get;

            using (var response = (HttpWebResponse) await request.GetResponseAsync())
            {
                var result = new List<ulong>();

                using (var body = response.GetResponseStream())
                {
                    var mem = new MemoryStream();
                    await body.CopyToAsync(mem);   

                    var buf = mem.ToArray();

                    var read = 0;

                    while (read < buf.Length)
                    {
                        result.Add(BitConverter.ToUInt64(buf, read));

                        read += sizeof(ulong);
                    }
                }

                return result;
            }
        }
    }
}
