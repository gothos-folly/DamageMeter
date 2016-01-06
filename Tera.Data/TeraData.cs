// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Tera.Game;

namespace Tera.Data
{
    public class TeraData
    {
        public Region Region { get; private set; }
        public OpCodeNamer OpCodeNamer { get; private set; }
        public SkillDatabase SkillDatabase { get; private set; }
        public NpcDatabase NpcDatabase { get; private set; }

        internal TeraData(BasicTeraData basicData, string region)
        {
//            Region = basicData.Regions.Single(x => x.Key == region);
            string suffix = (basicData.Language == "Auto") ? (region != "EU") ? region : "EU-EN" : basicData.Language;
            SkillDatabase = new SkillDatabase(basicData.ResourceDirectory, suffix);
            NpcDatabase = new NpcDatabase(basicData.ResourceDirectory, suffix);
            OpCodeNamer = new OpCodeNamer(Path.Combine(basicData.ResourceDirectory, $"opcodes\\opcodes-{region}.txt"));
        }
    }
}
