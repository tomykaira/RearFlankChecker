using System;
using Advanced_Combat_Tracker;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace RearFlankChecker.Util
{
    class CombatActionChecker
    {
        Dictionary<string, Skill> skillMap;
        private const int BONUS_COMBO = 0;
        private const int BONUS_POSITIONAL = 1;
        private const int NO_BONUS_PERCENT = 0;
        private const int SKILL_ID_AUTO_ATTACK = 7;

        public CombatActionChecker()
        {
            String path = ResourceLocator.findResourcePath("resources/potencies.json");
            string data = File.ReadAllText(path);
            skillMap = ParseJsonToSkillMap(data);
        }

        public bool JudgeFlankOrRearSkill(UInt32 skillID, UInt32 potencyPercent, string skillName)
        {
            Skill s;

            if (skillID == SKILL_ID_AUTO_ATTACK)
            {
                return true;
            }

            if (!skillMap.TryGetValue(skillID.ToString(), out s))
            {
                return true;
            }

            var result = !s.MissedPositionalBonusPercents.Contains((int)potencyPercent);
            ActGlobals.oFormActMain.WriteInfoLog($"RFC: Check: {skillID} {skillName} {result} {potencyPercent} {string.Join(",", s.MissedPositionalBonusPercents)}");

            return result;
        }

        static Dictionary<string, Skill> ParseJsonToSkillMap(string jsonString)
        {
            Dictionary<string, Skill> skillMap = new Dictionary<string, Skill>();
            try
            {
                JObject root = JObject.Parse(jsonString);
                foreach (var prop in root)
                {
                    string key = prop.Key;
                    if (prop.Value.Type == JTokenType.Object)
                    {
                        skillMap.Add(key, ParseSkill((JObject)prop.Value));
                    }
                    else
                    {
                        ActGlobals.oFormActMain.WriteInfoLog("RFC: Cannot read potencies.json: Invalid value at " + key);
                    }
                }
                return skillMap;
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteInfoLog($"RFC: Error parsing JSON to Skill map: {ex.Message}");
                return new Dictionary<string, Skill>();
            }
        }

        static Skill ParseSkill(JObject elem)
        {
            Skill skill = new Skill();
            skill.Name = (string)elem["name"];
            JArray potenciesArray = (JArray)elem["potencies"];
            foreach (JObject p in potenciesArray)
            {
                Potency potency = new Potency();
                potency.Value = (int)p["value"];
                JArray bonusModifiersArray = (JArray)p["bonusModifiers"];
                if (bonusModifiersArray != null)
                {
                    foreach (var m in bonusModifiersArray)
                    {
                        potency.BonusModifiers.Add((int)m);
                    }
                }
                skill.Potencies.Add(potency);
            }
            skill.MissedPositionalBonusPercents = CalculateMissedPositionalBonusPercents(skill.Potencies);
            return skill;
        }

        static List<int> CalculateMissedPositionalBonusPercents(List<Potency> potencies)
        {
            // Ref: https://github.com/xivanalysis/xivanalysis/blob/dawntrail/src/parser/core/modules/Positionals.tsx#L92
        var missedPositionalBonusPercents = new List<int> { NO_BONUS_PERCENT };
            if (potencies.Count() == 0)
            {
                return missedPositionalBonusPercents;
            }

            var possibleBasePotencies = potencies.Where(potency =>
                potency.BonusModifiers.Count == 0 ||
                (potency.BonusModifiers.Count == 1 && potency.BonusModifiers[0] == BONUS_COMBO)
            ).ToList();

            foreach (var basePotency in possibleBasePotencies)
            {
                var possibleBonusPotencies = potencies.Where(potency =>
                    !potency.BonusModifiers.Contains(BONUS_POSITIONAL) &&
                    potency.Value > basePotency.Value
                ).ToList();

                foreach (var bonusPotency in possibleBonusPotencies)
                {
                    missedPositionalBonusPercents.Add(CalculateBonusPercent(
                        basePotency.Value,
                        bonusPotency.Value
                    ));
                }
            }

            return missedPositionalBonusPercents.Distinct().ToList();

        }

        static int CalculateBonusPercent(int baseValue, int bonus)
        {
            return (int)(100 * (1.0 - (double)baseValue / (double)bonus));
        }
    }

    class Skill
    {
        public string Name { get; set; }
        public List<Potency> Potencies { get; set; }
        public string Id {  get; set; }
        public List<int> MissedPositionalBonusPercents { get; set; }

        public Skill()
        {
            Potencies = new List<Potency>();
            MissedPositionalBonusPercents = new List<int>();
        }
    }

    class Potency
    {
        public int Value { get; set; }
        public List<int> BonusModifiers { get; set; }
        // Not supported because the values are int or string.
        // public List<int> baseModifiers { get; set; }

        public Potency()
        {
            BonusModifiers = new List<int>();
        }
    }

}
