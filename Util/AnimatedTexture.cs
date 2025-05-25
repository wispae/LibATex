using LibATex.Extensions;
using LibATex.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace LibATex.Util
{
	public class AnimatedTexture : IAnimatedTexture
	{
		protected AssetLocation animatedLocation;
		protected AssetLocation targetLocation;

		protected LoadedTexture animatedTexture;
		protected LoadedTexture targetTexture;

		protected TextureAtlasPosition animatedTexturePosition;
		protected TextureAtlasPosition targetTexturePosition;
		protected EnumTextureAtlasType atlasType;

		protected ITextureAtlasAPI targetAtlas;

		protected AnimatedTextureManager manager;

		protected int currentColumn;
		protected int currentRow;

        protected float frameheight;
        protected float framewidth;

        protected float frameTime;

        internal int numFrames;

        protected int targetOffsetY;

        public int Columns
        {
            get; set;
        }
		public int Rows
        {
            get; set;
        }
		public int Dimension;

        public ulong Id
        {
            get; internal set;
        }

		public bool IsActive
		{
			get;
			internal set;
		}

		public bool IsComplete
		{
			get;
			protected set;
		}

		public int CurrentRow
		{
			get { return currentRow; }
			set
			{
				if (value < Rows)
				{
					currentRow = value;
				}
				else
				{
					currentRow = Rows - 1;
				}
			}
		}
		public int CurrentColumn
		{
			get { return currentColumn; }
			set
			{
				if (value < Columns)
				{
					currentColumn = value;
				}
				else
				{
					currentColumn = Columns - 1;
				}
			}
		}

		public int NumFrames
		{
			get
			{
				return numFrames;
			}
			set
			{
				if (value > (Rows * Columns))
				{
					throw new ArgumentOutOfRangeException("value", $"Number of frames ({value}) does not fit within the number of rows and columns defined");
				}

				numFrames = value;
			}
		}

        public int CurrentFrame
        {
            get
            {
                return (CurrentRow * Columns) + CurrentColumn;
            }
        }

		public int TargetOffsetX
		{
			get
			{
				return targetOffsetX;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value", "targetOffset can not be negative");
				}

				targetOffsetX = value;
			}
		}
		protected int targetOffsetX;

		public int TargetOffsetY
		{
			get
			{
				return targetOffsetY;
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentOutOfRangeException("value", "targetOffset can not be negative");
				}

				targetOffsetY = value;
			}
		}

        public int PixelHeight
        {
            get => animatedTexture.Height;
        }

        public int PixelWidth
        {
            get => animatedTexture.Width;
        }

		public int PaddingX = 0;
		public int PaddingY = 0;

		public float TimePerFrame
        {
            get; set;
        }

		public string Name;

		/// <summary>
		/// Creates an animated texture from a configuration
		/// </summary>
		/// <param name="capi"></param>
		/// <param name="sourceLocation"></param>
		/// <param name="targetLocation">The assetlocation of the texture to animate</param>
		/// <param name="configuration"></param>
		/// <returns></returns>
		public static AnimatedTexture FromConfiguration(ICoreClientAPI capi, AssetLocation sourceLocation, AssetLocation targetLocation, AnimatedTextureConfig configuration)
		{
			AnimatedTexture tex = new AnimatedTexture(capi);
			ITextureAtlasAPI animatedTextureAtlas = capi.ModLoader.GetModSystem<LibATexModSystem>().AnimatedTextureAtlas;

			tex.animatedLocation = sourceLocation;
			tex.targetLocation = targetLocation;
			AddConfigToAnimation(tex, configuration);

			if (tex.animatedTexture == null)
			{
				tex.animatedTexture = new LoadedTexture(capi);
			}
			tex.targetAtlas = GetCorrectTextureAtlas(capi, configuration.atlasType);

			bool success = false;
			int subId;
			if (sourceLocation != null)
			{
				success = animatedTextureAtlas.GetOrInsertTexture(sourceLocation, out subId, out tex.animatedTexturePosition);
				tex.animatedTexture = animatedTextureAtlas.AtlasTextures[tex.animatedTexturePosition.atlasNumber];

				float availableSpace = tex.animatedTexturePosition.x2 - tex.animatedTexturePosition.x1 - ((float)tex.PaddingX / tex.animatedTexture.Width);
				tex.framewidth = availableSpace / tex.Columns;
				availableSpace = tex.animatedTexturePosition.y2 - tex.animatedTexturePosition.y1 - ((float)tex.PaddingY / tex.animatedTexture.Height);
				tex.frameheight = availableSpace / tex.Rows;
			}

			success = false;
			success = tex.targetAtlas.GetOrInsertTexture(targetLocation, out subId, out tex.targetTexturePosition);
			tex.targetTexture = tex.targetAtlas.AtlasTextures[tex.targetTexturePosition.atlasNumber];
			tex.Name = $"{configuration.AnimationQualifiedPath} (id: {tex.animatedTexture.TextureId}) => {configuration.TargetQualifiedPath} (id: {tex.targetTexture.TextureId})";

			tex.Validate();

			return tex;
		}

		public static AnimatedTexture FromConfiguration(ICoreClientAPI capi, TextureAtlasPosition sourcePosition, TextureAtlasPosition targetPosition, AnimatedTextureConfig configuration)
		{
			if (targetPosition == null)
			{
				throw new ArgumentException("Targetposition cannot be null");
			}
			AnimatedTexture tex = new AnimatedTexture(capi);
			ITextureAtlasAPI animatedTextureAtlas = capi.ModLoader.GetModSystem<LibATexModSystem>().AnimatedTextureAtlas;

			AddConfigToAnimation(tex, configuration);

			if (tex.animatedTexture == null)
			{
				tex.animatedTexture = new LoadedTexture(capi);
			}
			tex.targetAtlas = GetCorrectTextureAtlas(capi, configuration.atlasType);

			if (sourcePosition != null)
			{
				tex.animatedTexturePosition = sourcePosition;
				tex.animatedTexture = animatedTextureAtlas.AtlasTextures[tex.animatedTexturePosition.atlasNumber];

				float availableSpace = tex.animatedTexturePosition.x2 - tex.animatedTexturePosition.x1 - ((float)tex.PaddingX / tex.animatedTexture.Width);
				tex.framewidth = availableSpace / tex.Columns;
				availableSpace = tex.animatedTexturePosition.y2 - tex.animatedTexturePosition.y1 - ((float)tex.PaddingY / tex.animatedTexture.Height);
				tex.frameheight = availableSpace / tex.Rows;
			}

			tex.targetTexturePosition = targetPosition;
			tex.targetTexture = tex.targetAtlas.AtlasTextures[tex.targetTexturePosition.atlasNumber];
			tex.Name = $"{configuration.AnimationQualifiedPath} (id: {tex.animatedTexture.TextureId}) => {configuration.TargetQualifiedPath} (id: {tex.targetTexture.TextureId})";

			tex.Validate();

			return tex;
		}

		protected static void AddConfigToAnimation(AnimatedTexture tex, AnimatedTextureConfig configuration)
		{
			tex.atlasType = configuration.atlasType;

			tex.atlasType = configuration.atlasType;
			tex.Columns = configuration.NumColumns;
			tex.Rows = configuration.NumRows;
			tex.NumFrames = configuration.NumFrames;
			tex.TimePerFrame = configuration.SecondsPerFrame;

			tex.TargetOffsetX = configuration.TargetOffsetX;
			tex.TargetOffsetY = configuration.TargetOffsetY;
			tex.PaddingX = configuration.PaddingX;
			tex.PaddingY = configuration.PaddingY;

			tex.currentColumn = 0;
			tex.currentRow = 0;
			tex.frameTime = 0;
		}

		private static ITextureAtlasAPI GetCorrectTextureAtlas(ICoreClientAPI capi, EnumTextureAtlasType atlasType)
		{
			ITextureAtlasAPI atlas;
			switch (atlasType)
			{
				case EnumTextureAtlasType.Block:
					{
						atlas = capi.BlockTextureAtlas;

						break;
					}
				case EnumTextureAtlasType.Entity:
					{
						atlas = capi.EntityTextureAtlas;

						break;
					}
				case EnumTextureAtlasType.Item:
					{
						atlas = capi.ItemTextureAtlas;

						break;
					}
				default:
					{
						atlas = capi.BlockTextureAtlas;
						break;
					}
			}

			return atlas;
		}

		/// <summary>
		/// Reserves a texture of width by height on the animated texture atlas.
		/// Automatically assigns the resulting atlas position to this animated texture.
		/// </summary>
		/// <param name="animatedAtlas"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public TextureAtlasPosition ReserveTextureSpace(ITextureAtlasAPI animatedAtlas, int width, int height)
		{
			if (animatedAtlas is not AnimatedTextureAtlasManager)
			{
				throw new ArgumentException("The provided texture atlas was not an animated texture atlas");
			}

			TextureAtlasPosition pos;
			bool success;
			success = animatedAtlas.AllocateTextureSpace(width, height, out _, out pos);

			if (success)
			{
				animatedTexturePosition = pos;
				return pos;
			}

			return null;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="animatedAtlas"></param>
		/// <param name="numCols"></param>
		/// <param name="numRows"></param>
		/// <param name="framewidth"></param>
		/// <param name="frameheight"></param>
		/// <returns></returns>
		public TextureAtlasPosition ReserveTextureSpace(ITextureAtlasAPI animatedAtlas, int numCols, int numRows, int framewidth, int frameheight)
		{
			try
			{
				return ReserveTextureSpace(animatedAtlas, numCols * framewidth, numRows * frameheight);
			} catch (ArgumentException e)
			{
				ExceptionDispatchInfo.Capture(e).Throw();
			}

			return null;
		}

		public bool Validate()
		{
			bool isValid;

			isValid = (animatedTexturePosition != null && targetTexturePosition != null);
			isValid &= (framewidth != 0 && frameheight != 0);
			isValid &= animatedTexture != null;

			IsComplete = isValid;

			return isValid;
		}

		public AnimatedTexture(ICoreClientAPI capi)
		{
			IsComplete = false;
		}

		/// <summary>
		/// Creates a managed animated texture
		/// </summary>
		/// <param name="capi"></param>
		/// <param name="animatedTextureAtlas"></param>
		/// <param name="sourceLocation">AssetLocation of the texture with the animation frames</param>
		/// <param name="targetLocation">AssetLocation of the texture to be animated</param>
		/// <param name="configuration">The configuration to base this animated texture on</param>
		public AnimatedTexture(ICoreClientAPI capi, ITextureAtlasAPI animatedTextureAtlas, AssetLocation sourceLocation, AssetLocation targetLocation, AnimatedTextureConfig configuration)
		{
			animatedLocation = sourceLocation;
			this.targetLocation = targetLocation;
			atlasType = configuration.atlasType;

			animatedTexture = new LoadedTexture(capi);
			ILogger logger = capi.ModLoader.GetMod("libatex").Logger;

			switch (atlasType)
			{
				case EnumTextureAtlasType.Block:
					{
						targetAtlas = capi.BlockTextureAtlas;

						break;
					}
				case EnumTextureAtlasType.Entity:
					{
						targetAtlas = capi.EntityTextureAtlas;

						break;
					}
				case EnumTextureAtlasType.Item:
					{
						targetAtlas = capi.ItemTextureAtlas;

						break;
					}
				default:
					{
						targetAtlas = capi.BlockTextureAtlas;
						break;
					}
			}

			bool success = false;
			int subId;
			success = animatedTextureAtlas.GetOrInsertTexture(sourceLocation, out subId, out animatedTexturePosition);
			animatedTexture = animatedTextureAtlas.AtlasTextures[animatedTexturePosition.atlasNumber];

			success = false;
			success = targetAtlas.GetOrInsertTexture(targetLocation, out subId, out targetTexturePosition);
			targetTexture = targetAtlas.AtlasTextures[targetTexturePosition.atlasNumber];

			atlasType = configuration.atlasType;
			Columns = configuration.NumColumns;
			Rows = configuration.NumRows;
			NumFrames = configuration.NumFrames;
			TimePerFrame = configuration.SecondsPerFrame;

			TargetOffsetX = configuration.TargetOffsetX;
			TargetOffsetY = configuration.TargetOffsetY;
			PaddingX = configuration.PaddingX;
			PaddingY = configuration.PaddingY;

			Name = $"{configuration.AnimationQualifiedPath} (id: {animatedTexture.TextureId}) => {configuration.TargetQualifiedPath} (id: {targetTexture.TextureId})";

			currentColumn = 0;
			currentRow = 0;
			frameTime = 0;

			float availableSpace = animatedTexturePosition.x2 - animatedTexturePosition.x1 - ((float)PaddingX / animatedTexture.Width);
			framewidth = availableSpace / Columns;
			availableSpace = animatedTexturePosition.y2 - animatedTexturePosition.y1 - ((float)PaddingY / animatedTexture.Height);
			frameheight = availableSpace / Rows;

			IsComplete = true;
		}

		public virtual void Advance(ICoreClientAPI capi, float dt, ref bool didRender)
		{
			frameTime += dt;
			if (frameTime >= TimePerFrame)
			{
				frameTime = frameTime - TimePerFrame;
				NextFrame(capi);

				didRender = true;
			}
		}

		public void NextFrame(ICoreClientAPI capi)
		{
			currentColumn++;
			if (currentColumn >= Columns)
			{
				currentColumn = 0;
				currentRow++;
				if (currentRow >= Rows)
				{
					currentRow = 0;
				}
			}

			if ((currentRow * Columns + currentColumn) >= numFrames)
			{
				currentColumn = 0;
				currentRow = 0;
			}

			RenderCurrentFrame(capi);
		}

        /// <summary>
        /// Renders the current frame onto the target texture
        /// </summary>
        /// <param name="capi"></param>
		public virtual void RenderCurrentFrame(ICoreClientAPI capi)
		{
			float d = animatedTexturePosition.x2 - animatedTexturePosition.x1;

			float x1 = animatedTexturePosition.x1 + framewidth * currentColumn;
			float y1 = animatedTexturePosition.y1 + frameheight * currentRow;
			float x2 = targetTexturePosition.x1;
			float y2 = targetTexturePosition.y1;

			targetAtlas.RenderTextureIntoAtlasPersistent(capi, targetTexturePosition.atlasNumber, animatedTexture, x1, y1, framewidth, frameheight, x2, y2, targetOffsetX, targetOffsetY);
		}

		/// <summary>
		/// Disposes of this animated texture<br/>
		/// Also stops the animated texture and removes it from the manager
		/// </summary>
		/// <remarks>
		/// Leaves the target texture intact
		/// </remarks>
		public void Dispose()
		{
			animatedTexture.Dispose();
		}
	}
}
