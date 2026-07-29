using System;
using System.Collections.Generic;

using Server.Custom.AIGM.Characters.Waylander;
using Server.Mobiles;

namespace Server.Custom.AIGM.Tasks
{
    public static class AIGMRosterFactionService
    {
        public static string ResolveCharacterId(Mobile mobile)
        {
            if (mobile == null)
                return String.Empty;

            if (mobile is IAIGMRosterTaskAgent taskAgent)
                return taskAgent.RosterCharacterId ?? String.Empty;

            return String.Empty;
        }

        public static WaylanderCharacterDefinition ResolveDefinition(Mobile mobile)
        {
            string characterId = ResolveCharacterId(mobile);
            return String.IsNullOrWhiteSpace(characterId)
                ? null
                : WaylanderRosterCatalog.GetDefinition(characterId);
        }

        public static AIGMRosterFaction ResolveFaction(string characterId)
        {
            switch ((characterId ?? String.Empty).ToLowerInvariant())
            {
                case "dakeyras":
                case "dakeyras.grey_man":
                case "danyal":
                case "miriel":
                case "krylla":
                case "angel":
                case "senta":
                case "keeva_taliana":
                case "scar":
                    return AIGMRosterFaction.WaylanderFamily;
                case "dardalion":
                    return AIGMRosterFaction.TheThirty;
                case "karnak":
                case "egel":
                case "gellan":
                case "jonat":
                case "sarvaj":
                case "orien":
                case "druss":
                case "regnak":
                    return AIGMRosterFaction.Drenai;
                case "belash":
                case "ansi_chen":
                case "tenaka_khan":
                    return AIGMRosterFaction.Wolfshead;
                case "kesa_khan":
                    return AIGMRosterFaction.Nadir;
                case "zhu_chao":
                case "bodalen":
                    return AIGMRosterFaction.Gothir;
                case "innicas":
                case "dark_brotherhood_knight":
                case "morak":
                    return AIGMRosterFaction.DarkBrotherhood;
                case "duke_of_kydor":
                case "aric":
                    return AIGMRosterFaction.Kydor;
                case "sathuli_lord":
                    return AIGMRosterFaction.Sathuli;
                case "kuan_hador_demon_lord":
                    return AIGMRosterFaction.KuanHador;
                case "joining":
                case "ustarte":
                    return AIGMRosterFaction.Joinings;
                case "durmast":
                case "cadoras":
                case "hewla":
                case "kai":
                case "kysumu":
                case "yu_yu_liang":
                case "matze_chai":
                case "niallad":
                    return AIGMRosterFaction.Independent;
                case "kaem":
                    return AIGMRosterFaction.Neutral;
                default:
                    return AIGMRosterFaction.Neutral;
            }
        }

        public static AIGMRosterFaction ResolveFaction(Mobile mobile)
        {
            return ResolveFaction(ResolveCharacterId(mobile));
        }

