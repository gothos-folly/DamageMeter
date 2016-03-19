// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Tera.Game;

namespace Tera.Data
{
    public class BasicTeraData
    {
        public string ResourceDirectory { get; private set; }
        public ServerDatabase Servers { get; private set; }
//        public IEnumerable<Region> Regions { get;private set; }
        public string Language { get; private set; }
        private readonly Func<string, TeraData> _dataForRegion;

        public TeraData DataForRegion(string region)
        {
            return _dataForRegion(region);
        }

        public BasicTeraData(string language="Auto")
            : this(FindResourceDirectory(),language)
        {
        }

        public BasicTeraData(string resourceDirectory,string language)
        {
            ResourceDirectory = resourceDirectory;
            Language = language;
            _dataForRegion = Helpers.Memoize<string, TeraData>(region => new TeraData(this, region));
            Servers = new ServerDatabase(ResourceDirectory);
//            Regions = GetRegions(Path.Combine(ResourceDirectory, "regions.txt")).ToList();
        }

        private static string FindResourceDirectory()
        {
            var directory = Path.GetDirectoryName(typeof(BasicTeraData).Assembly.Location);
            while (directory != null)
            {
                var resourceDirectory = Path.Combine(directory, @"resources\");
                if (Directory.Exists(resourceDirectory))
                    return resourceDirectory;
                directory = Path.GetDirectoryName(directory);
            }
            throw new InvalidOperationException("Could not find the resource directory");
        }

        private static IEnumerable<Region> GetRegions(string filename)
        {
            return File.ReadAllLines(filename)
                       .Where(s => !string.IsNullOrWhiteSpace(s))
                       .Select(s => s.Split(new[] { ' ' }))
                       .Select(parts => new Region(parts[0], parts[1]));
        }
    }
}
