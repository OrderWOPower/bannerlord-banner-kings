﻿using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

namespace BannerKings.Managers.Institutions.Religions
{
    public abstract class ReligiousLeadership
    {

        public abstract void Initialize(Religion religion);

        public abstract TextObject GetName();

        public abstract TextObject GetHint();

        public void ChangeClergymanRank(Religion religion, Clergyman clergyman, int newRank)
        {
            TextObject firstName = clergyman.Hero.FirstName;
            TextObject fullName = new TextObject("{=!}{RELIGIOUS_TITLE} {NAME}")
                .SetTextVariable("RELIGIOUS_TITLE", religion.Faith.GetRankTitle(newRank))
                .SetTextVariable("NAME", firstName);
            clergyman.Hero.SetName(fullName, firstName);
            clergyman.Rank = newRank;
        }

        public abstract bool IsLeader(Clergyman clergyman);
    }
}