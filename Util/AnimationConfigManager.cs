using LibATex.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace LibATex.Util
{
	public class AnimationConfigManager
	{
		public Dictionary<string, ModAnimationConfig> animConfigs;
		public Dictionary<string, bool> overwriteRules;

		internal AssetLocation configBaseLocation;
		internal JsonSerializerSettings configSerializerSettings;

		private ICoreClientAPI capi;
		private ILogger logger;

		public AnimationConfigManager(ICoreClientAPI capi, ILogger logger)
		{
			this.capi = capi;
			this.logger = logger;

			animConfigs = new Dictionary<string, ModAnimationConfig>();
			overwriteRules = new Dictionary<string, bool>();

			configBaseLocation = new AssetLocation();
			configBaseLocation.Path = "config/animatedtextures.json";

			configSerializerSettings = new JsonSerializerSettings();
			configSerializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
		}

		/// <summary>
		/// Attempts to load the animated texture configuration
		/// for the mod with given mod id
		/// </summary>
		/// <remarks>
		/// Does not validate the configurations
		/// </remarks>
		/// <param name="modId"></param>
		/// <returns></returns>
		public bool LoadConfigForModId(string modId)
		{
			configBaseLocation.Domain = modId;
			logger.Debug($"Scanning mod {modId} for animated textures at location {configBaseLocation}");
			ModAnimationConfig c;

			try
			{
				IAsset a = capi.Assets.Get(configBaseLocation);

				c = a.ToObject<ModAnimationConfig>(configSerializerSettings);
				c.ModId = modId;
			}
			catch
			{
				overwriteRules.Add(modId, true);
				return false;
			}
			overwriteRules.Add(modId, c.AllowsOverriding);

			if (c.Animations.Count < 1)
			{
				animConfigs.Remove(modId);
				return false;
			}

			animConfigs.Remove(modId);
			animConfigs.Add(modId, c);

			return true;
		}

		/// <summary>
		/// Validates all loaded configurations, removing the invalid ones
		/// </summary>
		/// <remarks>
		/// Does not touch the mod overwrite rules
		/// </remarks>
		public void ValidateConfigs()
		{
			List<string> invalidConfigs = new List<string>();
			foreach (ModAnimationConfig config in animConfigs.Values)
			{
				if (!config.ValidateConfigurations(capi, logger, overwriteRules))
				{
					invalidConfigs.Add(config.ModId);
				}
			}

			foreach (string toRemove in invalidConfigs)
			{
				animConfigs.Remove(toRemove);
			}
		}

		/// <summary>
		/// Filters animations that target the same texture such that only
		/// one animation with the highest priority remains
		/// </summary>
		public void FilterUniqueTargetTextures()
		{
			Dictionary<string, List<AnimatedTextureConfig>> targetConfigs = new();
			List<AnimatedTextureConfig> uniqueAnimations = new List<AnimatedTextureConfig>();

			// 1. group all normal animations targeting the same texture
			foreach (ModAnimationConfig animconfig in animConfigs.Values)
			{
				foreach (AnimatedTextureConfig textureconfig in animconfig.Animations)
				{
					if (textureconfig.AnimationType.HasFlag(EnumAnimatedTextureType.PartialAnimatedTexture))
					{
						continue;
					}

					if (!targetConfigs.ContainsKey(textureconfig.TargetQualifiedPath))
					{
						targetConfigs.Add(textureconfig.TargetQualifiedPath, new List<AnimatedTextureConfig>());
					}

					targetConfigs[textureconfig.TargetQualifiedPath].Add(textureconfig);
				}
			}

			// 2. order each group by priority
			// 3. take the first of each group
			targetConfigs.Values.Foreach(tc =>
			{
				uniqueAnimations.Add(tc.OrderByDescending(o => o.Priority).First());
			});

			// 4. remove all others
			foreach (ModAnimationConfig animconfig in animConfigs.Values)
			{
				// remove all where a does not exist in uniqueAnimations
				animconfig.Animations.RemoveAll((a) =>
				{
					if (a.IsPartial || a.AnimationType.HasFlag(EnumAnimatedTextureType.PartialAnimatedTexture)) return false;
					bool shouldKeep = uniqueAnimations.Exists((u) =>
					{
						return u.Equals(a) || a.AnimationType.HasFlag(EnumAnimatedTextureType.PartialAnimatedTexture);
					});
					if (!shouldKeep) logger.Debug($"Ignoring animation {a.AnimationQualifiedPath} targeting {a.TargetQualifiedPath}; Superseded by higher priority");

					return !shouldKeep;
				});
			}
		}

		/// <summary>
		/// Registers this configuration managers animations to the provided animation manager
		/// </summary>
		/// <param name="texManager"></param>
		public void RegisterStartupConfigurations(AnimatedTextureManager texManager)
		{
			List<AnimatedTextureConfig> configsToLoad = new List<AnimatedTextureConfig>();
			foreach (ModAnimationConfig mconfig in animConfigs.Values)
			{
				foreach (AnimatedTextureConfig tconfig in mconfig.Animations)
				{
					configsToLoad.Add(tconfig);
				}
			}

			texManager.SetupManager(configsToLoad);
		}

		public int GetTotalAnimationCount()
		{
			int total = 0;

			foreach (ModAnimationConfig mconfig in animConfigs.Values)
			{
				total += mconfig.Animations.Count;
			}

			return total;
		}

		public void Dispose()
		{
			animConfigs.Clear();
			overwriteRules.Clear();
		}
	}
}
