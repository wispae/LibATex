using LibATex.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace LibATex.Util
{
	public enum EnumTextureAtlasType
	{
		Block,
		Entity,
		Item
	}

	public class AnimatedTextureManager
	{
		private static object _gLock = new();
		private static AnimatedTextureManager instance;

        // private List<AnimatedTexture> loadedTextures;
        // Dictionary for keeping all textures for quick lookup
        private Dictionary<ulong, AnimatedTexture> loadedTextures;
        // List for more efficient iterating over running textures
		private List<AnimatedTexture> runningTextures;

        private ulong idKeeper = 0;

		public ITextureAtlasAPI AnimatedTextureAtlas
		{
			get;
			private set;
		}

		private ICoreClientAPI capi;
		private ILogger logger;

		public bool IsInitialized = false;
		private long tickListenerId;

		public AnimatedTextureManager(ICoreClientAPI capi, ILogger logger)
		{
			this.capi = capi;
			this.logger = logger;
            // loadedTextures = new List<AnimatedTexture>();
            loadedTextures = new Dictionary<ulong, AnimatedTexture>();
			runningTextures = new List<AnimatedTexture>();
			// AnimatedTextureAtlas = new AnimatedTextureAtlasManager(capi.World as ClientMain);
		}

		public AnimatedTexture RegisterAnimatedTexture(AssetLocation animatedTextureLocation, AssetLocation targetTextureLocation, AnimatedTextureConfig configuration)
		{
			AnimatedTexture t = new AnimatedTexture(capi, AnimatedTextureAtlas, animatedTextureLocation, targetTextureLocation, configuration);
            ulong id = GetNextUniqueId();
            t.Id = id;
            loadedTextures.Add(id, t);

			return t;
		}

        protected ulong GetNextUniqueId()
        {
            bool idFound = false;

            while (!idFound)
            {
                idFound = !loadedTextures.ContainsKey(++idKeeper);
            }

            return idKeeper;
        }

		/// <summary>
		/// Registers an animated texture to the animated texture manager
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public AnimatedTexture RegisterAnimatedTexture(AnimatedTexture t)
		{
            ulong id = GetNextUniqueId();
			loadedTextures.Add(id, t);

			return t;
		}

		public void StartAllAnimations()
		{
			foreach (AnimatedTexture loadedTexture in loadedTextures.Values)
			{
				if (!runningTextures.Contains(loadedTexture))
				{
					runningTextures.Add(loadedTexture);
				}
			}

			// loadedTextures.Clear();
		}

		public void StopAllAnimations()
		{
			/*foreach (AnimatedTexture runningTexture in runningTextures)
			{
				if (!loadedTextures.Contains(runningTexture))
				{
					loadedTextures.Add(runningTexture);
				}
			}*/

			runningTextures.Clear();
		}

		/// <summary>
		/// Creates an animated texture from an animatedtextureconfig
		/// </summary>
		/// <param name="c"></param>
		public AnimatedTexture CreateAnimatedTexture(AnimatedTextureConfig c)
		{
			AssetLocation sourceLocation = new AssetLocation(c.AnimationQualifiedPath);
			AssetLocation targetLocation = new AssetLocation(c.TargetQualifiedPath);
			// logger.Debug($"Creating animated texture: {sourceLocation.ToString()} => {targetLocation.ToString()}");

			return RegisterAnimatedTexture(sourceLocation, targetLocation, c);
		}

		internal void SetupManager(List<AnimatedTextureConfig> textureConfigurations)
		{
			if (AnimatedTextureAtlas == null)
			{
				AnimatedTextureAtlas = new AnimatedTextureAtlasManager(capi.World as ClientMain);
			}
			PreregisterAnimatedTexture(textureConfigurations);
			ComposeAnimatedTextureAtlas();
			FinalizeAnimatedTextures(textureConfigurations);

			StartAllAnimations();
		}

		/// <summary>
		/// Adds all animated textures loaded at startup into the
		/// texture atlas at once
		/// </summary>
		/// <param name="textureConfigurations"></param>
		internal void PreregisterAnimatedTexture(List<AnimatedTextureConfig> textureConfigurations)
		{
			foreach (AnimatedTextureConfig textureConfig in textureConfigurations)
			{
				(AnimatedTextureAtlas as TextureAtlasManager).GetOrAddTextureLocation(new AssetLocationAndSource(textureConfig.AnimationQualifiedPath));
			}
		}

		/// <summary>
		/// Final compose of the texture atlas at startup<br/>
		/// This hopefully prevents excessive lag due to lots of texture loading
		/// during gameplay
		/// </summary>
		internal void ComposeAnimatedTextureAtlas()
		{
			TextureAtlasManager aTexManager = (AnimatedTextureAtlas as TextureAtlasManager);
			aTexManager.CreateNewAtlas("animations");
			aTexManager.PopulateTextureAtlassesFromTextures();

			aTexManager.ComposeTextureAtlasses_StageA();
			aTexManager.ComposeTextureAtlasses_StageB();
			aTexManager.ComposeTextureAtlasses_StageC();
		}

		/// <summary>
		/// Turns the animated texture configurations into fully fledged animated textures
		/// after the texture atlas has been composed
		/// </summary>
		/// <param name="textureConfigurations"></param>
		internal void FinalizeAnimatedTextures(List<AnimatedTextureConfig> textureConfigurations)
		{
			foreach (AnimatedTextureConfig textureConfig in textureConfigurations)
			{
				CreateAnimatedTexture(textureConfig);
			}
		}

		public void StartTicking()
		{
			logger.Debug("Starting texture manager ticking...");
			logger.Debug("Active textures:");
			foreach (AnimatedTexture t in runningTextures)
			{
				logger.Debug($"{t.Name}");
			}
			if (tickListenerId != -1 && runningTextures.Count > 0)
			{
				tickListenerId = capi.Event.RegisterGameTickListener(OnGameTick, 50);
				logger.Debug("Texture manager ticking started!");
			}
		}

		public void StopTicking()
		{
			logger.Debug("Stopping texture manager ticking...");
			if (tickListenerId != -1)
			{
				capi.Event.UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}
		}

		protected void OnGameTick(float delta)
		{
			bool didRender = false;

			foreach (AnimatedTexture t in runningTextures)
			{
				try
				{
					t.Advance(capi, delta, ref didRender);
				}
				catch
				{
					logger.Debug($"Exception encountered when trying to advance frame of animated texture {t.Name}");
					// uh oh! better not do that again!
					runningTextures.Remove(t);
					// loadedTextures.Add(t);
				}
			}

			if (didRender)
			{
				capi.BlockTextureAtlas.RegenMipMaps(0);
			}
		}

		public void Dispose()
		{
			logger.Debug("Disposing the manager...");
			StopTicking();
			runningTextures.Clear();
			loadedTextures.Clear();
			(AnimatedTextureAtlas as AnimatedTextureAtlasManager).Dispose();
			AnimatedTextureAtlas = null;
			IsInitialized = false;
			logger.Debug("Manager disposing done");
		}
	}
}
