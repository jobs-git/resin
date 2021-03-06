﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Sir.Store
{
    public class WriteJob
    {
        public string CollectionId { get; }
        public IEnumerable<IDictionary> Documents { get; }
        public Guid Id { get; }

        public WriteJob(string collectionId, IEnumerable<IDictionary> documents)
        {
            CollectionId = collectionId;
            Id = Guid.NewGuid();
            Documents = documents;
        }
    }

    public class IndexingJob
    {
        public string CollectionId { get; }
        public IEnumerable<IDictionary> Documents { get; }

        public IndexingJob(string collectionId, IEnumerable<IDictionary> documents)
        {
            CollectionId = collectionId;
            Documents = documents;
        }
    }
}
