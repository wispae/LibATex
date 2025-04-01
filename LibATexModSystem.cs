using LibATex.Model;
using LibATex.Util;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Vintagestory;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

[assembly: ModInfo("LibAtex",
					Authors = new string[] { "Nanotect" },
					Description = "Allows for the adding of animated textures. Does nothing on its own.",
					Version = "0.5.0",
					Side = "Client",
					RequiredOnServer = false,
					RequiredOnClient = true)]
namespace LibATex
{
	public class LibATexModSystem : ModSystem
	{
		private List<AnimatedTextureConfig> configs;
		private AnimatedTextureManager manager;
		private AnimationConfigManager configManager;
		private bool hasLoaded = false;

		public ITextureAtlasAPI AnimatedTextureAtlas => manager.AnimatedTextureAtlas;

		private AnimatedTextureAtlasManager animatedTextureAtlasManager;
		private ICoreClientAPI capi;

		public LibATexModSystem() : base()
		{
			configs = new List<AnimatedTextureConfig>();
		}

		public override bool ShouldLoad(EnumAppSide forSide)
		{
			return forSide == EnumAppSide.Client;
		}

		public override void StartClientSide(ICoreClientAPI api)
		{
			base.StartClientSide(api);

			capi = api;
		}

		public override void AssetsLoaded(ICoreAPI api)
		{
			base.AssetsLoaded(api);

			if (api.Side == EnumAppSide.Client) OnClientAssetsLoaded((ICoreClientAPI)api);
		}

		public void OnClientAssetsLoaded(ICoreClientAPI capi)
		{
			this.capi = capi;
			configManager = new AnimationConfigManager(capi, Mod.Logger);

			foreach (Mod m in capi.ModLoader.Mods)
			{
				configManager.LoadConfigForModId(m.Info.ModID);
			}

			configManager.ValidateConfigs();
			configManager.FilterUniqueTargetTextures();

			Mod.Logger.Debug($"Loaded {configManager.GetTotalAnimationCount()} animated texture configurations from {capi.ModLoader.Mods.Count()} mods");
			hasLoaded = true;

			capi.Event.BlockTexturesLoaded += OnTexturesLoaded;
			capi.Event.ReloadTextures += OnTextureReload;
			capi.Event.PlayerJoin += OnPlayerJoined;
			capi.Event.PlayerLeave += OnPlayerLeft;
		}

		public override double ExecuteOrder()
		{
			return 0.9d;
		}

		private void OnTextureReload()
		{
			Mod.Logger.Debug("Texture reload occured!");
		}

		private void OnTexturesLoaded()
		{
			manager = new AnimatedTextureManager(capi, Mod.Logger);
			configManager.RegisterStartupConfigurations(manager);
			manager.IsInitialized = true;
		}

		public override void Dispose()
		{
			Mod.Logger.Debug("Dispose of LibATexModSystem requested!");

			configs.Clear();
			hasLoaded = false;

			if (manager != null)
			{
				manager.Dispose();
			}

			if (capi == null) return;

			capi.Event.PlayerJoin -= OnPlayerJoined;
			capi.Event.PlayerLeave -= OnPlayerLeft;

			base.Dispose();
		}

		private void OnPlayerJoined(IPlayer player)
		{
			Mod.Logger.Debug("Player joined, attempting to start manager...");
			if (player.PlayerUID == capi.World.Player.PlayerUID)
			{
				if (manager != null && manager.IsInitialized)
				{
					manager.StartTicking();
				}
				else
				{
					Mod.Logger.Debug("Failed to start manager; not initialized");
				}
			}
		}

		private void OnPlayerLeft(IPlayer player)
		{
			if (player.PlayerUID == capi.World.Player.PlayerUID)
			{
				Mod.Logger.Debug("Player left the world, disposing of the manager");
				manager.Dispose();
				manager = null;
			}
		}
	}
}
