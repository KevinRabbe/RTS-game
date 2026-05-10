namespace RtsGame.Sim.Data
{
    public static class TechRules
    {
        public static bool IsCompleted(PlayerState player, TechId techId)
        {
            for (int i = 0; i < player.TechState.CompletedTechs.Count; i++)
            {
                if (player.TechState.CompletedTechs[i] == techId)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsQueued(PlayerState player, TechId techId)
        {
            for (int i = 0; i < player.TechState.ResearchQueue.Count; i++)
            {
                if (player.TechState.ResearchQueue[i].TechId == techId)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetModifierValue(PlayerState player, ModifierId modifierId)
        {
            int value = 0;
            for (int i = 0; i < player.TechState.Modifiers.Count; i++)
            {
                PlayerModifier modifier = player.TechState.Modifiers[i];
                if (modifier.ModifierId == modifierId)
                {
                    value += modifier.Value;
                }
            }

            return value;
        }

        public static int GetModifiedUnitAttackDamage(UnitTypeId unitTypeId, PlayerState player)
        {
            int baseDamage = GameData.GetUnitAttackDamage(unitTypeId);
            if (unitTypeId == UnitTypeId.Infantry)
            {
                return baseDamage + GetModifierValue(player, ModifierId.InfantryAttackBonus);
            }

            return baseDamage;
        }

        public static void ApplyCompletedTech(PlayerState player, TechId techId)
        {
            player.TechState.CompletedTechs.Add(techId);
            switch (techId)
            {
                case TechId.InfantryAttack1:
                    player.TechState.Modifiers.Add(new PlayerModifier(ModifierId.InfantryAttackBonus, GameData.InfantryAttack1DamageBonus));
                    break;
            }
        }
    }
}
