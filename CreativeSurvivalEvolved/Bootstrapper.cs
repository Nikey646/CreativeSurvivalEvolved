using System;
using System.Collections.Generic;
using System.Linq;
#if DEBUG
using UnityEngine;
#endif

namespace CreativeSurvivalEvolved
{
	public class Bootstrapper : FortressCraftMod
	{
		private Boolean _registered;
		private Boolean _recipesChanged;

		private List<string> _recipes;

		private Boolean _itemsChanged;

		private void Update()
		{
			if (!this._registered)
				return;

			if (!this._recipesChanged)
			{
				if (this._recipes == null)
					this._recipes = new List<String>();
				
				foreach (var craftData in CraftData.mRecipesForSet.SelectMany(recipe => recipe.Value.Where(craftData => craftData.Key != "arther core")))
				{
					if (!this._recipes.Contains(craftData.CraftedKey))
						this._recipes.Add(craftData.CraftedKey);

					craftData.Costs.Clear();
					craftData.CraftTime = 0;
					craftData.ResearchCost = 0;
					craftData.ResearchRequirements.Clear();
					craftData.ResearchRequirementEntries.Clear();
					craftData.ScanRequirements.Clear();
				}
				this._recipesChanged = true;
#if DEBUG
				Debug.LogError($"{this._recipes.Count} Recipes are now Free");
#endif
			}

			if (!this._itemsChanged && ItemEntry.mbEntriesLoaded)
			{
				var items = 0;

				var itemRecipes = new List<CraftData>();

				foreach (var itemEntry in ItemEntry.mEntries.Where(item => item != null))
				{
					var iDic = ItemEntry.mEntriesById[itemEntry.ItemID];
					var nDic = ItemEntry.mEntriesByKey[itemEntry.Key];

					itemEntry.ResearchRequirements.Clear();
					itemEntry.ScanRequirements.Clear();

					iDic.ResearchRequirements.Clear();
					iDic.ScanRequirements.Clear();

					nDic.ResearchRequirements.Clear();
					nDic.ScanRequirements.Clear();

					if (this._recipes != null)
					{
						if (this._recipes.Contains(itemEntry.Key))
						{
							itemRecipes.AddRange(this.CreateRecipe(itemEntry));
						}
					}

					items++;
				}

				if (this._recipes != null)
				{
					this._recipes.Clear();
					this._recipes = null;
				}

				CraftData.LinkEntries(itemRecipes, "Unknown");
				CraftData.mRecipesForSet.First().Value.AddRange(itemRecipes);

				this._itemsChanged = true;
#if DEBUG
				Debug.LogError($"{items} Items have been modified");
#endif
			}

		}

		private CraftData[] CreateRecipe(ItemEntry item)
		{
			return new[]
			{
				new CraftData
				{
					Key = item.Key,
					CraftedKey = item.Key,
					CraftedName = item.Name,
					Costs = new List<CraftCost>(),
					Description = "Added to Crafting Menu by Creative Survival Evolved."
				},
				new CraftData
				{
					Key = item.Key,
					CraftedKey = item.Key,
					CraftedName = item.Name,
					Costs = new List<CraftCost>(),
					Description = "Added to Self Crafting Menu by Creative Survival Evolved.",
					CanCraftAnywhere = true,
				}
			};
		}

		public override ModRegistrationData Register()
		{
			this._registered = true;
			return base.Register();
		}

	}
}
