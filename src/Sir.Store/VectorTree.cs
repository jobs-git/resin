﻿using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sir.Store
{
    /// <summary>
    /// A tree of indexes (one per collection and key).
    /// </summary>
    public class VectorTree
    {
        public int Count { get; private set; }

        private ConcurrentDictionary<ulong, SortedList<long, VectorNode>> _ix;
        private object _sync = new object();

        public VectorTree() : this(new ConcurrentDictionary<ulong, SortedList<long, VectorNode>>()) { }

        public VectorTree(ConcurrentDictionary<ulong, SortedList<long, VectorNode>> ix)
        {
            _ix = ix;
        }

        public void Add(ulong collectionId, long keyId, VectorNode index)
        {
            SortedList<long, VectorNode> collection;

            if (!_ix.TryGetValue(collectionId, out collection))
            {
                collection = new SortedList<long, VectorNode>();
                collection.Add(keyId, index);

                _ix.GetOrAdd(collectionId, collection);
            }
            else
            {
                if (!collection.ContainsKey(keyId))
                {
                    collection.Add(keyId, index);
                }
                else
                {
                    collection[keyId] = index;
                }
            }
        }

        public SortedList<long, VectorNode> GetIndex(ulong collectionId)
        {
            SortedList<long, VectorNode> ix;

            if (!_ix.TryGetValue(collectionId, out ix))
            {
                return null;
            }

            return ix;
        }
    }
}