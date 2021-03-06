﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Sir.Postings
{
    public class StreamRepository
    {
        private readonly IConfigurationService _config;

        private IDictionary<ulong, IDictionary<long, IList<(long, long)>>> _index { get; set; }

        private const string DataFileNameFormat = "{0}.pos";
        private const string IndexFileName = "_.pix";

        public StreamRepository(IConfigurationService config)
        {
            _config = config;

            var ixfn = Path.Combine(_config.Get("data_dir"), IndexFileName);

            if (File.Exists(ixfn))
            {
                _index = ReadIndex(ixfn);
            }
            else
            {
                _index = new Dictionary<ulong, IDictionary<long, IList<(long, long)>>>();
            }
        }

        private IDictionary<ulong, IDictionary<long, IList<(long, long)>>> ReadIndex(string fileName)
        {
            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None, 4096))
            {
                var formatter = new BinaryFormatter();
                return (IDictionary<ulong, IDictionary<long, IList<(long, long)>>>)formatter.Deserialize(fs);
            }
        }

        private void FlushIndex()
        {
            var fileName = Path.Combine(_config.Get("data_dir"), IndexFileName);

            Task.Run(() =>
            {
                using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(fs, _index);
                }
            });
        }

        public async Task<MemoryStream> Read(ulong collectionId, long id)
        {
            var collectionIndex = GetIndex(collectionId);
            var result = new MemoryStream();

            IList<(long, long)> ix;

            if (!collectionIndex.TryGetValue(id, out ix))
            {
                return result;
            }

            var fileName = Path.Combine(_config.Get("data_dir"), string.Format(DataFileNameFormat, collectionId));

            using (var file = new FileStream(
                fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true))
            {
                foreach (var loc in ix)
                {
                    var pos = loc.Item1;
                    var len = loc.Item2;

                    file.Seek(pos, SeekOrigin.Begin);

                    var buf = new byte[len];
                    var read = await file.ReadAsync(buf);

                    if (read != len)
                    {
                        throw new InvalidDataException();
                    }

                    await result.WriteAsync(buf);
                }
            }

            return result;
        }

        public async Task<MemoryStream> Write(ulong collectionId, byte[] messageBuf)
        {
            var collectionIndex = GetIndex(collectionId);
            int read = 0;

            // read first word of payload
            var payloadCount = BitConverter.ToInt32(messageBuf, 0);
            read = sizeof(int);

            // read lengths
            var lengths = new List<int>(payloadCount);

            for (int index = 0; index < payloadCount; index++)
            {
                lengths.Add(BitConverter.ToInt32(messageBuf, read));
                read += sizeof(int);
            }

            // read lists
            var lists = new List<byte[]>(payloadCount);

            for (int index = 0; index < lengths.Count; index++)
            {
                var size = lengths[index];
                var buf = new byte[size];
                Buffer.BlockCopy(messageBuf, read, buf, 0, size);
                read += size;
                lists.Add(buf);
            }

            if (lists.Count != payloadCount)
            {
                throw new DataMisalignedException();
            }

            var positions = new List<long>(payloadCount);

            var fileName = Path.Combine(_config.Get("data_dir"), string.Format(DataFileNameFormat, collectionId));

            using (var file = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true))
            {
                for (int index = 0; index < lists.Count; index++)
                {
                    long pos = file.Position;
                    var word = lists[index];

                    await file.WriteAsync(word);

                    long len = file.Position - pos;

                    if (len != lengths[index])
                    {
                        throw new DataMisalignedException();
                    }

                    IList<(long, long)> ix;

                    if (!collectionIndex.TryGetValue(pos, out ix))
                    {
                        ix = new List<(long, long)>();

                        collectionIndex.Add(pos, ix);
                    }

                    ix.Add((pos, len));
                    positions.Add(pos);
                }
            }

            if (positions.Count != payloadCount)
            {
                throw new DataMisalignedException();
            }

            var res = new MemoryStream();

            for (int i = 0; i < positions.Count; i++)
            {
                await res.WriteAsync(BitConverter.GetBytes(positions[i]));
            }

            res.Position = 0;

            FlushIndex();

            return res;
        }


        private IDictionary<long, IList<(long, long)>> GetIndex(ulong collectionId)
        {
            IDictionary<long, IList<(long, long)>> collectionIndex;

            if (!_index.TryGetValue(collectionId, out collectionIndex))
            {
                collectionIndex = new Dictionary<long, IList<(long, long)>>();
                _index.Add(collectionId, collectionIndex);
            }

            return collectionIndex;
        }
    }
}
