using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMAddAdapter
    {
        public static bool TryCreateItemInContainer(Mobile requester, Container targetContainer, string typeOrAlias, string[] constructorArgs, string[,] properties, out Item item, out string message)
        {
            item = null;
            message = null;

            Log("TryCreateItemInContainer start requester=" + SafeName(requester) + " alias=" + (typeOrAlias ?? String.Empty) + " args=" + String.Join(",", constructorArgs ?? new string[0]) + " container=" + (targetContainer != null ? targetContainer.GetType().Name : "null"));

            if (requester == null)
            {
                message = "No requesting mobile was provided.";
                Log("fail requester missing");
                return false;
            }

            if (targetContainer == null || targetContainer.Deleted)
            {
                message = "The target container is not available.";
                Log("fail container unavailable");
                return false;
            }

            string resolvedTypeName = ResolveTypeName(typeOrAlias);
            Log("resolvedTypeName=" + (resolvedTypeName ?? "null"));
            if (String.IsNullOrWhiteSpace(resolvedTypeName))
            {
                message = "I do not yet know how to create that item safely.";
                Log("fail type unresolved");
                return false;
            }

            Type type = ScriptCompiler.FindTypeByName(resolvedTypeName);
            Log("resolvedClrType=" + (type != null ? type.FullName : "null"));
            if (type == null || !typeof(Item).IsAssignableFrom(type))
            {
                message = "No constructable item type with that name was found.";
                Log("fail clr type invalid");
                return false;
            }

            ConstructorInfo[] ctors = type.GetConstructors();
            string[] args = constructorArgs ?? new string[0];

            for (int i = 0; i < ctors.Length; ++i)
            {
                ConstructorInfo ctor = ctors[i];

                if (!Add.IsConstructable(ctor, requester.AccessLevel))
                    continue;

                ParameterInfo[] paramList = ctor.GetParameters();
                if (paramList.Length != args.Length)
                    continue;

                Log("trying ctor=" + ctor.ToString());
                object[] values = Add.ParseValues(paramList, args);
                if (values == null)
                {
                    Log("ctor parse failed");
                    continue;
                }

                bool sendError = false;
                object built = Add.Build(requester, ctor, values, properties, null, ref sendError);
                Item created = built as Item;
                if (created == null)
                {
                    Log("ctor built null or non-item sendError=" + sendError);
                    continue;
                }

                if (!targetContainer.TryDropItem(requester, created, false))
                {
                    created.Delete();
                    message = "The target container could not receive that item.";
                    Log("fail container rejected item type=" + created.GetType().Name);
                    return false;
                }

                item = created;
                message = "Item created via Add adapter.";
                Log("success created type=" + created.GetType().Name + " serial=" + created.Serial.Value);
                return true;
            }

            message = "No matching constructable item constructor could be used for that request.";
            Log("fail no matching constructable constructor");
            return false;
        }

        private static string SafeName(Mobile mob)
        {
            if (mob == null)
                return "null";

            return String.Format("{0}({1})", mob.Name ?? mob.GetType().Name, mob.Serial.Value);
        }

        private static void Log(string message)
        {
            try
            {
                string path = Path.Combine(Core.BaseDirectory, "Logs", "AIGMAddAdapter.log");
                File.AppendAllText(path, DateTime.UtcNow.ToString("o") + " " + (message ?? String.Empty) + Environment.NewLine);
            }
            catch
            {
            }
        }

        private static string ResolveTypeName(string raw)
        {
            if (String.IsNullOrWhiteSpace(raw))
                return null;

            string alias = raw.Trim().ToLowerInvariant();
            while (alias.Contains("  "))
                alias = alias.Replace("  ", " ");

            switch (alias)
            {
                case "scissors": return "Scissors";
                case "katana": return "Katana";
                case "longsword": return "Longsword";
                case "broadsword": return "Broadsword";
                case "dagger": return "Dagger";
                case "bag": return "Bag";
                case "backpack": return "Backpack";
                case "bandage":
                case "bandages": return "Bandage";
                case "blank scroll":
                case "blank scrolls": return "BlankScroll";
                case "recall rune":
                case "rune": return "RecallRune";
                case "gold": return "Gold";
                case "black pearl":
                case "black pearls": return "BlackPearl";
                case "bloodmoss": return "Bloodmoss";
                case "garlic": return "Garlic";
                case "ginseng": return "Ginseng";
                case "mandrake":
                case "mandrake root":
                case "mandrakes": return "MandrakeRoot";
                case "nightshade": return "Nightshade";
                case "sulfurous ash": return "SulfurousAsh";
                case "spider silk":
                case "spiders silk": return "SpidersSilk";
                default: return raw.Trim();
            }
        }
    }
}
