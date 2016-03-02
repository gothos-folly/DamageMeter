// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Tera.Game
{
    public class SkillInfo
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public bool? IsChained { get; private set; }
        public string Detail { get; private set; }

        internal SkillInfo(int id, string name, bool? isChained = null, string detail = "")
        {
            Id = id;
            Name = name;
            IsChained = isChained;
            Detail = detail;
        }
    }

    public class UserSkillInfo : SkillInfo
    {
        public RaceGenderClass RaceGenderClass { get; private set; }

        public UserSkillInfo(int id, RaceGenderClass raceGenderClass, string name, bool? isChained = null, string detail = "")
            : base(id, name, isChained, detail)
        {
            RaceGenderClass = raceGenderClass;
        }

        public override bool Equals(object obj)
        {
            var other = obj as UserSkillInfo;
            if (other == null)
                return false;
            return (Id == other.Id) && (RaceGenderClass.Equals(other.RaceGenderClass));
        }

        public override int GetHashCode()
        {
            return Id + RaceGenderClass.GetHashCode();
        }
    }
}