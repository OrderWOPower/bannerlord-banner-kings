﻿using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace BannerKings.UI.Items
{
    public class BKTraitItemVM : ViewModel
    {
		public BKTraitItemVM(TraitObject traitObj, bool positive)
		{
			_traitObj = traitObj;
			TraitId = traitObj.StringId;
			Value = positive ? 2 : -2;
			Hint = new HintViewModel(traitObj.Description, null);
		}

		[DataSourceProperty]
		public string Name => _traitObj.Name.ToString();

		[DataSourceProperty]
		public string TraitId
		{
			get => this._traitId;
			set
			{
				if (value != this._traitId)
				{
					this._traitId = value;
					base.OnPropertyChangedWithValue(value, "TraitId");
				}
			}
		}


		[DataSourceProperty]
		public HintViewModel Hint
		{
			get => this._hint;
			set
			{
				if (value != this._hint)
				{
					this._hint = value;
					base.OnPropertyChangedWithValue(value, "Hint");
				}
			}
		}

		[DataSourceProperty]
		public int Value
		{
			get => this._value;
			set
			{
				if (value != this._value)
				{
					this._value = value;
					base.OnPropertyChangedWithValue(value, "Value");
				}
			}
		}

		private readonly TraitObject _traitObj;
		private string _traitId;
		private int _value;
		private HintViewModel _hint;
	}
}
