using System;
using Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace CreativeSurvivalEvolved
{
	public class Bootstrapper : FortressCraftMod
	{
		private Ini _settings;

		private List<string> _recipes;
		private List<CraftData> _newRecipes;

		private Boolean _registered;
		private Boolean _recipesChanged;
		private Boolean _itemsChanged;
		private Boolean _terrainChanged;
		private Boolean _researchChanged;

		private void Update()
		{
			if (!this._registered)
				return;

			if (!this._recipesChanged)
				this.ModifyRecipes();

			if (!this._itemsChanged && ItemEntry.mbEntriesLoaded)
				this.ModifyItems();

			if (!this._terrainChanged && TerrainData.mbEntriesLoaded)
				this.ModifyTerrain();

			if (this._recipes != null && this._terrainChanged && this._itemsChanged && this._recipesChanged)
				this.ApplyRecipes();

			if (!this._researchChanged && ResearchDataEntry.mEntries != null)
				this.ModifyResearch();

			if (SurvivalPlayerScript.meTutorialState != SurvivalPlayerScript.eTutorialState.NowFuckOff)
				this.ProgressTutorial();
		}

		private void ModifyRecipes()
		{
			var recipes = 0;

			foreach (var recipeSet in CraftData.mRecipesForSet)
			{
				if (!this._settings.GetBoolean(recipeSet.Key, true))
					continue;

				foreach (var recipe in recipeSet.Value)
				{
					recipe.Costs.Clear();
					recipe.CraftTime = 0;
					recipe.ResearchCost = 0;
					recipe.ResearchRequirements.Clear();
					recipe.ResearchRequirementEntries.Clear();
					recipe.ScanRequirements.Clear();
					recipe.RequiredModule = eManufacturingPlantModule.None;
					recipes++;
				}
			}

			this._recipes.AddRange(CraftData.mRecipesForSet.First().Value.Select(recipe => recipe.CraftedKey));

			var machineRecipes = CraftData.mRecipesForSet.Skip(1); // Non-Manufacturer recipes for the Manufacturer Crafting Panel
			foreach (var craftData in machineRecipes.SelectMany(machineRecipe => machineRecipe.Value))
			{
				if (this._recipes.Contains(craftData.CraftedKey))
					continue;
				this._newRecipes.AddRange(this.CreateRecipe(craftData));
			}

			this._recipesChanged = true;
#if DEBUG
			Debug.LogError($"{recipes} Recipes are now Free");
#endif
		}

		private void ModifyItems()
		{
			var items = 0;

			foreach (var itemEntry in ItemEntry.mEntries.Where(item => item != null))
			{
				var iDic = ItemEntry.mEntriesById[itemEntry.ItemID];
				var nDic = ItemEntry.mEntriesByKey[itemEntry.Key];

				itemEntry.MaxStack = 100;
				itemEntry.ResearchRequirements.Clear();
				itemEntry.ScanRequirements.Clear();

				iDic.MaxStack = 100;
				iDic.ResearchRequirements.Clear();
				iDic.ScanRequirements.Clear();

				nDic.MaxStack = 100;
				nDic.ResearchRequirements.Clear();
				nDic.ScanRequirements.Clear();

				if (this._recipes != null)
				{
					if (!this._recipes.Contains(itemEntry.Key) && !this._newRecipes.Any(recipe => recipe.CraftedKey == itemEntry.Key))
					{
						this._newRecipes.AddRange(this.CreateRecipe(itemEntry));
					}
				}

				items++;
			}

			this._itemsChanged = true;
#if DEBUG
			Debug.LogError($"{items} Items have been modified");
#endif
		}

		private void ModifyTerrain()
		{
			var terrains = 0;

			foreach (var terrain in TerrainData.mEntries.Where(terrain => terrain != null))
			{
				if (this._recipes != null && !this._recipes.Contains(terrain.Key))
					this._newRecipes.AddRange(this.CreateRecipe(terrain));
				TerrainData.mEntriesByKey[terrain.Key].MaxStack = 100;
				terrain.MaxStack = 100;
				terrains++;
			}

			this._terrainChanged = true;
#if DEBUG
			Debug.LogError($"{terrains} Cubes have been modified");
#endif
		}

		private void ApplyRecipes()
		{
			if (this._recipes == null)
				return;

			CraftData.LinkEntries(this._newRecipes, "Unknown");
			CraftData.mRecipesForSet.First().Value.AddRange(this._newRecipes);

			foreach (var newRecipe in this._newRecipes)
			{
				CraftData.mCraftCategoryDic[newRecipe.Category].recipes.Add(newRecipe);
			}

#if DEBUG
			Debug.LogError($"{this._newRecipes.Count} new recipes added.");
#endif

			this._newRecipes.Clear();
			this._recipes.Clear();
			this._newRecipes = null;
			this._recipes = null;
		}

		private void ModifyResearch()
		{
			var researches = 0;
			foreach (var entry in ResearchDataEntry.mEntries)
			{
				var keyEntry = ResearchDataEntry.mEntriesByKey?[entry.Key];

				if (keyEntry != null)
				{
					keyEntry.ResearchCost = 0;

					keyEntry.ResearchRequirements?.Clear();
					keyEntry.ProjectItemRequirements?.Clear();
					keyEntry.ScanRequirements?.Clear();
				}

				entry.ResearchCost = 0;

				entry.ResearchRequirements?.Clear();
				entry.ProjectItemRequirements?.Clear();
				entry.ScanRequirements?.Clear();

				researches++;
			}

			this._researchChanged = true;

#if DEBUG
			Debug.LogError($"{researches} Research Projects now are free to research!");
#endif
		}

		private void ProgressTutorial()
		{
			var state = (Int32)SurvivalPlayerScript.meTutorialState;
			if (state >= 2)
				SurvivalPlayerScript.meTutorialState++;

			switch ((SurvivalPlayerScript.eTutorialState) state)
			{
				case SurvivalPlayerScript.eTutorialState.CraftSomething:
					WorldScript.mLocalPlayer.mInventory.AddItem(ItemManager.SpawnItem(2000)); // Give the player an Arther Power Core
					break;
			}

		}

		private CraftData[] CreateRecipe(ItemEntry item)
		{
			if (string.IsNullOrEmpty(item.Sprite))
			{
				Debug.LogError($"Unable to add free recipe for: {item.Name}");
				return new CraftData[0];
			}
			return this.CreateRecipe(item.Key, item.Key, item.Name,
				"Added to Crafting Menu by Creative Survival Evolved.");
		}

		private CraftData[] CreateRecipe(CraftData recipe)
		{
			return this.CreateRecipe(recipe.Key, recipe.CraftedKey, recipe.CraftedName,
				recipe.Description + "\nAdded to Crafting Menu by Creative Survival Evolved.");
		}

		private CraftData[] CreateRecipe(TerrainDataEntry terrain)
		{
			if (string.IsNullOrEmpty(terrain.IconName))
			{
				Debug.LogError($"Unable to add free recipe for: {terrain.Name}");
				return new CraftData[0];
			}
			return this.CreateRecipe(terrain.Key, terrain.Key, terrain.Name,
				"Added to Crafting Menu by Creative Survival Evolved.");
		}

		private CraftData[] CreateRecipe(string key, string craftedKey, string name, string description)
		{
			return new[]
			{
				new CraftData
				{
					Key = key,
					CraftedKey = craftedKey,
					CraftedName = name,
					Costs = new List<CraftCost>(),
					Description = description,
					Category = "decoration",
				},
				new CraftData
				{
					Key = key,
					CraftedKey = craftedKey,
					CraftedName = name,
					Costs = new List<CraftCost>(),
					Description = description,
					CanCraftAnywhere = true,
					Category = "decoration",
				}
			};
		}

		public override ModRegistrationData Register()
		{
			this._registered = true;

			var location = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CreativeSurvivalEvolved.ini");
			if (!File.Exists(location))
			{
				using (var fs = File.OpenWrite(location))
				using (var stream = new StreamWriter(fs))
				{
					stream.WriteLine("[CreativeSurvivalEvolved]");

					foreach (var key in CraftData.mRecipesForSet.Keys)
					{
						stream.WriteLine($"{key}=true # Free recipes for {key}?");
					}
				}
			}

			this._settings = new Ini(location);
			if (!this._settings.ContainsSection("CreativeSurvivalEvolved"))
				throw new IniException("Invalid CreativeSurvivalEvolved.ini");

			this._settings.SetSection("CreativeSurvivalEvolved");

			this._recipes = new List<String>();
			this._newRecipes = new List<CraftData>();

			return base.Register();
		}

	}
}
