﻿using System;

namespace Sir.Store
{
    public class Query
    {
        public Query()
        {
        }

        public Query(IComparable key, IComparable value)
        {
            Term = new Term(key, value);
            Or = true;
        }

        public ulong Collection { get; set; }
        public bool And { get; set; }
        public bool Or { get; set; }
        public bool Not { get; set; }
        public Term Term { get; set; }
        public Query Next { get; set; }
        public int Take { get; set; }

        public override string ToString()
        {
            var op = And ? "+" : Or ? " " : "-";
            return string.Format("{0}{1}", op, Term);
        }
    }
}