        public static bool IsHero(string characterId)
        {
            switch ((characterId ?? String.Empty).ToLowerInvariant())
            {
                case "dakeyras":
                case "dakeyras.grey_man":
                case "danyal":
                case "dardalion":
                case "miriel":
                case "angel":
                case "senta":
                case "belash":
                case "druss":
                case "ustarte":
                case "kysumu":
                case "yu_yu_liang":
                case "tenaka_khan":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsHero(Mobile mobile)
        {
            return IsHero(ResolveCharacterId(mobile));
        }

        public static bool IsCommander(string characterId)
        {
            switch ((characterId ?? String.Empty).ToLowerInvariant())
            {
                case "innicas":
                case "zhu_chao":
                case "dardalion":
                case "karnak":
                case "egel":
                case "ansi_chen":
                case "druss":
                    return true;
                default:
                    return false;
            }
        }

        public static bool AreAllied(AIGMRosterFaction left, AIGMRosterFaction right)
        {
            if (left == AIGMRosterFaction.None || right == AIGMRosterFaction.None)
                return false;

            if (left == right)
                return true;

            if ((left == AIGMRosterFaction.Heroes && (right == AIGMRosterFaction.Drenai || right == AIGMRosterFaction.WaylanderFamily || right == AIGMRosterFaction.TheThirty))
                || (right == AIGMRosterFaction.Heroes && (left == AIGMRosterFaction.Drenai || left == AIGMRosterFaction.WaylanderFamily || left == AIGMRosterFaction.TheThirty)))
                return true;

            if ((left == AIGMRosterFaction.Drenai && right == AIGMRosterFaction.TheThirty)
                || (right == AIGMRosterFaction.Drenai && left == AIGMRosterFaction.TheThirty))
                return true;

            if ((left == AIGMRosterFaction.WaylanderFamily && (right == AIGMRosterFaction.Drenai || right == AIGMRosterFaction.TheThirty || right == AIGMRosterFaction.Heroes))
                || (right == AIGMRosterFaction.WaylanderFamily && (left == AIGMRosterFaction.Drenai || left == AIGMRosterFaction.TheThirty || left == AIGMRosterFaction.Heroes)))
                return true;

            if ((left == AIGMRosterFaction.Wolfshead && right == AIGMRosterFaction.Heroes)
                || (right == AIGMRosterFaction.Wolfshead && left == AIGMRosterFaction.Heroes))
                return true;

            return false;
        }

        public static bool AreEnemies(AIGMRosterFaction left, AIGMRosterFaction right)
        {
            if (left == AIGMRosterFaction.None || right == AIGMRosterFaction.None)
                return false;

            if (AreAllied(left, right))
                return false;

            if ((left == AIGMRosterFaction.DarkBrotherhood || left == AIGMRosterFaction.Gothir)
                && (right == AIGMRosterFaction.Heroes || right == AIGMRosterFaction.WaylanderFamily || right == AIGMRosterFaction.TheThirty || right == AIGMRosterFaction.Wolfshead))
                return true;

            if ((right == AIGMRosterFaction.DarkBrotherhood || right == AIGMRosterFaction.Gothir)
                && (left == AIGMRosterFaction.Heroes || left == AIGMRosterFaction.WaylanderFamily || left == AIGMRosterFaction.TheThirty || left == AIGMRosterFaction.Wolfshead))
                return true;

            if ((left == AIGMRosterFaction.KuanHador && right != AIGMRosterFaction.KuanHador)
                || (right == AIGMRosterFaction.KuanHador && left != AIGMRosterFaction.KuanHador))
                return true;

            if ((left == AIGMRosterFaction.Joinings && right == AIGMRosterFaction.Heroes)
                || (right == AIGMRosterFaction.Joinings && left == AIGMRosterFaction.Heroes))
                return true;

            return false;
        }

        public static bool HasExplicitEnemyRelationship(Mobile agent, Mobile target)
        {
            WaylanderCharacterDefinition definition = ResolveDefinition(agent);
            string targetId = ResolveCharacterId(target);
            if (definition == null || String.IsNullOrWhiteSpace(targetId) || definition.EnemyIds == null)
                return false;

            for (int i = 0; i < definition.EnemyIds.Length; i++)
            {
                if (String.Equals(definition.EnemyIds[i], targetId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static IEnumerable<string> GetGroupCharacterIds(string groupKey)
        {
            switch ((groupKey ?? String.Empty).Trim().ToLowerInvariant())
            {
                case "heroes":
                    return new[] { "dakeyras", "danyal", "dardalion", "miriel", "angel", "senta", "belash", "druss", "ustarte", "kysumu", "yu_yu_liang" };
                case "darkbrotherhood":
                case "dark brotherhood":
                case "brotherhood":
                    return new[] { "innicas", "dark_brotherhood_knight", "morak", "zhu_chao" };
                case "joinings":
                case "joining":
                    return new[] { "joining", "ustarte" };
                case "wolfshead":
                    return new[] { "belash", "ansi_chen", "kesa_khan", "tenaka_khan" };
                case "thethirty":
                case "the thirty":
                    return new[] { "dardalion" };
                default:
                    return Array.Empty<string>();
            }
        }
    }
}
