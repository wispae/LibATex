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
        private Dictionary<ulong, IAnimatedTexture> loadedTextures;
        // List for more efficient iterating over running textures
		private List<AnimatedTexture> runningTextures;
        private List<TimeAnimatedTexture> runningTimeTextures;

        private ulong idKeeper = 0;

		public ITextureAtlasAPI AnimatedTextureAtlas
		{
			get;
			private set;
		}

		private ICoreClientAPI capi;
		private ILogger logger;

		public bool IsInitialized = false;
		private long tickListenerId = -1;
        private long timeTickListenerId = -1;

		public AnimatedTextureManager(ICoreClientAPI capi, ILogger logger)
		{
			this.capi = capi;
			this.logger = logger;
            // loadedTextures = new List<AnimatedTexture>();
            loadedTextures = new Dictionary<ulong, IAnimatedTexture>();
			runningTextures = new List<AnimatedTexture>();
            runningTimeTextures = new List<TimeAnimatedTexture>();
			// AnimatedTextureAtlas = new AnimatedTextureAtlasManager(capi.World as ClientMain);
		}

        /// <summary>
        /// Instantiates an animated texture from the provided configuration and adds it to
        /// the list of loaded animations
        /// </summary>
        /// <param name="animatedTextureLocation"></param>
        /// <param name="targetTextureLocation"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
		public AnimatedTexture RegisterAnimatedTexture(AssetLocation animatedTextureLocation, AssetLocation targetTextureLocation, AnimatedTextureConfig configuration)
		{
            AnimatedTexture t;
            if (configuration.AnimationType.HasFlag(EnumAnimatedTextureType.TimeAnimatedTexture))
            {
                t = new TimeAnimatedTexture(capi, AnimatedTextureAtlas, animatedTextureLocation, targetTextureLocation, configuration);
            } else
            {
                t = new AnimatedTexture(capi, AnimatedTextureAtlas, animatedTextureLocation, targetTextureLocation, configuration);
            }

			// AnimatedTexture t = new AnimatedTexture(capi, AnimatedTextureAtlas, animatedTextureLocation, targetTextureLocation, configuration);
            ulong id = GetNextUniqueId();
            t.Id = id;
            loadedTextures.Add(id, t);

			return t;
		}

        /// <summary>
        /// Gets the next unique animated texture id
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Starts all loaded animations
        /// </summary>
		public void StartAllAnimations()
		{
			foreach (IAnimatedTexture loadedTexture in loadedTextures.Values)
			{
                switch (loadedTexture)
                {
                    case TimeAnimatedTexture tat:
                        if (!runningTimeTextures.Contains(tat))
                        {
                            runningTimeTextures.Add(tat);
                        }
                        break;
                    default:
                        AnimatedTexture at = loadedTexture as AnimatedTexture;
                        if (!runningTextures.Contains(at))
                        {
                            runningTextures.Add(at);
                        }
                        break;
                }
			}

			// loadedTextures.Clear();
		}

        /// <summary>
        /// Stops all currently running animations
        /// </summary>
		public void StopAllAnimations()
		{
            runningTextures.Clear();
            runningTimeTextures.Clear();
		}

		/// <summary>
		/// Creates an animated texture from an animatedtextureconfig
		/// </summary>
		/// <param name="c"></param>
		public AnimatedTexture CreateAnimatedTexture(AnimatedTextureConfig c)
		{
			AssetLocation sourceLocation = new AssetLocation(c.AnimationQualifiedPath);
			AssetLocation targetLocation = new AssetLocation(c.TargetQualifiedPath);

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
			if (tickListenerId == -1)
			{
				tickListenerId = capi.Event.RegisterGameTickListener(OnGameTick, 50);
			}
            if (timeTickListenerId == -1)
            {
                timeTickListenerId = capi.Event.RegisterGameTickListener(OnTimeTick, 1000);
            }

            logger.Debug("Texture manager ticking started!");
        }

		public void StopTicking()
		{
			logger.Debug("Stopping texture manager ticking...");
			if (tickListenerId != -1)
			{
				capi.Event.UnregisterGameTickListener(tickListenerId);
				tickListenerId = -1;
			}
            if (timeTickListenerId != -1)
            {
                capi.Event.UnregisterGameTickListener(timeTickListenerId);
                timeTickListenerId = -1;
            }
		}


        List<AnimatedTexture> errorAnimations = new List<AnimatedTexture>();
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
					logger.Debug($"Exception encountered when trying to advance frame of animated texture {t.Name}, removing from active animations");
                    errorAnimations.Add(t);
				}
			}

            if (errorAnimations.Count > 0)
            {
                foreach (AnimatedTexture t in errorAnimations)
                {
                    runningTextures.Remove(t);
                }
                errorAnimations.Clear();
            }

			if (didRender)
			{
				capi.BlockTextureAtlas.RegenMipMaps(0);
			}
		}

        List<TimeAnimatedTexture> errorTimeAnimations = new List<TimeAnimatedTexture>();
        protected void OnTimeTick(float delta)
        {
            bool didRender = false;
            float daytime = capi.World.Calendar.HourOfDay / capi.World.Calendar.HoursPerDay;

            foreach (TimeAnimatedTexture t in runningTimeTextures)
            {
                try
                {
                    t.Advance(capi, daytime, ref didRender);
                }
                catch
                {
                    logger.Debug($"Exception encountered when trying to advance frame of time animated texture {t.Name}, removing from active animations");
                    errorTimeAnimations.Add(t);
                }
            }

            if (errorTimeAnimations.Count > 0)
            {
                foreach (TimeAnimatedTexture t in errorTimeAnimations)
                {
                    runningTimeTextures.Remove(t);
                }
                errorTimeAnimations.Clear();
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
            runningTimeTextures.Clear();
			loadedTextures.Clear();
			(AnimatedTextureAtlas as AnimatedTextureAtlasManager).Dispose();
			AnimatedTextureAtlas = null;
			IsInitialized = false;
			logger.Debug("Manager disposing done");
		}
	}
}
