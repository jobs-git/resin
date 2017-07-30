﻿using DocumentTable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Resin
{
    /// <summary>
    /// Scored posting. To combine inside a index, use doc ID. To combine between indices, use doc hash.
    /// </summary>
    public class DocumentScore
    {
        public int DocumentId { get; private set; }
        public double Score { get; private set; }
        public BatchInfo Ix { get; private set; }
        public UInt64 DocHash { get; private set; }

        public DocumentScore(int documentId, UInt64 docHash, double score, BatchInfo ix)
        {
            DocumentId = documentId;
            Score = score;
            Ix = ix;
            DocHash = docHash;
        }

        public void Add(DocumentScore score)
        {
            if (!score.DocumentId.Equals(DocumentId)) throw new ArgumentException("Document IDs differ. Cannot combine.", "score");

            Score = (Score + score.Score);
        }

        public static IList<DocumentScore> Not(IList<DocumentScore> source, IList<DocumentScore> exclude)
        {
            var dic = exclude.ToDictionary(x => x.DocumentId);
            var result = new List<DocumentScore>();

            foreach (var score in source)
            {
                DocumentScore exists;
                if (!dic.TryGetValue(score.DocumentId, out exists))
                {
                    result.Add(score);
                }
            }
            return result;
        }

        public static IList<DocumentScore> CombineOrPhrase(IList<DocumentScore> first, IList<DocumentScore> other)
        {
            if (first == null && other == null) return new DocumentScore[0];
            if (first == null) return other;
            if (other == null) return first;

            return first.Concat(other).GroupBy(x => x.DocumentId).Select(group =>
            {
                var list = group.ToArray();

                var top = list[0];
                for (int index = 1; index < list.Length; index++)
                {
                    top.Add(list[index]);
                }
                return top;
            }).ToArray();
        }

        public static IList<DocumentScore> CombineAndPhrase(IList<DocumentScore> first, IList<DocumentScore> other)
        {
            if (first == null && other == null) return new DocumentScore[0];
            if (first == null) return other;
            if (other == null) return first;

            var dic = other.ToDictionary(x => x.DocumentId);
            var result = new List<DocumentScore>(dic.Count);

            foreach (var score in first)
            {
                DocumentScore exists;
                if (dic.TryGetValue(score.DocumentId, out exists))
                {
                    score.Add(exists);
                    result.Add(score);
                }
            }
            return result;
        }

        public static IList<DocumentScore> CombineOr(IList<DocumentScore> first, IList<DocumentScore> other)
        {
            if (first == null && other == null) return new DocumentScore[0];
            if (first == null) return other;
            if (other == null) return first;

            return first.Concat(other).GroupBy(x => x.DocumentId).Select(group =>
            {
                var list = group.ToArray();
                
                var top = list[0];
                for (int index = 1; index < list.Length; index++)
                {
                    top.Add(list[index]);
                }
                return top;
            }).ToArray();
        }

        public static IList<DocumentScore> CombineAnd(IList<DocumentScore> first, IList<DocumentScore> other)
        {
            var dic = other.ToDictionary(x => x.DocumentId);
            var result = new List<DocumentScore>(dic.Count);

            foreach (var score in first)
            {
                DocumentScore exists;
                if (dic.TryGetValue(score.DocumentId, out exists))
                {
                    score.Add(exists);
                    result.Add(score);
                }
            }
            return result;
        }

        public override string ToString()
        {
            return string.Format("docid:{0} score:{1}", DocumentId, Score);
        }
    }

    public static class DocumentScoreExtensions
    {
        public static IList<DocumentScore> Sum(this IList<DocumentScore>[] scores)
        {
            if (scores.Length == 0) return new DocumentScore[0];

            if (scores.Length == 1) return scores[0].Compress();

            var first = scores[0];

            for (int i = 1; i < scores.Length; i++)
            {
                first = DocumentScore.CombineOr(first, scores[i]);
            }
            return first;
        }

        public static IList<DocumentScore> Compress(this IList<DocumentScore> scores)
        {
            var compressed = new List<DocumentScore>();
            DocumentScore tmp = null;
            foreach(var score in scores)
            {
                if (tmp == null)
                {
                    tmp = score;
                    continue;
                }
                if (score.DocumentId == tmp.DocumentId)
                {
                    tmp.Add(score);
                    tmp = score;
                }
                else
                {
                    compressed.Add(tmp);
                    tmp = null;
                }
            }
            if (tmp != null)
            {
                compressed.Add(tmp);
            }
            return compressed;
        }

        public static DocumentScore[] CombineTakingLatestVersion(this IList<DocumentScore[]> scores)
        {
            if (scores.Count == 0) return new DocumentScore[0];

            if (scores.Count == 1) return scores[0];

            var first = scores[0];

            for (int i = 1; i < scores.Count; i++)
            {
                first = CombineTakingLatestVersion(first, scores[i]);
            }
            return first;
        }

        public static DocumentScore[] CombineTakingLatestVersion(DocumentScore[] first, DocumentScore[] second)
        {
            var unique = new Dictionary<UInt64, DocumentScore>();

            foreach (var score in first.Concat(second))
            {
                DocumentScore exists;

                if (unique.TryGetValue(score.DocHash, out exists))
                {
                    exists = TakeLatestVersion(exists, score);
                }
                else
                {
                    unique.Add(score.DocHash, score);
                }
            }
            return unique.Values.ToArray();
        }

        public static DocumentScore TakeLatestVersion(DocumentScore first, DocumentScore second)
        {
            if (!first.DocHash.Equals(second.DocHash)) throw new ArgumentException("Document hashes differ. Cannot take latest version.", "score");

            if (first.Ix.VersionId > second.Ix.VersionId)
            {
                return first;
            }
            return second;
        }
    }
}