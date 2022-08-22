﻿using BannerKings.Managers.Skills;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace BannerKings.Models.Vanilla
{
    public class BKBattleSimulationModel : DefaultCombatSimulationModel
    {
        public override int SimulateHit(CharacterObject strikerTroop, CharacterObject struckTroop, PartyBase strikerParty,
            PartyBase struckParty, float strikerAdvantage, MapEvent battle)
        {
            var result = base.SimulateHit(strikerTroop, struckTroop, strikerParty, struckParty, strikerAdvantage, battle);
            var leader = strikerParty.LeaderHero;
            if (leader != null)
            {
                var data = BannerKingsConfig.Instance.EducationManager.GetHeroEducation(leader);
                if (data.HasPerk(BKPerks.Instance.SiegePlanner) && strikerParty.SiegeEvent != null &&
                    strikerTroop.IsInfantry && strikerTroop.IsRanged)
                {
                    result = (int) (result * 1.15f);
                }
            }

            return result;
        }
    }
}