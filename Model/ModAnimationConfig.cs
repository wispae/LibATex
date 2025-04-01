using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace LibATex.Model
{
	public class ModAnimationConfig
	{
		public List<AnimatedTextureConfig> Animations;

		// Allow other mods to animate this mods textures
		public bool AllowsOverriding = true;
		public string ModId;

		public ModAnimationConfig()
		{
			Animations = new List<AnimatedTextureConfig>();
		}

		/// <summary>
		/// Validates this mods animation configuration
		/// </summary>
		/// <remarks>
		/// Removes loaded invalid configurations
		/// </remarks>
		/// <param name="capi"></param>
		/// <param name="logger"></param>
		/// <param name="overwriteRules">a ModId - boolean dictionary containing which mods allow overriding their textures</param>
		/// <returns>Whether the configuration contained any valid animations</returns>
		public bool ValidateConfigurations(ICoreClientAPI capi, ILogger logger, Dictionary<string, bool> overwriteRules)
		{
			List<AnimatedTextureConfig> validConfigs = new List<AnimatedTextureConfig>();

			foreach (AnimatedTextureConfig ac in Animations)
			{
				ac.ModId = ModId;
				if (ac.ValidateConfiguration(capi, logger))
				{
					if (ac.TargetModDomain == ac.ModId || !overwriteRules.ContainsKey(ac.TargetModDomain) || overwriteRules[ac.TargetModDomain])
					{
						validConfigs.Add(ac);
					}
				}
			}

			Animations.Clear();
			if (validConfigs.Count == 0)
			{
				logger.Warning($"Animation config file for mod {ModId} contained no valid animations");
				return false;
			}

			Animations = validConfigs;

			return true;
		}
	}
}
