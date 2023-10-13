﻿using BannerKings.Behaviours.Diplomacy;
using BannerKings.Behaviours.Diplomacy.Wars;
using BannerKings.Managers.Traits;
using BannerKings.Utils.Models;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BannerKings.Models.Vanilla
{
    public class BKDiplomacyModel : DefaultDiplomacyModel
    {
        public float TRADE_PACT_INFLUENCE_CAP { get;} = 100f;

        public ExplainedNumber GetPactInfluenceCost(Kingdom proposer, Kingdom proposed, bool explanations = false)
        {
            ExplainedNumber result = new ExplainedNumber(0, explanations);
            float peace = GetScoreOfDeclaringPeace(proposed, proposer, proposed, out TextObject reason) / 2f;

            foreach (var clan in proposer.Clans)
            {
                if (clan == proposer.RulingClan || clan.IsUnderMercenaryService)
                {
                    continue;
                }

                float relation = clan.Leader.GetRelation(proposer.RulingClan.Leader) / 150f;
                //result.Add((100000f - peace) * MathF.Sqrt(years), clan.Name);
            }
           
            result.AddFactor(-peace / 100000f, new TextObject("{=!}"));
            return result;
        }

        public bool IsTruceAcceptable(Kingdom proposer, Kingdom proposed, bool explanations = false)
        {
            if (proposed == proposer) return false;
            
            float peace = GetScoreOfDeclaringPeace(proposed, proposer, proposed, out TextObject reason);
            return peace > 0;
        }

        public bool IsTradeAcceptable(Kingdom proposer, Kingdom proposed, bool explanations = false)
        {
            if (proposed == proposer) return false;

            float peace = GetScoreOfDeclaringPeace(proposed, proposer, proposed, out TextObject reason);
            float influence = BannerKingsConfig.Instance.InfluenceModel.CalculateInfluenceCap(proposed.RulingClan)
                .ResultNumber;
            return peace > 0 && influence > TRADE_PACT_INFLUENCE_CAP;
        }

        public ExplainedNumber GetTruceDenarCost(Kingdom proposer, Kingdom proposed, float years = 3f, bool explanations = false)
        {
            ExplainedNumber result = new ExplainedNumber(0, explanations);
            float peace = GetScoreOfDeclaringPeace(proposed, proposer, proposed, out TextObject reason) / 2f;
            result.Add((100000f - peace) * MathF.Sqrt(years), new TextObject("{=!}Truce duration"));

            float relation = proposed.RulingClan.Leader.GetRelation(proposer.RulingClan.Leader) / 150f;
            result.AddFactor(-relation, new TextObject("{=BlidMNGT}Relation"));

            return result;
        }

        public ExplainedNumber GetTradePactInfluenceCost(Kingdom proposer, Kingdom proposed, bool explanations = false)
        {
            ExplainedNumber result = new ExplainedNumber(100, explanations);
            foreach (var fief in proposer.Fiefs)
            {
                if (fief.IsTown && fief.OwnerClan != proposer.RulingClan)
                {
                    result.Add(BannerKingsConfig.Instance.InfluenceModel.CalculateSettlementInfluence(fief.Settlement,
                        BannerKingsConfig.Instance.PopulationManager.GetPopData(fief.Settlement)).ResultNumber,
                        fief.Name);
                }
            }

            float peace = GetScoreOfDeclaringPeace(proposed, proposer, proposed, out TextObject reason);
            result.AddFactor(peace / -75000f, new TextObject("{=!}Peace interest"));
            return result;
        }

        public override int GetInfluenceCostOfProposingWar(Kingdom proposingKingdom)
        {
            return 100;
        }
        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan, out TextObject warReason)
        {
            return GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out warReason, null).ResultNumber * 10f;
        }

        public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, IFaction evaluatingClan, out TextObject peaceReason)
        {
            ExplainedNumber result = new ExplainedNumber(-GetScoreOfDeclaringWar(factionDeclaresPeace, 
                factionDeclaredPeace, evaluatingClan, out peaceReason, null).ResultNumber);

            War war = TaleWorlds.CampaignSystem.Campaign.Current.GetCampaignBehavior<BKDiplomacyBehavior>().GetWar(factionDeclaresPeace,factionDeclaredPeace);
            if (war != null)
            {
                BKExplainedNumber fatigue = BannerKingsConfig.Instance.WarModel.CalculateFatigue(war, Hero.MainHero.MapFaction, true);
                result.AddFactor(fatigue.ResultNumber);
            }

            return result.ResultNumber * 10f;
        }

        public ExplainedNumber GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan,
           out TextObject warReason, CasusBelli casusBelli = null, bool explanations = false)
        {
            warReason = TextObject.Empty;
            var result = new ExplainedNumber(0f, explanations);
            result.LimitMin(-50000f);
            result.LimitMax(50000f);

            if (factionDeclaresWar.MapFaction == factionDeclaredWar.MapFaction)
            {
                return new ExplainedNumber(-50000f);
            }

            StanceLink stance = factionDeclaresWar.GetStanceWith(factionDeclaredWar);
            if (stance.GetDailyTributePaid(factionDeclaredWar) < 0)
            {
                return new ExplainedNumber(-50000f);
            }

            float baseNumber = 0f;

            WarStats attackerStats = CalculateWarStats(factionDeclaresWar, factionDeclaredWar);
            float attackerScore = attackerStats.Strength + attackerStats.ValueOfSettlements - (attackerStats.TotalStrengthOfEnemies * 1.25f);

            if (factionDeclaredWar.IsKingdomFaction && factionDeclaresWar.IsKingdomFaction)
            {
                var attackerKingdom = (Kingdom)factionDeclaresWar;
                var defenderKingdom = (Kingdom)factionDeclaredWar;

                TextObject reason;
                bool warAllowed = TaleWorlds.CampaignSystem.Campaign.Current.Models.KingdomDecisionPermissionModel
                    .IsWarDecisionAllowedBetweenKingdoms(attackerKingdom, defenderKingdom, out reason);
                if (!warAllowed)
                {
                    return new ExplainedNumber(-50000f);
                }

                float relations = attackerKingdom.RulingClan.GetRelationWithClan(defenderKingdom.RulingClan);
                result.AddFactor(relations * -0.003f);

                var tributes = factionDeclaresWar.Stances.ToList().FindAll(x => x.GetDailyTributePaid(x.Faction2) > 0);
                result.AddFactor(-0.15f * tributes.Count);
   
                KingdomDiplomacy diplomacy = TaleWorlds.CampaignSystem.Campaign.Current.GetCampaignBehavior<BKDiplomacyBehavior>().GetKingdomDiplomacy(attackerKingdom);
                if (diplomacy != null)
                {
                    if (diplomacy.HasTradePact(defenderKingdom))
                    {
                        result.AddFactor(-0.25f);
                    }

                    if (casusBelli == null)
                    {
                        List<CasusBelli> justifications = diplomacy.GetAvailableCasusBelli(defenderKingdom);
                        foreach (var justification in justifications)
                        {
                            result.Add(justification.DeclareWarScore / justifications.Count);
                        }
                    }
                    else result.Add(casusBelli.DeclareWarScore * 2f);
                    baseNumber = result.BaseNumber;

                    result.Add(baseNumber * -diplomacy.Fatigue);
                }

                foreach (Kingdom enemyKingdom in FactionManager.GetEnemyKingdoms(attackerKingdom))
                {
                    if (enemyKingdom != attackerKingdom && enemyKingdom != defenderKingdom)
                    {
                        WarStats enemyStats = CalculateWarStats(factionDeclaresWar, enemyKingdom);
                        float enemyScore = enemyStats.Strength + enemyStats.ValueOfSettlements - (enemyStats.TotalStrengthOfEnemies * 1.25f);
                        float proportion = MathF.Clamp((attackerScore / (enemyScore * 4f)) - 1f, -1f, 0f);
                        result.Add(baseNumber * proportion);
                    }
                }

                War war = TaleWorlds.CampaignSystem.Campaign.Current.GetCampaignBehavior<BKDiplomacyBehavior>().GetWar(factionDeclaredWar, factionDeclaresWar);
                if (war != null)
                {
                    if (war.StartDate.ElapsedYearsUntilNow < 1f) result.Add(50000f);

                    float score = MathF.Clamp(war.CalculateWarScore(war.Attacker, false).ResultNumber /
                        war.TotalWarScore.ResultNumber, -1f, 1f) * 2f;
                    result.Add(baseNumber * (war.Attacker == factionDeclaresWar ? -score : score));

                    float fatigue = BannerKingsConfig.Instance.WarModel.CalculateFatigue(war, factionDeclaresWar).ResultNumber * 4f;
                    result.Add(baseNumber * - fatigue);
                }
                else
                {
                    if (stance.IsAtWar)
                    {
                        result.Add(-50000f);
                    }
                    else
                    {
                        War possibleWar = new War(attackerKingdom, defenderKingdom, null, null);
                        if (possibleWar.DefenderFront != null && possibleWar.AttackerFront != null)
                        {
                            float distance = TaleWorlds.CampaignSystem.Campaign.Current.Models.MapDistanceModel.GetDistance(possibleWar.DefenderFront.Settlement,
                                possibleWar.AttackerFront.Settlement) * 4f;
                            float factor = (TaleWorlds.CampaignSystem.Campaign.AverageDistanceBetweenTwoFortifications / distance) - 1f;
                            result.Add(baseNumber * factor);
                        }
                    }

                    //WarStats enemyStats = CalculateWarStats(factionDeclaresWar, enemyKingdom);
                    //float enemyScore = enemyStats.Strength + enemyStats.ValueOfSettlements - (enemyStats.TotalStrengthOfEnemies * 1.25f);
                }
            }

            if (evaluatingClan is Clan)
            {
                Clan evaluating = (Clan)evaluatingClan;
                Hero leader = evaluating.Leader;
                float traits = leader.GetTraitLevel(DefaultTraits.Valor) - leader.GetTraitLevel(DefaultTraits.Mercy) +
                    leader.GetTraitLevel(BKTraits.Instance.AptitudeViolence);
                result.Add(baseNumber * (traits / 4f));

                float enemies = 1f;
                if (evaluating.Kingdom != null) enemies += FactionManager.GetEnemyKingdoms(evaluating.Kingdom).Count();

                int gold = (int)(leader.Gold / enemies);
                if (gold < 50000)
                {
                    result.Add(result.BaseNumber * -0.8f);
                }
                else if (gold < 100000)
                {
                    result.Add(result.BaseNumber * -0.4f);
                }
            }

            /*WarStats defenderStats = CalculateWarStats(factionDeclaredWar, factionDeclaresWar);
            float defenderScore = defenderStats.Strength + defenderStats.ValueOfSettlements - (defenderStats.TotalStrengthOfEnemies * 1.25f);
            float scoreProportion = (attackerScore / defenderScore) - 1f;
            result.AddFactor(scoreProportion);*/

            return result;
        }

        private WarStats CalculateWarStats(IFaction faction, IFaction targetFaction)
        {
            Clan rulingClan = faction.IsClan ? (faction as Clan) : (faction as Kingdom).RulingClan;
            float valueOfSettlements = faction.Fiefs.Sum((Town f) => (float)(f.IsTown ? 2000 : 1000) + f.Prosperity * 0.33f) * 50f;
            float enemyStrength = 0f;
            foreach (StanceLink stanceLink in faction.Stances)
            {
                if (stanceLink.IsAtWar && stanceLink.Faction1 != targetFaction && stanceLink.Faction2 != targetFaction && (!stanceLink.Faction2.IsMinorFaction || stanceLink.Faction2.Leader == Hero.MainHero))
                {
                    IFaction faction2 = (stanceLink.Faction1 == faction) ? stanceLink.Faction2 : stanceLink.Faction1;
                    enemyStrength += faction2.TotalStrength;
                }
            }

            return new WarStats
            {
                RulingClan = rulingClan,
                Strength = faction.TotalStrength,
                ValueOfSettlements = valueOfSettlements,
                TotalStrengthOfEnemies = enemyStrength
            };
        }

        public struct WarStats
        {
            public Clan RulingClan;
            public float Strength;
            public float ValueOfSettlements;
            public float TotalStrengthOfEnemies;
        }
        public override void GetHeroesForEffectiveRelation(Hero hero1, Hero hero2, out Hero effectiveHero1, out Hero effectiveHero2)
        {
            effectiveHero1 = hero1;
            effectiveHero2 = hero2;
        }

        public override int GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(Hero supporter, bool hasHeroVotedAgainstOwner)
        {
            return base.GetRelationChangeAfterVotingInSettlementOwnerPreliminaryDecision(supporter, hasHeroVotedAgainstOwner);
        }
    }
}


