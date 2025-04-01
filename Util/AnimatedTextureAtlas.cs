using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Client;
using LibATex.Extensions;

namespace LibATex.Util
{
	public class AnimatedTextureAtlasManager : BlockTextureAtlasManager
	{
		internal FrameBufferRef readBufferRef;
		internal FrameBufferRef drawBufferRef;

		public AnimatedTextureAtlasManager(ClientMain c) : base(c)
		{
			readBufferRef = new FrameBufferRef
			{
				FboId = c.CreateBlankFramebuffer()
			};
			drawBufferRef = new FrameBufferRef
			{
				FboId = c.CreateBlankFramebuffer()
			};
		}

		public static EnumTextureAtlasType AtlasStringToAtlasEnum(string type)
		{
			switch (type)
			{
				case "block":
					{
						return EnumTextureAtlasType.Block;
					}
				case "item":
					{
						return EnumTextureAtlasType.Item;
					}
				case "entity":
					{
						return EnumTextureAtlasType.Entity;
					}
				default:
					{
						return EnumTextureAtlasType.Block;
					}
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			if (readBufferRef != null)
			{
				game.DestroyBlankFramebuffer(readBufferRef.FboId);
				readBufferRef = null;
			}

			if (drawBufferRef != null)
			{
				game.DestroyBlankFramebuffer(drawBufferRef.FboId);
				readBufferRef = null;
			}
		}
	}
}
