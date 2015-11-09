﻿// Copyright (c) Gothos
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Tera.Game.Messages;

namespace Tera.Game
{
    // A player character, including your own
    public class UserEntity : Entity
    {
        public string Name { get; set; }
        public string GuildName { get; set; }
        public RaceGenderClass RaceGenderClass { get; set; }
        public uint PlayerId { get; set; }

        public UserEntity(EntityId id)
            : base(id)
        {
        }

        internal UserEntity(SpawnUserServerMessage message)
            : this(message.Id)
        {
            Name = message.Name;
            GuildName = message.GuildName;
            RaceGenderClass = message.RaceGenderClass;
            PlayerId = message.PlayerId;
        }

        internal UserEntity(LoginServerMessage message)
            : this(message.Id)
        {
            Name = message.Name;
            GuildName = message.GuildName;
            RaceGenderClass = message.RaceGenderClass;
            PlayerId = message.PlayerId;
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", Name, GuildName);
        }

        public static UserEntity ForEntity(Entity entity)
        {
            var ownedEntity = entity as IHasOwner;
            while (ownedEntity != null && ownedEntity.Owner != null)
            {
                entity = ownedEntity.Owner;
                ownedEntity = entity as IHasOwner;
            }
            return entity as UserEntity;
        }
    }
}
