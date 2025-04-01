using LibATex.Extensions;
using LibATex.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace LibATex.Model
{
	public class PeriodicTextureConfig
	{
		public float LoopDelay = 0f;
		public float LoopDelayMin = 0f;
		public float LoopDelayMax = 0f;
		public bool IsRandom = false;
	}

	public class AnimatedTextureConfig
	{
		// Required options
		public string AnimationPath = string.Empty;
		public string TargetPath = string.Empty;
		public int NumColumns = 1;
		public int NumRows = 1;

		// Alternative options
		public string AnimationQualifiedPath = string.Empty;
		public string TargetQualifiedPath = string.Empty;

		// Optional options
		public string AnimationModDomain = string.Empty;
		public string TargetModDomain = string.Empty;
		public string AtlasType
		{
			get
			{
				return atlasTypeString;
			}
			set
			{
				atlasTypeString = value;
				atlasType = AnimatedTextureAtlasManager.AtlasStringToAtlasEnum(value);
			}
		}

		public EnumAnimatedTextureType AnimationType
		{
			get;
			internal set;
		}

		public int NumFrames;
		public int Priority = 0;
		public float SecondsPerFrame = 1f;

		public bool IsPartial = false;
		public int TargetOffsetX = 0;
		public int TargetOffsetY = 0;
		public int PaddingX = 0;
		public int PaddingY = 0;

		public PeriodicTextureConfig PeriodicOptions;

		public string ModId;

		internal EnumTextureAtlasType atlasType;
		private string atlasTypeString = string.Empty;

		private ICoreClientAPI capi;
		private ILogger logger;

		public bool ValidateConfiguration(ICoreClientAPI capi, ILogger logger)
		{
			this.capi = capi;
			this.logger = logger;

			AnimationType = EnumAnimatedTextureType.AnimatedTexture;

			// Pre-validate optional parameters, as some of these have an effect
			// on other options
			if (AnimationModDomain == string.Empty)
			{
				if (AnimationQualifiedPath != string.Empty && AnimationQualifiedPath.Contains(':'))
				{
					AnimationModDomain = AnimationQualifiedPath.Split(':', 2)[0];
				} else
				{
					AnimationModDomain = ModId;
				}
			}
			if (TargetModDomain == string.Empty)
			{
				if (TargetQualifiedPath != string.Empty && TargetQualifiedPath.Contains(':'))
				{
					TargetModDomain = TargetQualifiedPath.Split(':', 2)[0];
				} else
				{
					TargetModDomain = ModId;
				}
			}

			if (!capi.ModLoader.IsModEnabled(AnimationModDomain) || !capi.ModLoader.IsModEnabled(TargetModDomain))
			{
				logger.Debug($"Mod with id {AnimationModDomain} or {TargetModDomain} is not enabled, using own modId");
				AnimationModDomain = ModId;
				TargetModDomain = ModId;
			}

			atlasType = AnimatedTextureAtlasManager.AtlasStringToAtlasEnum(atlasTypeString);

			// Validate textures
			if (!GenerateQualifiedAnimationPath())
			{
				logger.Debug($"Animation texture {AnimationQualifiedPath} for {ModId} does not seem to exist, skipping...");
				return false;
			}

			if (!GenerateQualifiedTargetPath())
			{
				logger.Debug($"Target texture {TargetQualifiedPath} for {ModId} does not seem to exist, skipping...");
				return false;
			}

			// Validate required options
			if (NumColumns < 1 || NumRows < 1)
			{
				logger.Debug($"Number of rows or columns is less than 1, skipping...");
				return false;
			}

			if (NumFrames < 1)
			{
				NumFrames = NumColumns * NumRows;
			}

			// Set default minimum to 20ms (50Hz)
			if (SecondsPerFrame < 0.02f) SecondsPerFrame = 0.02f;

			if (AtlasType == string.Empty)
			{
				AtlasType = "block";
			}

			if (TargetOffsetX < 0) TargetOffsetX = 0;
			if (TargetOffsetY < 0) TargetOffsetY = 0;
			if (PaddingX < 0) PaddingX = 0;
			if (PaddingY < 0) PaddingY = 0;

			if (IsPartial || TargetOffsetX > 0 || TargetOffsetY > 0)
			{
				AnimationType = AnimationType | EnumAnimatedTextureType.PartialAnimatedTexture;
				IsPartial = true;
			}

			if (PeriodicOptions != null)
			{
				AnimationType = AnimationType | EnumAnimatedTextureType.PeriodicAnimatedTexture;

				if (PeriodicOptions.IsRandom)
				{
					AnimationType = AnimationType | EnumAnimatedTextureType.PeriodicRandomAnimatedTexture;
				}
			}
			
			return true;
		}

		public bool GenerateQualifiedAnimationPath()
		{
			string[] splitPath;
			string qualifiedPath = string.Empty;
			if (AnimationQualifiedPath != string.Empty)
			{
				if (AnimationQualifiedPath.Contains(AssetLocation.LocationSeparator))
				{
					splitPath = AnimationQualifiedPath.Split(AssetLocation.LocationSeparator, 2);
					qualifiedPath = splitPath[0] + AssetLocation.LocationSeparator + "textures/" + splitPath[1] + ".png";

					if (ValidateAssetPath(qualifiedPath))
					{
						return true;
					}
				}
			}

			qualifiedPath = AnimationModDomain + AssetLocation.LocationSeparator + "textures/" + AnimationPath + ".png";
			AnimationQualifiedPath = AnimationModDomain + AssetLocation.LocationSeparator + AnimationPath;
			return ValidateAssetPath(qualifiedPath);
		}

		public bool GenerateQualifiedTargetPath()
		{
			string[] splitPath;
			string qualifiedPath = string.Empty;
			if (TargetQualifiedPath != string.Empty)
			{
				if (TargetQualifiedPath.Contains(AssetLocation.LocationSeparator))
				{
					splitPath = TargetQualifiedPath.Split(AssetLocation.LocationSeparator, 2);
					qualifiedPath = splitPath[0] + AssetLocation.LocationSeparator + "textures/" + splitPath[1] + ".png";

					if (ValidateAssetPath(qualifiedPath))
					{
						return true;
					}
				}
			}

			qualifiedPath = TargetModDomain + AssetLocation.LocationSeparator + "textures/" + TargetPath + ".png";
			TargetQualifiedPath = TargetModDomain + AssetLocation.LocationSeparator + TargetPath;
			return ValidateAssetPath(qualifiedPath);
		}

		public bool ValidateAssetPath(string assetPath)
		{
			AssetLocation loc = new AssetLocation(assetPath);
			return capi.Assets.TryGet(loc) != null;
		}
	}
}
