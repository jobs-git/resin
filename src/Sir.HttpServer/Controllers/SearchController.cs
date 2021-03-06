﻿using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Sir.HttpServer.Controllers
{
    public class SearchController : UIController
    {
        private readonly PluginsCollection _plugins;

        public SearchController(PluginsCollection plugins, IConfigurationService config) : base(config)
        {
            _plugins = plugins;
        }

        [HttpGet("/search/")]
        [HttpPost("/search/")]
        public async Task<IActionResult> Index(string q, string collectionId)
        {
            if (string.IsNullOrWhiteSpace(q)) return View();

            string cid = collectionId ?? "www";

            ViewData["q"] = q;

            var reader = _plugins.Get<IReader>("application/json");

            if (reader == null)
            {
                throw new System.NotSupportedException();
            }

            var timer = new Stopwatch();
            timer.Start();

            var result = await reader.Read(cid, Request);

            ViewData["time_ms"] = timer.ElapsedMilliseconds;
            ViewData["collectionName"] = cid;
            ViewData["total"] = result.Total;

            if (result.Total== 0)
            {
                return View(new SearchResultModel[0]);
            }

            var documents = result.Documents.Select(x => new SearchResultModel { Document = x });

            return View(documents);
        }
    }

    public class SearchResultModel
    {
        public IDictionary Document { get; set; }
    }
}