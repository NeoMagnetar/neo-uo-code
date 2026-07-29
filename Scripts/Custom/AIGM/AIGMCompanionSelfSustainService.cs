using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.AIGM
{
    public static class AIGMCompanionSelfSustainService
    {
        public static bool TryAutoSelfBandage(BaseHire companion)
        {
            string response;
            return TryBandageSelf(companion, out response);
        }

        public static bool TryBandageSelf(BaseHire companion, out string response)
        {
            response = null;

            if (companion == null || companion.Deleted || !companion.Alive)
            {
                response = "invalid_companion";
                return false;
            }

            if (companion.Hits >= companion.HitsMax && !companion.Poisoned)
            {
                response = "self_sustain_not_needed";
                return false;
            }

            return AIGMCompanionHealingService.TryBeginSelfBandage(companion, out response);
        }

        public static bool TryUseCurePotion(BaseHire companion, out string response)
        {
            response = null;

            if (companion == null || companion.Deleted || !companion.Alive)
            {
                response = "invalid_companion";
                return false;
            }

            if (!companion.Poisoned)
            {
                response = "not_poisoned";
                return false;
            }

            BaseCurePotion potion = FindCurePotion(companion);
            if (potion == null)
            {
                response = "cure_potion_unavailable";
                return false;
            }

            if (potion.Deleted)
            {
                response = "cure_potion_deleted";
                return false;
            }

            potion.Drink(companion);
            response = "cure_potion_used";
            return true;
        }

        private static BaseCurePotion FindCurePotion(BaseHire companion)
        {
            if (companion == null || companion.Backpack == null)
                return null;

            Item[] items = companion.Backpack.FindItemsByType(typeof(BaseCurePotion), true);
            if (items == null || items.Length == 0)
                return null;

            return items[0] as BaseCurePotion;
        }
    }
}
