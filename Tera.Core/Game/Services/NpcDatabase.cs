using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Tera.Game
{
    public class NpcDatabase
    {
        private readonly Dictionary<Tuple<ushort, int>, NpcInfo> _dictionary;
        private readonly Func<Tuple<ushort, int>, NpcInfo> _getPlaceholder;

        public NpcDatabase(Dictionary<Tuple<ushort, int>, NpcInfo> npcInfo)
        {
            _dictionary = npcInfo;
            _getPlaceholder = Helpers.Memoize<Tuple<ushort, int>, NpcInfo>(x => new NpcInfo(x.Item1, x.Item2, false, 0, string.Format("Npc {0} {1}", x.Item1, x.Item2)));
        }

        private static Dictionary<Tuple<ushort, int>, NpcInfo> LoadNpcInfos(string directory, string reg_lang)
        {
            var xml = XDocument.Load(Path.Combine(directory, $"monsters\\monsters-{reg_lang}.xml"));
            var NPCs = (from zones in xml.Root.Elements("Zone")
                        let huntingzoneid = ushort.Parse(zones.Attribute("id").Value)
                        from monsters in zones.Elements("Monster")
                        let templateid = int.Parse(monsters.Attribute("id").Value)
                        let boss = bool.Parse(monsters.Attribute("isBoss").Value)
                        let hp = int.Parse(monsters.Attribute("hp").Value)
                        let name = monsters.Attribute("name").Value
                        select new NpcInfo(huntingzoneid, templateid, boss, hp, name)).ToDictionary(x => Tuple.Create(x.HuntingZoneId, x.TemplateId));
            xml = XDocument.Load(Path.Combine(directory, "monsters\\monsters-override.xml"));
            var overs = (from zones in xml.Root.Elements("Zone")
                         let huntingzoneid = ushort.Parse(zones.Attribute("id").Value)
                         from monsters in zones.Elements("Monster")
                         let templateid = int.Parse(monsters.Attribute("id").Value)
                         let boss = monsters.Attribute("isBoss")?.Value
                         let hp = monsters.Attribute("hp")?.Value
                         let name = monsters.Attribute("name")?.Value
                         select new { id = Tuple.Create(huntingzoneid, templateid), boss, hp, name });
            foreach (var over in overs)
            {
                if (NPCs.ContainsKey(over.id))
                {
                    NPCs[over.id] = new NpcInfo(NPCs[over.id].HuntingZoneId, NPCs[over.id].TemplateId,
                                                (over.boss == null) ? NPCs[over.id].Boss : bool.Parse(over.boss),
                                                (over.hp == null) ? NPCs[over.id].HP : int.Parse(over.hp),
                                                (over.name == null) ? NPCs[over.id].Name : over.name);
                }
                else
                {
                    NPCs.Add(over.id, new NpcInfo(over.id.Item1, over.id.Item2,
                                                (over.boss == null) ? false : bool.Parse(over.boss),
                                                (over.hp == null) ? 0 : int.Parse(over.hp),
                                                (over.name == null) ? $"Npc {over.id.Item1} {over.id.Item2}" : over.name));
                }
            }
            return NPCs;
        }

        public NpcDatabase(string directory, string reg_lang)
            : this(LoadNpcInfos(directory, reg_lang))
        {

        }

        public NpcInfo GetOrNull(ushort huntingZoneId, int templateId)
        {
            NpcInfo result;
            _dictionary.TryGetValue(Tuple.Create(huntingZoneId, templateId), out result);
            return result;
        }

        public NpcInfo GetOrPlaceholder(ushort huntingZoneId, int templateId)
        {
            return GetOrNull(huntingZoneId, templateId) ?? _getPlaceholder(Tuple.Create(huntingZoneId, templateId));
        }
    }
}
